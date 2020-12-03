using KatranClassLibrary;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace KatranServer
{
    class ClientService
    {
        private TcpClient client;
        internal static OracleConnection sql;
        public ClientService(TcpClient client)
        {
            this.client = client;
        }

        //обработка запроса пользователя
        public void Service()
        {
            bool isUserStreamlistener = false; //флаг для предотвращения закрытия потока юзера, который выступает в роли слушателя (если true, то поток не закроется)
            NetworkStream clientStream = null;
            try
            {
                clientStream = client.GetStream();

                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    #region Запись в MemoryStream данных из запроса
                    do
                    {
                        byte[] buffer = new byte[256];
                        int bytes;
                        bytes = clientStream.Read(buffer, 0, buffer.Length);
                        memoryStream.Write(buffer, 0, bytes);

                        buffer = new byte[256];
                    }
                    while (clientStream.DataAvailable);
                    #endregion

                    #region Приведение к RRTemplate и обработка запроса в соответствии с его типом
                    memoryStream.Position = 0;
                    RRTemplate clientRequest = formatter.Deserialize(memoryStream) as RRTemplate;
                    if (clientRequest != null)
                    {
                        sql = OracleDB.GetDBConnection();
                        sql.Open();

                        switch (clientRequest.RRType)
                        {
                            case RRType.Authorization:
                                AuthtorizationTemplate auth = clientRequest.RRObject as AuthtorizationTemplate;
                                if (auth != null)
                                {
                                    Authtorization(auth);
                                }
                                break;
                            case RRType.Registration:
                                RegistrationTemplate reg = clientRequest.RRObject as RegistrationTemplate;
                                if (reg != null)
                                {
                                    Registration(reg);
                                }
                                break;
                            case RRType.UserDisconected:
                                RefreshUserTemplate refrUC = clientRequest.RRObject as RefreshUserTemplate;
                                if (refrUC != null)
                                {
                                    Server.ChangeStatus(refrUC.UserId, Status.Offline);
                                }
                                break;
                            case RRType.RefreshUserConnection:
                                RefreshUserTemplate refrU = clientRequest.RRObject as RefreshUserTemplate;
                                if (refrU != null)
                                {
                                    RefreshClientListener(refrU);
                                    isUserStreamlistener = true;
                                }
                                break;
                            case RRType.RefreshContacts:
                                RefreshContactsTemplate refrC = clientRequest.RRObject as RefreshContactsTemplate;
                                if (refrC != null)
                                {
                                    RefreshContacts(refrC);
                                }
                                break;
                            case RRType.SearchOutContacts:
                                SearchOutContactsTemplate searchOutC = clientRequest.RRObject as SearchOutContactsTemplate;
                                if (searchOutC != null)
                                {
                                    SearchOutContacts(searchOutC);
                                }
                                break;
                            case RRType.RefreshUserData:
                                AuthtorizationTemplate refrUserData = clientRequest.RRObject as AuthtorizationTemplate;
                                if (refrUserData != null)
                                {
                                    RefreshUserData(refrUserData);
                                }
                                break;
                            case RRType.AddContact:
                                AddRemoveContactTemplate aContT = clientRequest.RRObject as AddRemoveContactTemplate;
                                if(aContT != null)
                                {
                                    AddContact(aContT);
                                }
                                break;
                            case RRType.RemoveContact:
                                AddRemoveContactTemplate rContT = clientRequest.RRObject as AddRemoveContactTemplate;
                                if (rContT != null)
                                {
                                    RemoveContact(rContT);
                                }
                                break;
                            case RRType.SendMessage:
                                SendMessageTemplate sMessT = clientRequest.RRObject as SendMessageTemplate;
                                if (sMessT != null)
                                {
                                    SendMessage(sMessT);
                                }
                                break;
                            case RRType.DownloadFile:
                                DownloadFileTemplate dfileT = clientRequest.RRObject as DownloadFileTemplate;
                                if (dfileT != null)
                                {
                                    DownloatFile(dfileT);
                                }
                                break;
                            case RRType.RefreshMessageState:
                                RefreshMessageStateTemplate rmessT = clientRequest.RRObject as RefreshMessageStateTemplate;
                                if (rmessT != null)
                                {
                                    RefreshMessageState(rmessT);
                                }
                                break;
                            case RRType.CreateConv:
                                CreateConvTemplate crconvT = clientRequest.RRObject as CreateConvTemplate;
                                if (crconvT != null)
                                {
                                    CreateConv(crconvT);
                                }
                                break;
                            case RRType.RemoveConv:
                                RemoveConvTemplate rconvT = clientRequest.RRObject as RemoveConvTemplate;
                                if (rconvT != null)
                                {
                                    RemoveConv(rconvT);
                                }
                                break;
                            case RRType.AdminSearch:
                                AdminSearchTemplate admST = clientRequest.RRObject as AdminSearchTemplate;
                                if (admST != null)
                                {
                                    AdminSearch(admST);
                                }
                                break;
                            case RRType.BlockUnblockUser:
                                BlockUnblockUserTemplate bunbUT = clientRequest.RRObject as BlockUnblockUserTemplate;
                                if (bunbUT != null)
                                {
                                    BlockUnblockUser(bunbUT);
                                }
                                break;
                            default:
                                ErrorResponse(ErrorType.Other, new Exception("Получен необработанный запрос"));
                                break;
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                ErrorResponse(ErrorType.Other, new Exception("Ошибка с обработкой данных из запроса"));
            }
            finally
            {
                if (!isUserStreamlistener)
                {
                    //закрываем поток данных и соединение с клиентом
                    clientStream.Close();
                    client.Close();
                    sql.Close();
                }
                
            }
        }

        private void BlockUnblockUser(BlockUnblockUserTemplate bunbUT)
        {
            #region Проверка, что действие совершает админ
            SqlCommand command = new SqlCommand("select u.id from Users as u where u.id = @adminId and law_status = 'Admin' ", null); //TODO: sql
            command.Parameters.Add(new SqlParameter("@adminId", bunbUT.AdminId));
            SqlDataReader reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Close();
                command = new SqlCommand("update Users_info set is_blocked = @isBlocked where id = @userId ", null); //TODO: sql
                command.Parameters.Add(new SqlParameter("@isBlocked", bunbUT.IsBlocked));
                command.Parameters.Add(new SqlParameter("@userId", bunbUT.UserId));
                command.ExecuteNonQuery();

                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    formatter.Serialize(memoryStream, new RRTemplate(RRType.BlockUnblockUser, bunbUT));

                    client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }

                foreach (ConectedUser u in Server.conectedUsers)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        formatter.Serialize(memoryStream, new RRTemplate(RRType.BlockUnblockUserTarget, bunbUT));

                        u.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                    }
                }
            }
            else
            {
                reader.Close();
                bunbUT.IsBlocked = !bunbUT.IsBlocked;
                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    formatter.Serialize(memoryStream, new RRTemplate(RRType.BlockUnblockUser, bunbUT));

                    client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }
            }
            #endregion
        }

        private void AdminSearch(AdminSearchTemplate admST)
        {
            #region Отправка запроса на поиск контактов по паттерну 
            SqlCommand command = new SqlCommand("select ui.id, ui.app_name, ui.image, ui.status, ui.is_blocked " +
                                                "from Users_info as ui " +
                                                "where charindex(@pattern, ui.app_name) > 0 and ui.id != @adminID ", null); //TODO: sql
            command.Parameters.Add(new SqlParameter("@pattern", admST.Pattern));
            command.Parameters.Add(new SqlParameter("@adminID", admST.AdminId));

            SqlDataReader reader = command.ExecuteReader();

            List<Contact> contacts = new List<Contact>();
            if (reader.HasRows)
            {
                Contact tempContact = null;
                while (reader.Read())
                {
                    tempContact = new Contact();
                    tempContact.UserId = reader.GetInt32(0);
                    tempContact.AppName = reader.GetString(1);

                    object imageObj = reader.GetValue(2);
                    if (imageObj is System.DBNull)
                    {
                        tempContact.AvatarImage = GetDefaultUserImage();
                    }
                    else
                    {
                        tempContact.AvatarImage = ((OracleBlob)imageObj).Value;
                    }
                    tempContact.Status = (Status)Enum.Parse(typeof(Status), reader.GetString(3));
                    tempContact.IsBlocked = reader.GetBoolean(4);
                    contacts.Add(tempContact);
                }
            }
            reader.Close();
            #endregion
            admST.Users = contacts;
            #region Отправка найденных контактов или пустого листа 

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.AdminSearch, admST));

                client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
            }

            #endregion

        }

        private void RemoveConv(RemoveConvTemplate rconvT)
        {
            #region Удаление из таблицы chatMembers юзера у беседы
            SqlCommand command = new SqlCommand("delete from ChatMembers where chat_id = @chatId and member_id = @memberId ", null); //TODO: sql
            command.Parameters.Add(new SqlParameter("@chatId", rconvT.ChatId));
            command.Parameters.Add(new SqlParameter("@memberId", rconvT.OwnerId));
            command.ExecuteNonQuery();
            #endregion

            #region Удаление чата и сообщений если участников беседы больше нет
            command = new SqlCommand("select member_id from ChatMembers where chat_id = @chatId", null); //TODO: sql
            command.Parameters.Add(new SqlParameter("@chatId", rconvT.ChatId));
            SqlDataReader reader = command.ExecuteReader();
            if (!reader.HasRows)
            {
                reader.Close();
                command = new SqlCommand($"drop table ChatMessages_{rconvT.ChatId} " +
                                          "delete from Chats where chat_id = @chatID", null); //TODO: sql
                command.Parameters.Add(new SqlParameter("@chatID", rconvT.ChatId));
                command.ExecuteNonQuery();
            }
            #endregion

            #region Ответ об успешном удалении юзера беседы
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.RemoveConv, rconvT));

                ConectedUser user = Server.conectedUsers.Find(x => x.id == rconvT.OwnerId);
                if (user != null)
                {
                    user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }
            }

            if (!reader.IsClosed)
            {
                using (MemoryStream memoryStream = new MemoryStream()) //Уведомление других юзеров о его выходе
                {
                    formatter.Serialize(memoryStream, new RRTemplate(RRType.RemoveConvTarget, rconvT));
                    ConectedUser user;
                    while (reader.Read())
                    {
                        user = Server.conectedUsers.Find(x => x.id == reader.GetInt32(0));
                        if (user != null)
                        {
                            user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                        }
                    }
                }
            }

            reader.Close();
            #endregion
        }

        private void CreateConv(CreateConvTemplate crconvT)
        {
            #region Добавление чата в бд для беседы
            SqlCommand command = new SqlCommand("insert into Chats (chat_title, chat_kind, chat_avatar) values (@chatTitle, @chatKind, @chatAvatar)", null); //TODO: sql
            command.Parameters.Add(new SqlParameter("@chatTitle", crconvT.Title));
            command.Parameters.Add(new SqlParameter("@chatKind", ContactType.Conversation.ToString()));

            if (crconvT.Image == null || crconvT.Image.Length == 0)
                command.Parameters.Add("@chatAvatar", SqlDbType.VarBinary).Value = DBNull.Value;
            else
                command.Parameters.Add(new SqlParameter("@chatAvatar", crconvT.Image));

            command.ExecuteNonQuery();
            #endregion

            #region Получаем Id только что добавленного чата и записываем в chatId
            SqlCommand getChatID = new SqlCommand("select chat_id from Chats where chat_title = @chatTitle", null); //TODO: sql
            getChatID.Parameters.Add(new SqlParameter("@chatTitle", crconvT.Title));
            SqlDataReader reader = getChatID.ExecuteReader();
            reader.Read();
            crconvT.ChatId = reader.GetInt32(0);
            reader.Close();
            #endregion

            #region Добавляем в таблицу ChatMembers участников беседы
            command = new SqlCommand("insert into ChatMembers (chat_id, member_id) values (@chatId, @member)", null); //TODO: sql
            foreach (Contact c in crconvT.ConvMembers)
            {
                command.Parameters.Add(new SqlParameter("@chatId", crconvT.ChatId));
                command.Parameters.Add(new SqlParameter("@member", c.UserId));
                command.ExecuteNonQuery();
                command.Parameters.Clear();
            }
            #endregion

            #region Создание таблицу с сообщениями для только что созданного чата
            command = new SqlCommand($"create table ChatMessages_{crconvT.ChatId} " +
                                    "( message_id int identity(1,1) primary key," +
                                    " sender_id int," +
                                    " message varbinary(MAX)," +
                                    " message_type varchar(4) check(message_type in ('File', 'Text'))," +
                                    " file_name varchar(max)," +
                                    " time datetime," +
                                    " message_status varchar(8) check(message_status in ('Readed', 'Sended', 'Unreaded', 'Unsended')))", null); //TODO: sql
            command.ExecuteNonQuery();
            #endregion

            #region Отправка созданной беседы всем ее участникам
            command = new SqlCommand("select ui.app_name, ui.image, ui.status from Users_info as ui where ui.id = @userID", null); //TODO: sql
            foreach (Contact c in crconvT.ConvMembers)
            {
                command.Parameters.Add(new SqlParameter("@userID", c.UserId));
                reader = command.ExecuteReader();
                reader.Read();

                c.AppName = reader.GetString(0);

                object imageObj = reader.GetValue(1);
                if (imageObj is System.DBNull)
                {
                    c.AvatarImage = GetDefaultUserImage();
                }
                else
                {
                    c.AvatarImage = ((OracleBlob)imageObj).Value;
                }

                c.Status = (Status)Enum.Parse(typeof(Status), reader.GetString(2));
                reader.Close();
                command.Parameters.Clear();
            }

            if (crconvT.Image == null)
            {
                crconvT.Image = GetDefaultUserImage();
            }

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.CreateConv, crconvT));

                foreach (Contact c in crconvT.ConvMembers)
                {
                    ConectedUser u = Server.conectedUsers.Find(x => x.id == c.UserId);
                    if (u != null)
                    {
                        u.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                    }
                }
            }

            #endregion

        }

        private void RefreshMessageState(RefreshMessageStateTemplate rmessT)
        {
            #region Изменение состояния сообщения на прочитанное
            using (SqlCommand command = new SqlCommand($"update ChatMessages_{rmessT.ChatId} set message_status = @status where message_id = @id", null)) //TODO: sql
            {
                if(rmessT.MessageState == MessageState.Sended)
                {
                    rmessT.MessageState = MessageState.Readed;
                }

                command.Parameters.Add(new SqlParameter("@status", rmessT.MessageState.ToString()));
                command.Parameters.Add(new SqlParameter("@id", rmessT.messageId));
                command.ExecuteNonQuery();
            }
            #endregion
            

            #region Ответ об успешной смене состояния сообщения
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream()) //отправка владельцу сообщения новое состояние
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.RefreshMessageState, rmessT));

                client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
            }

            // отправка всем членам чата уведомления о смене состояния сообщения
            using (SqlCommand command = new SqlCommand($"select cm.member_id from ChatMembers as cm where cm.chat_id = @chat_id", null)) //TODO: sql
            {
                command.Parameters.Add(new SqlParameter("@chat_id", rmessT.ChatId));
                SqlDataReader reader = command.ExecuteReader();


                if (reader.HasRows)
                {
                    ConectedUser user;
                    int chatMemberId;

                    while (reader.Read())
                    {
                        chatMemberId = reader.GetInt32(0);

                        user = Server.conectedUsers.Find((x) => x.id == chatMemberId);
                        if (user != null)
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                formatter.Serialize(memoryStream, new RRTemplate(RRType.RefreshMessageState, rmessT));
                                user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                            }
                            
                        }

                    }
                    reader.Close();
                }

                
            }
            #endregion
        }

        private void DownloatFile(DownloadFileTemplate dfileT)
        {
            SqlCommand command = new SqlCommand($"select message, file_name from ChatMessages_{dfileT.ChatId} where message_id = {dfileT.messageId}", null); //TODO: sql
            SqlDataReader reader = command.ExecuteReader();
            dfileT.Message = new Message();
            if (reader.HasRows)
            {
                reader.Read();
                dfileT.Message.MessageBody = (byte[])reader.GetValue(0);
                dfileT.Message.FileName = reader.GetString(1);

                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    formatter.Serialize(memoryStream, new RRTemplate(RRType.DownloadFile, dfileT));

                    client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }

            }
            reader.Close();
        }

        private void SendMessage(SendMessageTemplate sMessT)
        {
            #region Получаем ID всех членов чата
            SqlCommand command = new SqlCommand("select member_id from ChatMembers where chat_id = @chatId", null); //TODO: sql
            command.Parameters.Add(new SqlParameter("@chatId", sMessT.ReceiverChatID));
            SqlDataReader reader = command.ExecuteReader();
            List<int> chatMembers = new List<int>();
            if (reader.HasRows)
            {
                while(reader.Read())
                {
                    chatMembers.Add(reader.GetInt32(0));
                }
            }
            reader.Close();
            #endregion

            #region Отправляем сообщение в бд и обновляем его messageID and MessageState 
            command = new SqlCommand($"insert into ChatMessages_{sMessT.ReceiverChatID} (sender_id, message, message_type, file_name, time, message_status) " +
                                      "values(@sender_id, @message, @message_type, @file_name, @time, @message_status)", null); //TODO: sql
            command.Parameters.Add(new SqlParameter("@sender_id", sMessT.Message.SenderID));
            command.Parameters.Add(new SqlParameter("@message", sMessT.Message.MessageBody));
            command.Parameters.Add(new SqlParameter("@message_type", sMessT.Message.MessageType.ToString()));
            command.Parameters.Add(new SqlParameter("@file_name", sMessT.Message.FileName));
            command.Parameters.Add(new SqlParameter("@time", sMessT.Message.Time));
            command.Parameters.Add(new SqlParameter("@message_status", MessageState.Sended.ToString()));
            command.ExecuteNonQuery();
            command.Parameters.Clear();

            switch (sMessT.Message.MessageType)
            {
                case MessageType.File:
                    if (sMessT.Message.MessageBody.Length < 1000000) //если меньше мегабайта
                    {
                        sMessT.Message.FileSize = Convert.ToString(((float)sMessT.Message.MessageBody.Length / 1000) + " Kb");
                    }
                    else
                    {
                        if (sMessT.Message.MessageBody.Length < 1000000000) //если меньше гигабайта
                        {
                            sMessT.Message.FileSize = Convert.ToString(((float)sMessT.Message.MessageBody.Length / 1000000) + " Mb");
                        }
                        else
                        {
                            sMessT.Message.FileSize = Convert.ToString(((float)sMessT.Message.MessageBody.Length / 1000000000) + " Gb");
                        }
                    }
                    sMessT.Message.MessageBody = new byte[1];
                    break;
                default:
                    break;
            }

            command = new SqlCommand("select message_id, app_name " +
                                    $"from ChatMessages_{sMessT.ReceiverChatID} as c join Users_info as u on c.sender_id = u.id " +
                                    $"where sender_id = @sender_id and time = @time", null); //TODO: sql
            command.Parameters.Add(new SqlParameter("@sender_id", sMessT.Message.SenderID));
            command.Parameters.Add(new SqlParameter("@time", sMessT.Message.Time));
            reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                reader.Read();
                sMessT.Message.MessageID = reader.GetInt32(0);
                sMessT.Message.SenderName = reader.GetString(1);
                sMessT.Message.MessageState = MessageState.Sended;
            }
            reader.Close();
            #endregion

            #region Ответ об успешной отправке сообщения и отправка получателю, если он в сети
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.SendMessage, new SendMessageTemplate(sMessT.ReceiverChatID, sMessT.Message)));
                ConectedUser user;
                foreach (int userId in chatMembers)
                {
                    user = Server.conectedUsers.Find(x => x.id == userId);
                    if (user != null)
                    {
                        user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                    }
                }
            }
            #endregion

        }

        private void RemoveContact(AddRemoveContactTemplate rContT)
        {
            #region Удаление из таблицы контактов 
            SqlCommand command = new SqlCommand("delete from Contacts where contact_owner = @contactOwner and contact = @targetContact " +
                                                "delete from Contacts where contact_owner = @targetContact and contact = @contactOwner", null); //TODO: sql
            command.Parameters.Add(new SqlParameter("@contactOwner", rContT.ContactOwnerId));
            command.Parameters.Add(new SqlParameter("@targetContact", rContT.TargetContactId));
            command.ExecuteNonQuery();
            #endregion

            #region Удаление чата и сообщений этих юзеров из бд
            if (rContT.ChatId != -1)
            {
                command = new SqlCommand($"drop table ChatMessages_{rContT.ChatId} " +
                                          "delete from ChatMembers where chat_id = @chatID " +
                                          "delete from Chats where chat_id = @chatID", null); //TODO: sql
                command.Parameters.Add(new SqlParameter("@chatID", rContT.ChatId));
                command.ExecuteNonQuery();
            }
            #endregion

            #region Ответ об успешном удалении юзера из контактов
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.RemoveContact, new AddRemoveContactTemplate(rContT.ContactOwnerId, rContT.TargetContactId, rContT.ChatId)));

                ConectedUser user = Server.conectedUsers.Find(x => x.id == rContT.ContactOwnerId);
                if (user != null)
                {
                    user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }
            }


            using (MemoryStream memoryStream = new MemoryStream())
            {
                ConectedUser user = Server.conectedUsers.Find(x => x.id == rContT.TargetContactId); //удаление контакта у другого юзера
                if (user != null)
                {
                    Contact rContact = new Contact();
                    rContact.UserId = rContT.ContactOwnerId;
                    formatter.Serialize(memoryStream, new RRTemplate(RRType.RemoveContactTarget, new AddRemoveContactTargetTemplate(rContact)));
                    user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }
            }
            #endregion

        }

        private void AddContact(AddRemoveContactTemplate arContT)
        {
            #region Добавление в таблицу контактов 
            OracleCommand addUserCommand = new OracleCommand("katran_procedures.AddContact", sql);
            addUserCommand.CommandType = CommandType.StoredProcedure;
            addUserCommand.Parameters.Add(CreateParam("in_outContactOwner", arContT.ContactOwnerId, ParameterDirection.InputOutput));
            addUserCommand.Parameters.Add(CreateParam("inTargetContact", arContT.TargetContactId, ParameterDirection.Input));
            addUserCommand.ExecuteNonQuery();
            #endregion

            #region Добавление чата в бд для этих юзеров и получаем Id только что добавленного чата и записываем в chatId
            string chatTitle = String.Format("{0}_{1}", arContT.ContactOwnerId, arContT.TargetContactId);
            OracleCommand addChatCommand = new OracleCommand("katran_procedures.AddChat", sql);
            addChatCommand.CommandType = CommandType.StoredProcedure;
            addChatCommand.Parameters.Add(CreateParam("inChatTitle", chatTitle, ParameterDirection.Input));
            addChatCommand.Parameters.Add(CreateParam("out_AddedChatId", -1, ParameterDirection.Output));
            addChatCommand.ExecuteNonQuery();
            arContT.ChatId = (int)addChatCommand.Parameters["out_AddedChatId"].Value;
            #endregion

            #region Добавляем в таблицу СhatMembers юзеров
            OracleCommand addChatMemberCommand = new OracleCommand("katran_procedures.AddChatMember", sql);
            addChatMemberCommand.CommandType = CommandType.StoredProcedure;
            addChatMemberCommand.Parameters.Add(CreateParam("inChatId", arContT.ChatId, ParameterDirection.Input));
            addChatMemberCommand.Parameters.Add(CreateParam("in_outMemberId", arContT.ContactOwnerId, ParameterDirection.InputOutput));
            addChatMemberCommand.ExecuteNonQuery();

            addChatMemberCommand = new OracleCommand("katran_procedures.AddChatMember", sql);
            addChatMemberCommand.CommandType = CommandType.StoredProcedure;
            addChatMemberCommand.Parameters.Add(CreateParam("inChatId", arContT.ChatId, ParameterDirection.Input));
            addChatMemberCommand.Parameters.Add(CreateParam("in_outMemberId", arContT.TargetContactId, ParameterDirection.InputOutput));
            addChatMemberCommand.ExecuteNonQuery();
            #endregion

            #region Создание таблицу с сообщениями для только что созданного чата
            command = new SqlCommand($"create table ChatMessages_{arContT.ChatId} " +
                                    "( message_id int identity(1,1) primary key," +
                                    " sender_id int," +
                                    " message varbinary(MAX)," +
                                    " message_type varchar(4) check(message_type in ('File', 'Text'))," +
                                    " file_name varchar(max)," +
                                    " time datetime," +
                                    " message_status varchar(8) check(message_status in ('Readed', 'Sended', 'Unreaded', 'Unsended')))", null); //TODO: sql
            command.ExecuteNonQuery();
            #endregion

            #region Отправка добавленного контакта

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.AddContact, new AddRemoveContactTemplate(arContT.ContactOwnerId, arContT.TargetContactId, arContT.ChatId)));

                ConectedUser user = Server.conectedUsers.Find(x => x.id == arContT.ContactOwnerId);
                if (user != null)
                {
                    user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                ConectedUser user = Server.conectedUsers.Find(x => x.id == arContT.TargetContactId); //если другой юзер онлайн, то добавить контакт и ему
                if (user != null)
                {
                    Contact newContact = new Contact();
                    newContact.ChatId = arContT.ChatId;
                    newContact.UserId = arContT.ContactOwnerId;
                    newContact.Status = Status.Online;

                    command = new SqlCommand("select ui.app_name, ui.image from Users_info as ui where ui.id = @userID", null); //TODO: sql
                    command.Parameters.Add(new SqlParameter("@userID", newContact.UserId));

                    reader = command.ExecuteReader();
                    reader.Read();

                    newContact.AppName = reader.GetString(0);

                    object imageObj = reader.GetValue(1);
                    if (imageObj is System.DBNull)
                    {
                        newContact.AvatarImage = GetDefaultUserImage();
                    }
                    else
                    {
                        newContact.AvatarImage = ((OracleBlob)imageObj).Value;
                    }
                    reader.Close();
                    formatter.Serialize(memoryStream, new RRTemplate(RRType.AddContactTarget, new AddRemoveContactTargetTemplate(newContact)));
                    user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }

            }
            #endregion

        }

        //поиск контактов вне контактов пользователя по поисковому запросу
        private void SearchOutContacts(SearchOutContactsTemplate searchOutC)
        {
            #region Отправка запроса на поиск контактов по паттерну вне контактов и запись их в лист contacts
            SqlCommand command = new SqlCommand("select ui.id, ui.app_name, ui.image, ui.status, ui.is_blocked " +
                                                "from Users_info as ui " +
                                                "where charindex(@pattern, ui.app_name) > 0 and ui.id != @userID and " +
                                                "ui.id NOT IN(select ui.id from Users_info as ui join Contacts as c on ui.id = c.contact and c.contact_owner = @userID)", null); //TODO: sql
            command.Parameters.Add(new SqlParameter("@pattern", searchOutC.SearchPattern));
            command.Parameters.Add(new SqlParameter("@userID", searchOutC.ContactsOwner));

            SqlDataReader reader = command.ExecuteReader();

            List<Contact> contacts = new List<Contact>();
            if (reader.HasRows)
            {
                Contact tempContact = null;
                while (reader.Read())
                {
                    tempContact = new Contact();
                    tempContact.UserId = reader.GetInt32(0);
                    tempContact.AppName = reader.GetString(1);

                    object imageObj = reader.GetValue(2);
                    if (imageObj is System.DBNull)
                    {
                        tempContact.AvatarImage = GetDefaultUserImage();
                    }
                    else
                    {
                        tempContact.AvatarImage = ((OracleBlob)imageObj).Value;
                    }
                    tempContact.Status = (Status)Enum.Parse(typeof(Status), reader.GetString(3));
                    tempContact.IsBlocked = reader.GetBoolean(4);
                    contacts.Add(tempContact);
                }
            }
            reader.Close();
            #endregion

            #region Отправка найденных контактов или пустого листа 

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.SearchOutContacts, new SearchOutContactsTemplate(searchOutC.ContactsOwner, "", contacts)));

                ConectedUser user = Server.conectedUsers.Find(x => x.id == searchOutC.ContactsOwner);
                if (user != null)
                {
                    user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }
            }

            #endregion

        }

        private void RefreshUserData(AuthtorizationTemplate refrUserData)
        {
            #region Запрос на данные о пользователе
            OracleCommand command = new OracleCommand($"select id, password from katran_admin.users where auth_login = :auth_login", sql);
            command.Parameters.Add(new OracleParameter("auth_login", refrUserData.AuthLogin));

            OracleDataReader reader_user = command.ExecuteReader();
            #endregion

            if (reader_user.HasRows)
            {
                #region Запрос на данные о пользователе
                reader_user.Read();
                int id = reader_user.GetInt32(0);
                string password = reader_user.GetString(1);

                OracleCommand getUserInfo = new OracleCommand("katran_procedures.GetUserInfoById", sql);
                getUserInfo.CommandType = CommandType.StoredProcedure;
                getUserInfo.Parameters.Add(CreateParam("in_outUserId", id, ParameterDirection.InputOutput));
                getUserInfo.Parameters.Add(CreateParam("in_outAppName", new string('\0', 200), ParameterDirection.InputOutput));
                getUserInfo.Parameters.Add(CreateParam("in_outEmail", new string('\0', 200), ParameterDirection.InputOutput));
                getUserInfo.Parameters.Add(CreateParam("in_outUserDescription", new string('\0', 200), ParameterDirection.InputOutput));
                getUserInfo.Parameters.Add(CreateParam("in_outImage", new OracleBlob(sql), ParameterDirection.InputOutput, OracleDbType.Blob));
                getUserInfo.Parameters.Add(CreateParam("in_outStatus", new string('\0', 200), ParameterDirection.InputOutput));
                getUserInfo.Parameters.Add(CreateParam("in_outLawStatus", new string('\0', 200), ParameterDirection.InputOutput));
                getUserInfo.Parameters.Add(CreateParam("in_outIsBlocked", 1, ParameterDirection.InputOutput));
                getUserInfo.ExecuteNonQuery();
                #endregion

                #region Отправка ответа на запрос авторизации

                #region Обработка случая если картинка пользователя не задана, то ставится стандартная
                OracleBlob imageObj = (OracleBlob)getUserInfo.Parameters["in_outImage"].Value;
                byte[] image;
                if (imageObj.IsNull)
                {
                    image = GetDefaultUserImage();
                }
                else
                {
                    image = imageObj.Value;
                }
                #endregion

                #region Отправка ответа на запрос обновление данных о пользователе
                RegistrationTemplate regTempl = new RegistrationTemplate(
                    id,
                    (string)getUserInfo.Parameters["in_outAppName"].Value,
                    (string)getUserInfo.Parameters["in_outEmail"].Value,
                    (string)getUserInfo.Parameters["in_outUserDescription"].Value,
                    image,
                    (Status)Enum.Parse(typeof(Status), (string)getUserInfo.Parameters["in_outStatus"].Value),
                    (LawStatus)Enum.Parse(typeof(LawStatus), (string)getUserInfo.Parameters["in_outLawStatus"].Value),
                    (int)getUserInfo.Parameters["in_outIsBlocked"].Value > 0,
                    refrUserData.AuthLogin, 
                    password);

                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    formatter.Serialize(memoryStream, new RRTemplate(RRType.RefreshUserData, regTempl));

                    client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }

                #endregion

                #endregion
            }
            else
            {
                reader_user.Close();
                ErrorResponse(ErrorType.WrongLoginOrPassword, new Exception("Wrong login or password."));
            }
        }

        private void RefreshContacts(RefreshContactsTemplate refrC)
        {
            #region Получение из бд контактов юзера и запись в лист contacts

            SqlCommand contactsCommand = new SqlCommand("select ui.id, ui.app_name, ui.image, ui.status, ui.is_blocked " +
                                                        "from Users_info as ui " +
                                                        "join Contacts as c " +
                                                        "on ui.id = c.contact and c.contact_owner = @userID", null); //TODO: sql
            contactsCommand.Parameters.Add(new SqlParameter("@userID", refrC.ContactsOwner));
            SqlDataReader contactsReader = contactsCommand.ExecuteReader();

            List<Contact> contacts = new List<Contact>();
            if (contactsReader.HasRows)
            {
                #region Получение основной инфы о контактах
                Contact tempContact = null;
                while (contactsReader.Read())
                {
                    tempContact = new Contact();
                    tempContact.UserId = contactsReader.GetInt32(0);
                    tempContact.AppName = contactsReader.GetString(1);

                    object imageObj = contactsReader.GetValue(2);
                    if (imageObj is System.DBNull)
                    {
                        tempContact.AvatarImage = GetDefaultUserImage();
                    }
                    else
                    {
                        tempContact.AvatarImage = (byte[])imageObj;
                    }
                    tempContact.Status = (Status)Enum.Parse(typeof(Status), contactsReader.GetString(3));
                    tempContact.IsBlocked = contactsReader.GetBoolean(4);
                    contacts.Add(tempContact);
                }
                contactsReader.Close();
                #endregion

                #region Получение chat_id с каждым контактом
                SqlCommand command = new SqlCommand("select chat_id from Chats where (chat_title = @chatTitle_1 or chat_title = @chatTitle_2) and chat_kind = 'Chat' ", null); //TODO: sql
                string chatTitle_1;
                string chatTitle_2;
                SqlDataReader reader;

                foreach (Contact contact in contacts)
                {
                    chatTitle_1 = String.Format("{0}_{1}", refrC.ContactsOwner, contact.UserId);
                    chatTitle_2 = String.Format("{1}_{0}", refrC.ContactsOwner, contact.UserId);
                    command.Parameters.Add(new SqlParameter("@chatTitle_1", chatTitle_1));
                    command.Parameters.Add(new SqlParameter("@chatTitle_2", chatTitle_2));
                    reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        contact.ChatId = reader.GetInt32(0);
                    }
                    reader.Close();
                    command.Parameters.Clear();
                }
                #endregion

                #region Загрузка сообщений чата

                SqlCommand chatMessages;
                SqlDataReader chatMessagesReader;
                List<Message> tempMessages;
                Message tempMessage;

                foreach (Contact contact in contacts)
                {
                    chatMessages = new SqlCommand("select message_id, sender_id, message, message_type, file_name, time, message_status, app_name " +
                                                 $"from ChatMessages_{contact.ChatId} as c join Users_info as u on c.sender_id = u.id " +
                                                  "order by message_id desc", null); //TODO: sql
                    chatMessagesReader = chatMessages.ExecuteReader();
                    tempMessages = new List<Message>();
                    if (chatMessagesReader.HasRows)
                    {
                        while (chatMessagesReader.Read())
                        {
                            tempMessage = new Message();

                            tempMessage.MessageID = chatMessagesReader.GetInt32(0);
                            tempMessage.SenderID = chatMessagesReader.GetInt32(1);
                            //tempMessage.SenderName = contact.AppName;
                            tempMessage.MessageBody = (byte[])chatMessagesReader.GetValue(2);
                            tempMessage.MessageType = (MessageType)Enum.Parse(typeof(MessageType), chatMessagesReader.GetString(3));

                            switch (tempMessage.MessageType)
                            {
                                case MessageType.File:

                                    if (tempMessage.MessageBody.Length < 1000000) //если меньше мегабайта
                                    {
                                        tempMessage.FileSize = Convert.ToString(((float)tempMessage.MessageBody.Length / 1000) + " Kb");
                                    }
                                    else
                                    {
                                        if (tempMessage.MessageBody.Length < 1000000000) //если меньше гигабайта
                                        {
                                            tempMessage.FileSize = Convert.ToString(((float)tempMessage.MessageBody.Length / 1000000) + " Mb");
                                        }
                                        else
                                        {
                                            tempMessage.FileSize = Convert.ToString(((float)tempMessage.MessageBody.Length / 1000000000) + " Gb");
                                        }
                                    }
                                    tempMessage.FileName = chatMessagesReader.GetString(4);
                                    tempMessage.MessageBody = new byte[1];
                                    break;
                                case MessageType.Text:
                                    tempMessage.FileName = "";
                                    tempMessage.FileSize = "";
                                    break;
                                default:
                                    break;
                            }

                            tempMessage.Time = chatMessagesReader.GetDateTime(5);
                            tempMessage.MessageState = (MessageState)Enum.Parse(typeof(MessageState), chatMessagesReader.GetString(6));
                            tempMessage.SenderName = chatMessagesReader.GetString(7);

                            tempMessages.Add(tempMessage);
                        }
                    }
                    contact.Messages = tempMessages;
                    chatMessagesReader.Close();
                }

                #endregion
            }
            contactsReader.Close();
            #endregion

            #region Добавление в лист контактов всех бесед

            SqlCommand convCommand = new SqlCommand("select chat.chat_id, chat.chat_title, chat.chat_avatar " +
                                                    "from Chats as chat " +
                                                    "where chat.chat_id in (select cm.chat_id from ChatMembers cm where cm.member_id = @memberId) and chat.chat_kind = 'Conversation'", null); //TODO: sql
            convCommand.Parameters.Add(new SqlParameter("@memberId", refrC.ContactsOwner));
            SqlDataReader convReader = convCommand.ExecuteReader();
            if (convReader.HasRows)
            {
                List<Contact> conversations = new List<Contact>();

                #region Получение основной информации о беседах
                Contact tempConv;
                while (convReader.Read())
                {
                    tempConv = new Contact();
                    tempConv.ChatId = convReader.GetInt32(0);
                    tempConv.AppName = convReader.GetString(1);

                    object imageObj = convReader.GetValue(2);
                    if (imageObj is System.DBNull)
                    {
                        tempConv.AvatarImage = GetDefaultUserImage();
                    }
                    else
                    {
                        tempConv.AvatarImage = (byte[])imageObj;
                    }

                    tempConv.ContactType = ContactType.Conversation;
                    tempConv.Members = new List<Contact>();
                    conversations.Add(tempConv);
                    
                }
                convReader.Close();
                #endregion

                #region Получение информации о членах беседы
                convCommand = new SqlCommand("select ui.id, ui.app_name, ui.image, ui.status " +
                                             "from Users_info as ui " +
                                             "join ChatMembers as cm " +
                                             "on ui.id = cm.member_id and cm.chat_id = @chatId", null); //TODO: sql

                foreach (Contact conv in conversations)
                {
                    convCommand.Parameters.Add(new SqlParameter("@chatId", conv.ChatId));
                    using (SqlDataReader convMembersReader = convCommand.ExecuteReader())
                    {
                        while (convMembersReader.Read())
                        {
                            Contact member = new Contact();
                            member.UserId = convMembersReader.GetInt32(0);
                            member.AppName = convMembersReader.GetString(1);

                            object imageObj = convMembersReader.GetValue(2);
                            if (imageObj is System.DBNull)
                            {
                                member.AvatarImage = GetDefaultUserImage();
                            }
                            else
                            {
                                member.AvatarImage = (byte[])imageObj;
                            }
                            member.Status = (Status)Enum.Parse(typeof(Status), convMembersReader.GetString(3));
                            conv.Members.Add(member);
                        }
                    }

                    convCommand.Parameters.Clear();
                }

                
                #endregion

                #region Загрузка сообщений беседы

                SqlCommand chatMessages;
                SqlDataReader chatMessagesReader;
                List<Message> tempMessages;
                Message tempMessage;

                foreach (Contact conv in conversations)
                {
                    chatMessages = new SqlCommand("select message_id, sender_id, message, message_type, file_name, time, message_status " +
                                                 $"from ChatMessages_{conv.ChatId} " +
                                                  "order by message_id desc", null); //TODO: sql
                    chatMessagesReader = chatMessages.ExecuteReader();
                    tempMessages = new List<Message>();
                    if (chatMessagesReader.HasRows)
                    {
                        while (chatMessagesReader.Read())
                        {
                            tempMessage = new Message();

                            tempMessage.MessageID = chatMessagesReader.GetInt32(0);
                            tempMessage.SenderID = chatMessagesReader.GetInt32(1);
                            tempMessage.MessageBody = (byte[])chatMessagesReader.GetValue(2);
                            tempMessage.MessageType = (MessageType)Enum.Parse(typeof(MessageType), chatMessagesReader.GetString(3));

                            switch (tempMessage.MessageType) //TODO: change max size ON CLIENT
                            {
                                case MessageType.File:

                                    if (tempMessage.MessageBody.Length < 1000000) //если меньше мегабайта
                                    {
                                        tempMessage.FileSize = Convert.ToString(((float)tempMessage.MessageBody.Length / 1000) + " Kb");
                                    }
                                    else
                                    {
                                        if (tempMessage.MessageBody.Length < 1000000000) //если меньше гигабайта
                                        {
                                            tempMessage.FileSize = Convert.ToString(((float)tempMessage.MessageBody.Length / 1000000) + " Mb");
                                        }
                                        else
                                        {
                                            tempMessage.FileSize = Convert.ToString(((float)tempMessage.MessageBody.Length / 1000000000) + " Gb");
                                        }
                                    }
                                    tempMessage.FileName = chatMessagesReader.GetString(4);
                                    tempMessage.MessageBody = new byte[1];
                                    break;
                                case MessageType.Text:
                                    tempMessage.FileName = "";
                                    tempMessage.FileSize = "";
                                    break;
                                default:
                                    break;
                            }

                            tempMessage.Time = chatMessagesReader.GetDateTime(5);
                            tempMessage.MessageState = (MessageState)Enum.Parse(typeof(MessageState), chatMessagesReader.GetString(6));

                            tempMessages.Add(tempMessage);
                        }
                    }
                    conv.Messages = tempMessages;
                    chatMessagesReader.Close();
                }

                #endregion

                contacts.AddRange(conversations);
            }
            convReader.Close();
            #endregion

            #region Отправка контактов юзера или пустого листа 

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.RefreshContacts, new RefreshContactsTemplate(refrC.ContactsOwner, contacts)));

                ConectedUser user = Server.conectedUsers.Find(x => x.id == refrC.ContactsOwner);
                if (user != null)
                {
                    user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }
            }

            #endregion
            
            
        }

        //обновление Статуса пользователя (Online/Offline)
        private void RefreshClientListener(RefreshUserTemplate refrUser)
        {
            ConectedUser user = Server.conectedUsers.Find(x => x.id == refrUser.UserId);
            if (user == null)
            {
                Server.conectedUsers.Add(new ConectedUser(refrUser.UserId, client));
                Server.ChangeStatus(refrUser.UserId, Status.Online);
            }
            else
            {
                user.userSocket = client;
                Server.ChangeStatus(refrUser.UserId, Status.Online);
            }
        }

        //Регистрация пользователя
        private void Registration(RegistrationTemplate reg)
        {
            #region Проверка есть ли зарегестрированный пользователь с таким логином
            OracleCommand checkForAlreadyReg = new OracleCommand("katran_procedures.GetUserIdByLogin", sql);
            checkForAlreadyReg.CommandType = CommandType.StoredProcedure;
            checkForAlreadyReg.Parameters.Add(CreateParam("inAuthLogin", reg.Login, ParameterDirection.Input));
            checkForAlreadyReg.Parameters.Add(CreateParam("outFoundUserId", -1, ParameterDirection.Output));
            checkForAlreadyReg.ExecuteNonQuery();

            if ((int)checkForAlreadyReg.Parameters["outFoundUserId"].Value > 0)
            {
                ErrorResponse(ErrorType.UserAlreadyRegistr, new Exception("User with this login is already registered."));
                return;
            }
            #endregion

            #region Добавление в таблицу Users
            OracleCommand addUserCommand = new OracleCommand("katran_procedures.AddUser", sql);
            addUserCommand.CommandType = CommandType.StoredProcedure;
            addUserCommand.Parameters.Add(CreateParam("inAuthLogin", reg.Login, ParameterDirection.Input));
            addUserCommand.Parameters.Add(CreateParam("inPassword", reg.Password, ParameterDirection.Input));
            addUserCommand.Parameters.Add(CreateParam("inLawStatus", reg.LawStatus.ToString(), ParameterDirection.Input));
            addUserCommand.Parameters.Add(CreateParam("outAddedUserId", -1, ParameterDirection.Output));
            addUserCommand.ExecuteNonQuery();


            if ((reg.Id = (int)addUserCommand.Parameters["outAddedUserId"].Value) < 0)
            {
                ErrorResponse(ErrorType.UserAlreadyRegistr, new Exception("Error adding a new user to the database, registration."));
                return;
            }

            #endregion

            #region Добавление в таблицу Users_info
            OracleCommand addUserInfoCommand = new OracleCommand("katran_procedures.AddUserInfo", sql);
            addUserInfoCommand.CommandType = CommandType.StoredProcedure;
            addUserInfoCommand.Parameters.Add(CreateParam("inId", reg.Id, ParameterDirection.Input));
            addUserInfoCommand.Parameters.Add(CreateParam("inAppName", reg.App_name, ParameterDirection.Input));
            addUserInfoCommand.Parameters.Add(CreateParam("inEmail", reg.Email, ParameterDirection.Input));
            addUserInfoCommand.Parameters.Add(CreateParam("inUserDescription", reg.User_discription, ParameterDirection.Input));

            if (reg.Image == null || reg.Image.Length == 0)
                addUserInfoCommand.Parameters.Add(CreateParam("inImage", DBNull.Value, ParameterDirection.Input, OracleDbType.Blob));
            else
                addUserInfoCommand.Parameters.Add(CreateParam("inImage", reg.Image, ParameterDirection.Input, OracleDbType.Blob));

            addUserInfoCommand.Parameters.Add(CreateParam("inStatus", reg.Status.ToString(), ParameterDirection.Input));
            addUserInfoCommand.Parameters.Add(CreateParam("outAddedUserId", -1, ParameterDirection.Output));
            addUserInfoCommand.ExecuteNonQuery();
            if ((int)addUserInfoCommand.Parameters["outAddedUserId"].Value < 0)
            {
                ErrorResponse(ErrorType.Other, new Exception("Registration error, user info wasn't added"));
                return;
            }
            #endregion

            #region Ответ об успешной регистрации
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                reg.Status = Status.Online;
                if (reg.Image == null)
                {
                    reg.Image = GetDefaultUserImage();
                }
                
                formatter.Serialize(memoryStream, new RRTemplate(RRType.Authorization, reg));

                client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
            }
            #endregion
        }

        //Авторизация пользователя
        void Authtorization(AuthtorizationTemplate auth)
        {
            #region Проверка введенного логина и пароля с хранящимися данными в бд
            OracleCommand checkUserByLoginAndPassword = new OracleCommand("katran_procedures.CheckUserByLoginAndPassword", sql);
            checkUserByLoginAndPassword.CommandType = CommandType.StoredProcedure;
            checkUserByLoginAndPassword.Parameters.Add(CreateParam("@inAuthLogin", auth.AuthLogin, ParameterDirection.Input));
            checkUserByLoginAndPassword.Parameters.Add(CreateParam("@inPassword", auth.Password, ParameterDirection.Input));
            checkUserByLoginAndPassword.Parameters.Add(CreateParam("@outAddedUserId", -1, ParameterDirection.Output));
            checkUserByLoginAndPassword.ExecuteNonQuery();
            #endregion
            //если пользователь с таким логином и паролем существует
            if ((int)checkUserByLoginAndPassword.Parameters["@outAddedUserId"].Value > 0)
            {
                #region Запрос на данные о пользователе
                OracleCommand getUserInfo = new OracleCommand("katran_procedures.GetUserInfoById", sql);
                getUserInfo.CommandType = CommandType.StoredProcedure;
                getUserInfo.Parameters.Add(CreateParam("@in_outUserId", (int)checkUserByLoginAndPassword.Parameters["@outAddedUserId"].Value, ParameterDirection.InputOutput));
                getUserInfo.Parameters.Add(CreateParam("@in_outAppName", new string('\0', 200), ParameterDirection.InputOutput));
                getUserInfo.Parameters.Add(CreateParam("@in_outEmail", new string('\0', 200), ParameterDirection.InputOutput));
                getUserInfo.Parameters.Add(CreateParam("@in_outUserDescription", new string('\0', 200), ParameterDirection.InputOutput));
                getUserInfo.Parameters.Add(CreateParam("@in_outImage", new OracleBlob(sql), ParameterDirection.InputOutput, OracleDbType.Blob));
                getUserInfo.Parameters.Add(CreateParam("@in_outStatus", new string('\0', 200), ParameterDirection.InputOutput));
                getUserInfo.Parameters.Add(CreateParam("@in_outLawStatus", new string('\0', 200), ParameterDirection.InputOutput));
                getUserInfo.Parameters.Add(CreateParam("@in_outIsBlocked", 1, ParameterDirection.InputOutput));
                getUserInfo.ExecuteNonQuery();
                #endregion

                #region Отправка ответа на запрос авторизации

                #region Обработка случая если картинка не пользователя задана, то ставится стандартная
                OracleBlob imageObj = (OracleBlob)getUserInfo.Parameters["@in_outImage"].Value;
                byte[] image;
                if (imageObj.IsNull)
                {
                    image = GetDefaultUserImage();
                }
                else
                {
                    image = imageObj.Value;
                }
                #endregion

                
                RegistrationTemplate regTempl = new RegistrationTemplate(
                    (int)getUserInfo.Parameters["@in_outUserId"].Value,
                    (string)getUserInfo.Parameters["@in_outAppName"].Value,
                    (string)getUserInfo.Parameters["@in_outEmail"].Value,
                    (string)getUserInfo.Parameters["@in_outUserDescription"].Value,
                    image,
                    (Status)Enum.Parse(typeof(Status), (string)getUserInfo.Parameters["@in_outStatus"].Value),
                    (LawStatus)Enum.Parse(typeof(LawStatus), (string)getUserInfo.Parameters["@in_outLawStatus"].Value),
                    (int)getUserInfo.Parameters["@in_outIsBlocked"].Value != 0,
                    auth.AuthLogin, auth.Password);

                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    regTempl.Status = Status.Online;
                    formatter.Serialize(memoryStream, new RRTemplate(RRType.Authorization, regTempl));

                    client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }

                #endregion

            }
            else
            {
                ErrorResponse(ErrorType.WrongLoginOrPassword, new Exception("Wrong login or password."));
            }
        }

        //Создание параметра
        public static OracleParameter CreateParam(string paramName, object value, System.Data.ParameterDirection direction, OracleDbType dbType = OracleDbType.Single)
        {
            OracleParameter retParam = new OracleParameter(paramName, value);
            retParam.Direction = direction;
            if (dbType != OracleDbType.Single)
            {
                retParam.OracleDbType = dbType;
            }
            return retParam;
        }

        //Возвращает стандартную аватарку для юзера
        byte[] GetDefaultUserImage()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Properties.Resources.defaultUserImage.Save(ms, ImageFormat.Png);
                return ms.GetBuffer();
            }
        }

        //Отправка пользователю уведомления об ошибке
        void ErrorResponse(ErrorType errorType, Exception ex)
        {
            if (client.Connected)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    formatter.Serialize(memoryStream, new RRTemplate(RRType.Error, new ErrorReportTemplate(errorType, ex)));

                    try
                    {
                        client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                    }
                    catch (Exception exc)
                    {
                        return;
                    }
                }
            }
        }
    }
}