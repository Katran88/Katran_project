﻿using KatranClassLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace KatranServer
{
    class ClientService
    {
        private TcpClient client;
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
                }
                
            }
        }

        private void RemoveContact(AddRemoveContactTemplate rContT)
        {
            #region Удаление из таблицы контактов 
            SqlCommand command = new SqlCommand("delete from Contacts where contact_owner = @contactOwner and contact = @targetContact " +
                                                "delete from Contacts where contact_owner = @targetContact and contact = @contactOwner", Server.sql);
            command.Parameters.Add(new SqlParameter("@contactOwner", rContT.ContactOwnerId));
            command.Parameters.Add(new SqlParameter("@targetContact", rContT.TargetContactId));
            command.ExecuteNonQuery();
            #endregion

            #region Удаление чата и сообщений этих юзеров из бд 
            command = new SqlCommand("select chat_id from Chats where chat_title = @chatTitle_1 or chat_title = @chatTitle_2", Server.sql);
            string chatTitle_1 = String.Format("{0}_{1}", rContT.ContactOwnerId, rContT.TargetContactId);
            string chatTitle_2 = String.Format("{1}_{0}", rContT.ContactOwnerId, rContT.TargetContactId);
            command.Parameters.Add(new SqlParameter("@chatTitle_1", chatTitle_1));
            command.Parameters.Add(new SqlParameter("@chatTitle_2", chatTitle_2));
            SqlDataReader reader = command.ExecuteReader();
            int chatID = -1;
            if(reader.HasRows)
            {
                reader.Read();
                chatID = reader.GetInt32(0);
            }
            reader.Close();

            if (chatID != -1)
            {
                command = new SqlCommand($"drop table ChatMessages_{chatID} " +
                                          "delete from Chats where chat_id = @chatID", Server.sql);
                command.Parameters.Add(new SqlParameter("@chatID", chatID));
                command.ExecuteNonQuery();
            }
            #endregion

            #region Ответ об успешном удалении юзера из контактов
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.RemoveContact, new AddRemoveContactTemplate(rContT.ContactOwnerId, rContT.TargetContactId)));

                ConectedUser user = Server.conectedUsers.Find(x => x.id == rContT.ContactOwnerId);
                if (user != null)
                {
                    user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }
            }
            #endregion

        }

        private void AddContact(AddRemoveContactTemplate arContT)
        {
            #region Добавление в таблицу контактов 
            SqlCommand command = new SqlCommand("insert into Contacts (contact_owner, contact) values(@contactOwner, @targetContact), (@targetContact, @contactOwner)", Server.sql);
            command.Parameters.Add(new SqlParameter("@contactOwner", arContT.ContactOwnerId));
            command.Parameters.Add(new SqlParameter("@targetContact", arContT.TargetContactId));
            command.ExecuteNonQuery();
            #endregion

            #region Добавление чата в бд для этих юзеров
            string chatTitle = String.Format("{0}_{1}", arContT.ContactOwnerId, arContT.TargetContactId);
            command = new SqlCommand("insert into Chats (chat_title) values (@chatTitle)", Server.sql);
            command.Parameters.Add(new SqlParameter("@chatTitle", chatTitle));
            command.ExecuteNonQuery();
            #endregion

            #region Получаем Id только что добавленного чата и записываем в chatId
            SqlCommand getChatID = new SqlCommand("select chat_id from Chats where chat_title = @chatTitle", Server.sql);
            getChatID.Parameters.Add(new SqlParameter("@chatTitle", chatTitle));
            SqlDataReader reader = getChatID.ExecuteReader();
            reader.Read();
            int chatId = reader.GetInt32(0);
            reader.Close();
            #endregion
            

            #region Создание таблицы с сообщениями для только что созданного чата
            command = new SqlCommand($"create table {String.Format("ChatMessages_{0}", chatId)} " +
                                    "( message varbinary(MAX)," +
                                    " message_type varchar(4) check(message_type in ('File', 'Text'))," +
                                    " time smalldatetime," +
                                    " message_status varchar(8) check(message_status in ('Readed', 'Sended', 'Unreaded', 'Unsended')))", Server.sql);
            command.ExecuteNonQuery();
            #endregion

            #region Отправка добавленного контакта и уведомление сокету, что все прошло успешно

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.AddContact, new AddRemoveContactTemplate(arContT.ContactOwnerId, arContT.TargetContactId)));

                ConectedUser user = Server.conectedUsers.Find(x => x.id == arContT.ContactOwnerId);
                if (user != null)
                {
                    user.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }
            }

            #endregion

        }

        //поиск контактов вне контактов пользователя по поисковому запросу
        private void SearchOutContacts(SearchOutContactsTemplate searchOutC)
        {
            #region Отправка запроса на поискконтактов по паттерну вне контактов и запись их в лист contacts
            SqlCommand command = new SqlCommand("select ui.id, ui.app_name, ui.image, ui.status " +
                                                "from Users_info as ui " +
                                                "where charindex(@pattern, ui.app_name) > 0 and ui.id != @userID and " +
                                                "ui.id NOT IN(select ui.id from Users_info as ui join Contacts as c on ui.id = c.contact and c.contact_owner = @userID)", Server.sql);
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
                        tempContact.AvatarImage = (byte[])imageObj;
                    }
                    tempContact.Status = (Status)Enum.Parse(typeof(Status), reader.GetString(3));

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
            SqlCommand command = new SqlCommand("select ui.id, ui.app_name, ui.email, ui.user_discription, ui.image, ui.status, u.law_status, u.password " +
                                                "from Users_info ui join Users u on ui.id = u.id " +
                                                "where ui.id = (select u2.id from Users as u2 where u2.auth_login = @auth_login)", Server.sql);
            command.Parameters.Add(new SqlParameter("@auth_login", refrUserData.AuthLogin));

            SqlDataReader reader_User_info = command.ExecuteReader();
            reader_User_info.Read();
            #endregion

            #region Обработка случая если картинка пользователя задана, то ставится стандартная
            object imageObj = reader_User_info.GetValue(4);
            byte[] image;
            if (imageObj is System.DBNull)
            {
                image = GetDefaultUserImage();
            }
            else
            {
                image = (byte[])imageObj;
            }
            #endregion

            #region Отправка ответа на запрос обновление данных о пользователе
            RegistrationTemplate regTempl = new RegistrationTemplate(
                (int)reader_User_info.GetValue(0),
                (string)reader_User_info.GetValue(1),
                (string)reader_User_info.GetValue(2),
                (string)reader_User_info.GetValue(3),
                image,
                (Status)Enum.Parse(typeof(Status), reader_User_info.GetString(5)),
                (LawStatus)Enum.Parse(typeof(LawStatus), reader_User_info.GetString(6)),
                refrUserData.AuthLogin,
                (string)reader_User_info.GetValue(7));

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.RefreshUserData, regTempl));

                client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
            }

            reader_User_info.Close();
            #endregion
        }

        private void RefreshContacts(RefreshContactsTemplate refrC)
        {
            #region Получение из бд контактов юзера и запись в лист contacts

            SqlCommand contactsCommand = new SqlCommand("select ui.id, ui.app_name, ui.image, ui.status " +
                                                        "from Users_info as ui " +
                                                        "join Contacts as c " +
                                                        "on ui.id = c.contact and c.contact_owner = @userID", Server.sql);
            contactsCommand.Parameters.Add(new SqlParameter("@userID", refrC.ContactsOwner));
            SqlDataReader contactsReader = contactsCommand.ExecuteReader();

            List<Contact> contacts = new List<Contact>();
            if (contactsReader.HasRows)
            {
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

                    contacts.Add(tempContact);
                }
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
            
            contactsReader.Close();
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
            }
        }

        //Регистрация пользователя
        private void Registration(RegistrationTemplate reg)
        {
            #region Проверка есть ли зарегестрированный пользователь с таким логином
            SqlCommand checkForAlreadyReg = new SqlCommand("select * from Users where auth_login = @auth_login", Server.sql);
            checkForAlreadyReg.Parameters.Add(new SqlParameter("@auth_login", reg.Login));
            SqlDataReader checkForAlreadyRegReader = checkForAlreadyReg.ExecuteReader();
            if (checkForAlreadyRegReader.HasRows)
            {
                ErrorResponse(ErrorType.UserAlreadyRegistr, new Exception("Пользователь с таким логином уже существует"));
                checkForAlreadyRegReader.Close();
                return;
            }
            checkForAlreadyRegReader.Close();
            #endregion

            #region Добавление в таблицу Users
            SqlCommand commandToUsers = new SqlCommand("insert into Users (auth_login, password, law_status) " +
                                                       "values (@auth_login, @password, @law_status)", Server.sql);
            commandToUsers.Parameters.Add(new SqlParameter("@auth_login", reg.Login));
            commandToUsers.Parameters.Add(new SqlParameter("@password", reg.Password));
            commandToUsers.Parameters.Add(new SqlParameter("@law_status", reg.LawStatus.ToString()));
            commandToUsers.ExecuteNonQuery();
            #endregion

            #region Добавление в таблицу Users_info
            
            //Получаем Id только что добавленного юзера
            SqlCommand command = new SqlCommand("select id from Users where auth_login = @auth_login", Server.sql);
            command.Parameters.Add(new SqlParameter("@auth_login", reg.Login));
            SqlDataReader userIdReader = command.ExecuteReader();

            userIdReader.Read();
            reg.Id = (int)userIdReader["id"];
            userIdReader.Close();

            SqlCommand commandToUsersInfo = new SqlCommand("insert into Users_info (id, app_name, email, user_discription, image, status) " +
                                                           "values(@id, @app_name, @email, @user_discription, @image, @status)", Server.sql);
            commandToUsersInfo.Parameters.Add(new SqlParameter("@id", reg.Id));
            commandToUsersInfo.Parameters.Add(new SqlParameter("@app_name", reg.App_name));
            commandToUsersInfo.Parameters.Add(new SqlParameter("@email", reg.Email));
            commandToUsersInfo.Parameters.Add(new SqlParameter("@user_discription", reg.User_discription));
            if (reg.Image == null || reg.Image.Length == 0)
                commandToUsersInfo.Parameters.Add("@image", SqlDbType.VarBinary).Value = DBNull.Value;
            else
                commandToUsersInfo.Parameters.Add(new SqlParameter("@image", reg.Image));
            commandToUsersInfo.Parameters.Add(new SqlParameter("@status", reg.Status.ToString()));
            if (commandToUsersInfo.ExecuteNonQuery() == 0)
            {
                ErrorResponse(ErrorType.Other, new Exception("Ошибка регистрации"));
                return;
            }
            #endregion

            #region Ответ об успешной регистрации
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.Authorization, reg));

                client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
            }
            #endregion
        }

        //Авторизация пользователя
        void Authtorization(AuthtorizationTemplate auth)
        {
            #region Проверка введенного логина и пароля с хранящимися данными в бд
            SqlCommand command = new SqlCommand("select id, law_status from Users where auth_login = @auth_login and password = @password", Server.sql);
            command.Parameters.Add(new SqlParameter("@auth_login", auth.AuthLogin));
            command.Parameters.Add(new SqlParameter("@password", auth.Password));
            SqlDataReader reader_Users = command.ExecuteReader();
            #endregion
            
            //если пользователь с таким логином и паролем существует
            if (reader_Users.HasRows)
            {
                #region Запрос на данные о пользователе
                reader_Users.Read();
                command = new SqlCommand("select ui.id, ui.app_name, ui.email, ui.user_discription, ui.image, ui.status, u.law_status from Users_info ui, Users u where ui.id = @id", Server.sql);
                command.Parameters.Add(new SqlParameter("@id", (int)reader_Users.GetValue(0)));
                reader_Users.Close();
                SqlDataReader reader_Users_info = command.ExecuteReader();
                reader_Users_info.Read();
                #endregion               

                #region Обработка случая если картинка пользователя задана, то ставится стандартная
                object imageObj = reader_Users_info.GetValue(4);
                byte[] image;
                if (imageObj is System.DBNull)
                {
                    image = GetDefaultUserImage();
                }
                else
                {
                    image = (byte[])imageObj;
                }
                #endregion

                #region Отправка ответа на запрос авторизации
                RegistrationTemplate regTempl = new RegistrationTemplate(
                    (int)reader_Users_info.GetValue(0),
                    (string)reader_Users_info.GetValue(1),
                    (string)reader_Users_info.GetValue(2),
                    (string)reader_Users_info.GetValue(3),
                    image,
                    (Status)Enum.Parse(typeof(Status), reader_Users_info.GetString(5)),
                    (LawStatus)Enum.Parse(typeof(LawStatus), reader_Users_info.GetString(6)),
                    auth.AuthLogin, auth.Password);

                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    formatter.Serialize(memoryStream, new RRTemplate(RRType.Authorization, regTempl));

                    client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }

                reader_Users_info.Close();
                #endregion
                
            }
            else 
            {
                reader_Users.Close();
                ErrorResponse(ErrorType.WrongLoginOrPassword, new Exception("Неверный логин или пароль"));
            }
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
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, new RRTemplate(RRType.Error, new ErrorReportTemplate(errorType, ex)));

                client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
            }
        }
    }
}
