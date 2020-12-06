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
                                if (aContT != null)
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
            OracleCommand getUserLawStatusCommand = new OracleCommand("katran_procedures.GetUserLawStatusById", sql);
            getUserLawStatusCommand.CommandType = CommandType.StoredProcedure;
            getUserLawStatusCommand.Parameters.Add(CreateParam("inUserId", bunbUT.AdminId, ParameterDirection.InputOutput));
            getUserLawStatusCommand.Parameters.Add(CreateParam("outUserLawStatus", new string('\0', 5), ParameterDirection.InputOutput));
            getUserLawStatusCommand.ExecuteNonQuery();
            LawStatus userLawStatus = (LawStatus)Enum.Parse(typeof(LawStatus), (string)getUserLawStatusCommand.Parameters["outUserLawStatus"].Value);
            if (userLawStatus == LawStatus.Admin)
            {
                OracleCommand blockUnblockUserCommand = new OracleCommand("katran_procedures.BlockUnblockUser", sql);
                blockUnblockUserCommand.CommandType = CommandType.StoredProcedure;
                blockUnblockUserCommand.Parameters.Add(CreateParam("in_outUserId", bunbUT.UserId, ParameterDirection.InputOutput));
                blockUnblockUserCommand.Parameters.Add(CreateParam("inIsBlocked", bunbUT.IsBlocked ? 1 : 0, ParameterDirection.Input));
                blockUnblockUserCommand.ExecuteNonQuery();

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
            OracleCommand adminSearchCommand = new OracleCommand("katran_procedures.AdminSearch", sql);
            adminSearchCommand.CommandType = CommandType.StoredProcedure;
            adminSearchCommand.Parameters.Add(CreateParam("outResult", null, ParameterDirection.Output, OracleDbType.RefCursor)); //курсор должен быть на первом месте в параметрах
            adminSearchCommand.Parameters.Add(CreateParam("inAdminId", admST.AdminId, ParameterDirection.Input));
            adminSearchCommand.Parameters.Add(CreateParam("inSearchPattern", admST.Pattern, ParameterDirection.Input));
            
            adminSearchCommand.ExecuteNonQuery();

            OracleDataAdapter orclDataAdapter = new OracleDataAdapter(adminSearchCommand);
            DataSet orclDataSet = new DataSet();
            orclDataAdapter.Fill(orclDataSet);

            List<Contact> contacts = new List<Contact>();
            Contact tempContact = null;
            foreach (DataRow row in orclDataSet.Tables[0].Rows)
            {
                tempContact = new Contact();
                tempContact.UserId = Convert.ToInt32(row.ItemArray[0]);
                tempContact.AppName = (string)row.ItemArray[1];
                tempContact.AvatarImage = ObjectToByteArray(row.ItemArray[2]);
                tempContact.Status = (Status)Enum.Parse(typeof(Status), (string)row.ItemArray[3]);
                tempContact.IsBlocked = Convert.ToInt32(row.ItemArray[4]) > 0;
                contacts.Add(tempContact);
            }

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
            #region Удаление из таблицы chatMembers юзера из беседы

            OracleCommand removeChatMemberCommand = new OracleCommand("katran_procedures.RemoveChatMember", sql);
            removeChatMemberCommand.CommandType = CommandType.StoredProcedure;
            removeChatMemberCommand.Parameters.Add(CreateParam("inChatId", rconvT.ChatId, ParameterDirection.Input));
            removeChatMemberCommand.Parameters.Add(CreateParam("inMemberId", rconvT.OwnerId, ParameterDirection.Input));
            removeChatMemberCommand.ExecuteNonQuery();

            #endregion

            #region Удаление чата и сообщений если участников беседы больше нет
            OracleCommand getChatMembersCountCommand = new OracleCommand("katran_procedures.GetChatMembersCount", sql);
            getChatMembersCountCommand.CommandType = CommandType.StoredProcedure;
            getChatMembersCountCommand.Parameters.Add(CreateParam("inChatId", rconvT.ChatId, ParameterDirection.Input));
            getChatMembersCountCommand.Parameters.Add(CreateParam("outMembersCount", 0, ParameterDirection.Output));
            getChatMembersCountCommand.ExecuteNonQuery();

            if ((int)getChatMembersCountCommand.Parameters["outMembersCount"].Value == 0)
            {
                DropChatMessagesTable(rconvT.ChatId);

                OracleCommand removeChat = new OracleCommand("katran_procedures.RemoveChat", sql);
                removeChat.CommandType = CommandType.StoredProcedure;
                removeChat.Parameters.Add(CreateParam("in_outChatId", rconvT.ChatId, ParameterDirection.InputOutput));
                removeChat.ExecuteNonQuery();
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
            #region Оповещение других юзеров, что пользователь вышел из чата
            OracleCommand getChatmembersIdsCommand = new OracleCommand("katran_procedures.GetChatMembersIds", sql);
            getChatmembersIdsCommand.CommandType = CommandType.StoredProcedure;
            getChatmembersIdsCommand.Parameters.Add(CreateParam("outChatMembersIds", null, ParameterDirection.Output, OracleDbType.RefCursor)); //курсор должен быть на первом месте в параметрах
            getChatmembersIdsCommand.Parameters.Add(CreateParam("inChatId", rconvT.ChatId, ParameterDirection.Input));
            getChatmembersIdsCommand.ExecuteNonQuery();

            OracleDataAdapter orclDataAdapter = new OracleDataAdapter(getChatmembersIdsCommand);
            DataSet orclDataSet = new DataSet();
            orclDataAdapter.Fill(orclDataSet);

            foreach (DataRow row in orclDataSet.Tables[0].Rows)
            {
                using (MemoryStream memoryStream = new MemoryStream()) //Уведомление других юзеров о его выходе
                {
                    formatter.Serialize(memoryStream, new RRTemplate(RRType.RemoveConvTarget, rconvT));
                    ConectedUser user;

                    user = Server.conectedUsers.Find(x => x.id == Convert.ToInt32(row.ItemArray[0]));
                    if (user != null)
                    {
                        user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                    }
                }
            }
            #endregion

            #endregion
        }

        private void CreateConv(CreateConvTemplate crconvT)
        {
            #region Добавление чата в бд для беседы
            OracleCommand addConvCommand = new OracleCommand("katran_procedures.AddChat", sql);
            addConvCommand.CommandType = CommandType.StoredProcedure;
            addConvCommand.Parameters.Add(CreateParam("out_AddedChatId", -1, ParameterDirection.Output));
            addConvCommand.Parameters.Add(CreateParam("inChatTitle", crconvT.Title, ParameterDirection.Input));
            addConvCommand.Parameters.Add(CreateParam("inChatKind", ContactType.Conversation.ToString(), ParameterDirection.Input));

            if (crconvT.Image == null || crconvT.Image.Length == 0)
                addConvCommand.Parameters.Add(CreateParam("inImage", DBNull.Value, ParameterDirection.Input, OracleDbType.Blob));
            else
                addConvCommand.Parameters.Add(CreateParam("inImage", crconvT.Image, ParameterDirection.Input, OracleDbType.Blob));

            addConvCommand.ExecuteNonQuery();
            #endregion

            crconvT.ChatId = (int)addConvCommand.Parameters["out_AddedChatId"].Value;

            #region Добавляем в таблицу ChatMembers участников беседы

            OracleCommand addChatMemberCommand = new OracleCommand("katran_procedures.AddChatMember", sql);
            addChatMemberCommand.CommandType = CommandType.StoredProcedure;

            foreach (Contact c in crconvT.ConvMembers)
            {
                addChatMemberCommand.Parameters.Add(CreateParam("inChatId", crconvT.ChatId, ParameterDirection.Input));
                addChatMemberCommand.Parameters.Add(CreateParam("in_outMemberId", c.UserId, ParameterDirection.InputOutput));
                addChatMemberCommand.ExecuteNonQuery();

                addChatMemberCommand.Parameters.Clear();
            }
            #endregion

            #region Создание таблицу с сообщениями для только что созданного чата
            CreateChatMessagesTable(crconvT.ChatId);
            #endregion

            #region Отправка созданной беседы всем ее участникам (заполнение инфы о ее членах)
            OracleCommand getUserInfo = new OracleCommand("katran_procedures.GetUserInfoById", sql);
            getUserInfo.CommandType = CommandType.StoredProcedure;
            foreach (Contact c in crconvT.ConvMembers)
            {
                ConectedUser u = Server.conectedUsers.Find(x => x.id == c.UserId);
                if (Server.conectedUsers.Find(x => x.id == c.UserId) != null)
                {
                    RegistrationTemplate userInfo = GetUserInfo(c.UserId);

                    c.AppName = userInfo.App_name;
                    c.AvatarImage = userInfo.Image;
                    c.Status = userInfo.Status;

                    getUserInfo.Parameters.Clear();
                }
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
            using (OracleCommand command = new OracleCommand($"update katran_admin.ChatMessages_{rmessT.ChatId} set message_status = :status where message_id = :id", sql))
            {
                if (rmessT.MessageState == MessageState.Sended)
                {
                    rmessT.MessageState = MessageState.Readed;
                }

                command.Parameters.Add(new OracleParameter("status", rmessT.MessageState.ToString()));
                command.Parameters.Add(new OracleParameter("id", rmessT.messageId));
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
            OracleCommand getChatmembersIdsCommand = new OracleCommand("katran_procedures.GetChatMembersIds", sql);
            getChatmembersIdsCommand.CommandType = CommandType.StoredProcedure;
            getChatmembersIdsCommand.Parameters.Add(CreateParam("outChatMembersIds", null, ParameterDirection.Output, OracleDbType.RefCursor)); //курсор должен быть на первом месте в параметрах
            getChatmembersIdsCommand.Parameters.Add(CreateParam("inChatId", rmessT.ChatId, ParameterDirection.Input));
            getChatmembersIdsCommand.ExecuteNonQuery();

            OracleDataAdapter orclDataAdapter = new OracleDataAdapter(getChatmembersIdsCommand);
            DataSet orclDataSet = new DataSet();
            orclDataAdapter.Fill(orclDataSet);

            ConectedUser user;
            int chatMemberId;
            foreach (DataRow row in orclDataSet.Tables[0].Rows)
            {
                chatMemberId = Convert.ToInt32(row.ItemArray[0]);

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
            #endregion
        }

        private void DownloatFile(DownloadFileTemplate dfileT)
        {
            OracleCommand command = new OracleCommand($"select message, file_name from katran_admin.ChatMessages_{dfileT.ChatId} where message_id = :messageId", sql);
            command.Parameters.Add(new OracleParameter("messageId", dfileT.messageId));
            OracleDataReader reader = command.ExecuteReader();
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
            OracleCommand getChatmembersIdsCommand = new OracleCommand("katran_procedures.GetChatMembersIds", sql);
            getChatmembersIdsCommand.CommandType = CommandType.StoredProcedure;
            getChatmembersIdsCommand.Parameters.Add(CreateParam("outChatMembersIds", null, ParameterDirection.Output, OracleDbType.RefCursor)); //курсор должен быть на первом месте в параметрах
            getChatmembersIdsCommand.Parameters.Add(CreateParam("inChatId", sMessT.ReceiverChatID, ParameterDirection.Input));
            getChatmembersIdsCommand.ExecuteNonQuery();

            OracleDataAdapter orclDataAdapter = new OracleDataAdapter(getChatmembersIdsCommand);
            DataSet orclDataSet = new DataSet();
            orclDataAdapter.Fill(orclDataSet);

            List<int> chatMembers = new List<int>();
            foreach (DataRow row in orclDataSet.Tables[0].Rows)
            {
                chatMembers.Add(Convert.ToInt32(row.ItemArray[0]));
            }

            #endregion

            #region Отправляем сообщение в бд и обновляем его messageID and MessageState 

            OracleCommand sendMessageCommand = new OracleCommand($"insert into katran_admin.ChatMessages_{sMessT.ReceiverChatID} (sender_id, message, message_type, file_name, time, message_status) " +
                                                                  "values(:sender_id, :message, :message_type, :file_name, :time, :message_status)", sql);
            sendMessageCommand.Parameters.Add(new OracleParameter("sender_id", sMessT.Message.SenderID));
            sendMessageCommand.Parameters.Add(CreateParam("message", sMessT.Message.MessageBody, ParameterDirection.Input, OracleDbType.Blob));
            sendMessageCommand.Parameters.Add(new OracleParameter("message_type", sMessT.Message.MessageType.ToString()));
            sendMessageCommand.Parameters.Add(new OracleParameter("file_name", sMessT.Message.FileName));
            sendMessageCommand.Parameters.Add(new OracleParameter("time", sMessT.Message.Time));
            sendMessageCommand.Parameters.Add(new OracleParameter("message_status", MessageState.Sended.ToString()));
            sendMessageCommand.ExecuteNonQuery();

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

            OracleCommand command = new OracleCommand("select message_id, app_name " +
                                                     $"from katran_admin.ChatMessages_{sMessT.ReceiverChatID} c join katran_admin.users_info u on c.sender_id = u.id " +
                                                     $"where sender_id = :sender_id and time = to_date('{sMessT.Message.Time.ToString("yyyy-MM-dd HH:mm:ss")}', 'YYYY-MM-DD HH24:MI:SS')", sql);
            command.Parameters.Add(new OracleParameter("sender_id", sMessT.Message.SenderID));
            OracleDataReader reader = command.ExecuteReader();

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
            OracleCommand deleteContact = new OracleCommand("katran_procedures.RemoveContact", sql);
            deleteContact.CommandType = CommandType.StoredProcedure;
            deleteContact.Parameters.Add(CreateParam("in_outContactOwner", rContT.ContactOwnerId, ParameterDirection.InputOutput));
            deleteContact.Parameters.Add(CreateParam("inTargetContact", rContT.TargetContactId, ParameterDirection.Input));
            deleteContact.ExecuteNonQuery();

            deleteContact.Parameters.Clear();

            deleteContact.Parameters.Add(CreateParam("in_outContactOwner", rContT.TargetContactId, ParameterDirection.InputOutput));
            deleteContact.Parameters.Add(CreateParam("inTargetContact", rContT.ContactOwnerId, ParameterDirection.Input));
            deleteContact.ExecuteNonQuery();

            #endregion

            #region Удаление чата и сообщений этих юзеров из бд
            if (rContT.ChatId != -1)
            {
                DropChatMessagesTable(rContT.ChatId);

                OracleCommand removeChatAllMembers = new OracleCommand("katran_procedures.RemoveAllChatMembers", sql);
                removeChatAllMembers.CommandType = CommandType.StoredProcedure;
                removeChatAllMembers.Parameters.Add(CreateParam("inChatId", rContT.ChatId, ParameterDirection.Input));
                removeChatAllMembers.ExecuteNonQuery();

                OracleCommand removeChat = new OracleCommand("katran_procedures.RemoveChat", sql);
                removeChat.CommandType = CommandType.StoredProcedure;
                removeChat.Parameters.Add(CreateParam("in_outChatId", rContT.ChatId, ParameterDirection.InputOutput));
                removeChat.ExecuteNonQuery();
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
            addChatCommand.Parameters.Add(CreateParam("out_AddedChatId", -1, ParameterDirection.Output));
            addChatCommand.Parameters.Add(CreateParam("inChatTitle", chatTitle, ParameterDirection.Input));
            addChatCommand.ExecuteNonQuery();
            arContT.ChatId = (int)addChatCommand.Parameters["out_AddedChatId"].Value;
            #endregion

            if (arContT.ChatId > 0)
            {
                #region Добавляем в таблицу СhatMembers юзеров
                OracleCommand addChatMemberCommand = new OracleCommand("katran_procedures.AddChatMember", sql);
                addChatMemberCommand.CommandType = CommandType.StoredProcedure;
                addChatMemberCommand.Parameters.Add(CreateParam("inChatId", arContT.ChatId, ParameterDirection.Input));
                addChatMemberCommand.Parameters.Add(CreateParam("in_outMemberId", arContT.ContactOwnerId, ParameterDirection.InputOutput));
                addChatMemberCommand.ExecuteNonQuery();

                addChatMemberCommand.Parameters.Clear();

                addChatMemberCommand.Parameters.Add(CreateParam("inChatId", arContT.ChatId, ParameterDirection.Input));
                addChatMemberCommand.Parameters.Add(CreateParam("in_outMemberId", arContT.TargetContactId, ParameterDirection.InputOutput));
                addChatMemberCommand.ExecuteNonQuery();
                #endregion

                #region Создание таблицу с сообщениями для только что созданного чата
                CreateChatMessagesTable(arContT.ChatId);
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

                        RegistrationTemplate userInfo = GetUserInfo(newContact.UserId);

                        newContact.AppName = userInfo.App_name;
                        newContact.AvatarImage = userInfo.Image;
                        newContact.IsBlocked = userInfo.IsBlocked;

                        formatter.Serialize(memoryStream, new RRTemplate(RRType.AddContactTarget, new AddRemoveContactTargetTemplate(newContact)));
                        user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                    }

                }
                #endregion
            }

        }

        //поиск контактов вне контактов пользователя по поисковому запросу
        private void SearchOutContacts(SearchOutContactsTemplate searchOutC)
        {
            #region Отправка запроса на поиск контактов по паттерну вне контактов и запись их в лист contacts
            OracleCommand searchOutContactsCommand = new OracleCommand("katran_procedures.SearchOutOfContacts", sql);
            searchOutContactsCommand.CommandType = CommandType.StoredProcedure;
            searchOutContactsCommand.Parameters.Add(CreateParam("outResult", null, ParameterDirection.Output, OracleDbType.RefCursor)); //курсор должен быть на первом месте в параметрах
            searchOutContactsCommand.Parameters.Add(CreateParam("inPattern", searchOutC.SearchPattern, ParameterDirection.Input));
            searchOutContactsCommand.Parameters.Add(CreateParam("inSearcherUserID", searchOutC.ContactsOwner, ParameterDirection.Input));

            searchOutContactsCommand.ExecuteNonQuery();

            OracleDataAdapter orclDataAdapter = new OracleDataAdapter(searchOutContactsCommand);
            DataSet orclDataSet = new DataSet();
            orclDataAdapter.Fill(orclDataSet);

            List<Contact> contacts = new List<Contact>();
            Contact tempContact = null;
            foreach (DataRow row in orclDataSet.Tables[0].Rows)
            {
                tempContact = new Contact();
                tempContact.UserId = Convert.ToInt32(row.ItemArray[0]);
                tempContact.AppName = (string)row.ItemArray[1];
                tempContact.AvatarImage = ObjectToByteArray(row.ItemArray[2]);
                contacts.Add(tempContact);
            }
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

                Server.ChangeStatus(id, Status.Online);
                RegistrationTemplate regTempl = GetUserInfo(id);
                regTempl.Login = refrUserData.AuthLogin;
                regTempl.Password = password;
                #endregion

                #region Отправка ответа на запрос авторизации

                #region Отправка ответа на запрос обновление данных о пользователе

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
            OracleCommand getContactsCommand = new OracleCommand("katran_procedures.GetUserContacts", sql);
            getContactsCommand.CommandType = CommandType.StoredProcedure;
            getContactsCommand.Parameters.Add(CreateParam("outResult", null, ParameterDirection.Output, OracleDbType.RefCursor)); //курсор должен быть на первом месте в параметрах
            getContactsCommand.Parameters.Add(CreateParam("inContactsOwner", refrC.ContactsOwner, ParameterDirection.Input));
            getContactsCommand.ExecuteNonQuery();

            OracleDataAdapter orclDataAdapter_getContactsCommand = new OracleDataAdapter(getContactsCommand);
            DataSet orclDataSet_getContactsCommand = new DataSet();
            orclDataAdapter_getContactsCommand.Fill(orclDataSet_getContactsCommand);

            List<Contact> contacts = new List<Contact>();
            Contact tempContact = null;
            foreach (DataRow row in orclDataSet_getContactsCommand.Tables[0].Rows)
            {
                tempContact = new Contact();
                tempContact.UserId = Convert.ToInt32(row.ItemArray[0]);
                tempContact.AppName = (string)row.ItemArray[1];
                tempContact.AvatarImage = ObjectToByteArray(row.ItemArray[2]);
                tempContact.Status = (Status)Enum.Parse(typeof(Status), (string)row.ItemArray[3]);
                tempContact.IsBlocked = Convert.ToInt32(row.ItemArray[4]) > 0;
                contacts.Add(tempContact);
            }


            #endregion

            #region Получение chat_id с каждым контактом
            OracleCommand command = new OracleCommand("select chat_id from katran_admin.chats where (chat_title = :chatTitle_1 or chat_title = :chatTitle_2) and chat_kind = 'Chat' ", sql);
            string chatTitle_1;
            string chatTitle_2;
            OracleDataReader reader;

            foreach (Contact contact in contacts)
            {
                chatTitle_1 = String.Format("{0}_{1}", refrC.ContactsOwner, contact.UserId);
                chatTitle_2 = String.Format("{1}_{0}", refrC.ContactsOwner, contact.UserId);
                command.Parameters.Add(new OracleParameter("chatTitle_1", chatTitle_1));
                command.Parameters.Add(new OracleParameter("chatTitle_2", chatTitle_2));
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

            foreach (Contact contact in contacts)
            {
                contact.Messages = GetChatMessages(contact.ChatId);
            }

            #endregion

            #region Добавление в лист контактов всех бесед

            #region Получение основной информации о беседах
            OracleCommand getConvsInfoCommand = new OracleCommand("katran_procedures.GetConvsInfo", sql);
            getConvsInfoCommand.CommandType = CommandType.StoredProcedure;
            getConvsInfoCommand.Parameters.Add(CreateParam("outResult", null, ParameterDirection.Output, OracleDbType.RefCursor)); //курсор должен быть на первом месте в параметрах
            getConvsInfoCommand.Parameters.Add(CreateParam("inMemberId", refrC.ContactsOwner, ParameterDirection.Input));
            getConvsInfoCommand.ExecuteNonQuery();

            OracleDataAdapter orclDataAdapter_getConvsInfoCommand = new OracleDataAdapter(getConvsInfoCommand);
            DataSet orclDataSet_getConvsInfoCommand = new DataSet();
            orclDataAdapter_getConvsInfoCommand.Fill(orclDataSet_getConvsInfoCommand);

            #endregion
            if (orclDataSet_getConvsInfoCommand.Tables[0].Rows.Count > 0)
            {
                List<Contact> conversations = new List<Contact>();
                Contact tempConv;
                foreach (DataRow row in orclDataSet_getConvsInfoCommand.Tables[0].Rows)
                {
                    tempConv = new Contact();
                    tempConv.ChatId = Convert.ToInt32(row.ItemArray[0]);
                    tempConv.AppName = (string)row.ItemArray[1];
                    tempConv.AvatarImage = ObjectToByteArray(row.ItemArray[2]);
                    tempConv.ContactType = ContactType.Conversation;
                    tempConv.Members = new List<Contact>();
                    conversations.Add(tempConv);
                }

                #region Получение информации о членах беседы

                OracleCommand getConvMembersInfoCommand = new OracleCommand("katran_procedures.GetConvMembersInfo", sql);
                getConvMembersInfoCommand.CommandType = CommandType.StoredProcedure;

                foreach (Contact conv in conversations)
                {
                    getConvMembersInfoCommand.Parameters.Add(CreateParam("outResult", null, ParameterDirection.Output, OracleDbType.RefCursor)); //курсор должен быть на первом месте в параметрах
                    getConvMembersInfoCommand.Parameters.Add(CreateParam("inChatId", conv.ChatId, ParameterDirection.Input));
                    getConvMembersInfoCommand.ExecuteNonQuery();

                    OracleDataAdapter orclDataAdapter_getConvMembersInfoCommand = new OracleDataAdapter(getConvMembersInfoCommand);
                    DataSet orclDataSet_getConvMembersInfoCommand = new DataSet();
                    orclDataAdapter_getConvMembersInfoCommand.Fill(orclDataSet_getConvMembersInfoCommand);

                    foreach (DataRow row in orclDataSet_getConvMembersInfoCommand.Tables[0].Rows)
                    {
                        Contact member = new Contact();
                        member.UserId = Convert.ToInt32(row.ItemArray[0]);
                        member.AppName = (string)row.ItemArray[1];
                        member.AvatarImage = ObjectToByteArray(row.ItemArray[2]);
                        member.Status = (Status)Enum.Parse(typeof(Status), (string)row.ItemArray[3]);
                        conv.Members.Add(member);
                    }

                    getConvMembersInfoCommand.Parameters.Clear();
                }
                #endregion

                #region Загрузка сообщений беседы

                foreach (Contact conv in conversations)
                {
                    conv.Messages = GetChatMessages(conv.ChatId);
                }

                #endregion

                contacts.AddRange(conversations);
            }
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
            checkUserByLoginAndPassword.Parameters.Add(CreateParam("inAuthLogin", auth.AuthLogin, ParameterDirection.Input));
            checkUserByLoginAndPassword.Parameters.Add(CreateParam("inPassword", auth.Password, ParameterDirection.Input));
            checkUserByLoginAndPassword.Parameters.Add(CreateParam("outAddedUserId", -1, ParameterDirection.Output));
            checkUserByLoginAndPassword.ExecuteNonQuery();
            #endregion
            //если пользователь с таким логином и паролем существует
            if ((int)checkUserByLoginAndPassword.Parameters["outAddedUserId"].Value > 0)
            {
                #region Запрос на данные о пользователе
                RegistrationTemplate regTempl = GetUserInfo((int)checkUserByLoginAndPassword.Parameters["outAddedUserId"].Value);
                #endregion

                #region Отправка ответа на запрос авторизации
                regTempl.Login = auth.AuthLogin;
                regTempl.Password = auth.Password;

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

        static bool CreateChatMessagesTable(int chatId)
        {
            bool successCreation = false;
            if (chatId > 0)
            {
                OracleConnection adminConnection = OracleDB.GetDBConnection(true);
                adminConnection.Open();
                OracleCommand createTableCommand = new OracleCommand($"create table ChatMessages_{chatId} " +
                            "( message_id int generated as identity primary key," +
                            "  sender_id int," +
                            "  message blob," +
                            "  message_type varchar2(4) check(message_type in ('File', 'Text'))," +
                            "  file_name varchar2(200)," +
                            "  time date," +
                            "  message_status varchar2(8) check(message_status in ('Readed', 'Sended', 'Unreaded', 'Unsended'))) tablespace katran_tablespace ", adminConnection);

                createTableCommand.ExecuteNonQuery();
                adminConnection.Close();
                successCreation = true;
            }
            else
            {
                successCreation = false;
            }

            return successCreation;
        }

        static void DropChatMessagesTable(int chatId)
        {
            OracleConnection adminConnection = OracleDB.GetDBConnection(true);
            adminConnection.Open();
            OracleCommand dropTableCommand = new OracleCommand($"drop table ChatMessages_{chatId}", adminConnection);
            dropTableCommand.ExecuteNonQuery();
            adminConnection.Close();
        }

        //перевод того, что приходит с бд в массив байт для передачи пользователю
        static byte[] ObjectToByteArray(object paramObject)
        {
            byte[] retImage;
            OracleBlob blobImage = paramObject as OracleBlob;
            if (blobImage == null)
            {
                retImage = paramObject as byte[];
                if (retImage == null)
                { 
                    retImage = GetDefaultUserImage();
                }
            }
            else
            {
                retImage = blobImage.Value;
            }

            return retImage;
        }

        //Возвращает стандартную аватарку для юзера
        static byte[] GetDefaultUserImage()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Properties.Resources.defaultUserImage.Save(ms, ImageFormat.Png);
                return ms.GetBuffer();
            }
        }

        static List<Message> GetChatMessages(int chatId)
        {
            OracleConnection adminConnection = OracleDB.GetDBConnection(true);
            adminConnection.Open();
            OracleCommand chatMessagesCommand = new OracleCommand("select message_id, sender_id, message, message_type, file_name, time, message_status, app_name " +
                                                                 $"from ChatMessages_{chatId} c join Users_info u on c.sender_id = u.id " +
                                                                  "order by message_id desc", adminConnection);
            OracleDataReader chatMessagesReader = chatMessagesCommand.ExecuteReader();
            List<Message> convTempMessages = new List<Message>();
            
            if (chatMessagesReader.HasRows)
            {
                Message tempMessage;
                while (chatMessagesReader.Read())
                {
                    tempMessage = new Message();

                    tempMessage.MessageID = chatMessagesReader.GetInt32(0);
                    tempMessage.SenderID = chatMessagesReader.GetInt32(1);
                    tempMessage.MessageBody = ObjectToByteArray(chatMessagesReader.GetValue(2));
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

                    convTempMessages.Add(tempMessage);
                }
            }
            chatMessagesReader.Close();
            adminConnection.Close();

            return convTempMessages;
        }

        static RegistrationTemplate GetUserInfo(int userId)
        {
            sql = OracleDB.GetDBConnection();
            sql.Open();
            OracleCommand getUserInfo = new OracleCommand("katran_procedures.GetUserInfoById", sql);
            getUserInfo.CommandType = CommandType.StoredProcedure;

            getUserInfo.Parameters.Add(CreateParam("in_outUserId", userId, ParameterDirection.InputOutput));
            getUserInfo.Parameters.Add(CreateParam("in_outAppName", new string('\0', 200), ParameterDirection.InputOutput));
            getUserInfo.Parameters.Add(CreateParam("in_outEmail", new string('\0', 200), ParameterDirection.InputOutput));
            getUserInfo.Parameters.Add(CreateParam("in_outUserDescription", new string('\0', 200), ParameterDirection.InputOutput));
            getUserInfo.Parameters.Add(CreateParam("in_outImage", new OracleBlob(sql), ParameterDirection.InputOutput, OracleDbType.Blob));
            getUserInfo.Parameters.Add(CreateParam("in_outStatus", new string('\0', 200), ParameterDirection.InputOutput));
            getUserInfo.Parameters.Add(CreateParam("in_outLawStatus", new string('\0', 200), ParameterDirection.InputOutput));
            getUserInfo.Parameters.Add(CreateParam("in_outIsBlocked", 1, ParameterDirection.InputOutput));
            getUserInfo.ExecuteNonQuery();

            RegistrationTemplate regTempl = new RegistrationTemplate(
                    (int)getUserInfo.Parameters["in_outUserId"].Value,
                    (string)getUserInfo.Parameters["in_outAppName"].Value,
                    (string)getUserInfo.Parameters["in_outEmail"].Value,
                    (string)getUserInfo.Parameters["in_outUserDescription"].Value,
                    ObjectToByteArray(getUserInfo.Parameters["in_outImage"].Value),
                    (Status)Enum.Parse(typeof(Status), (string)getUserInfo.Parameters["in_outStatus"].Value),
                    (LawStatus)Enum.Parse(typeof(LawStatus), (string)getUserInfo.Parameters["in_outLawStatus"].Value),
                    (int)getUserInfo.Parameters["in_outIsBlocked"].Value != 0,
                    "", "");

            sql.Close();
            return regTempl;
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