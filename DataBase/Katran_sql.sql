--Oracle database set up file
alter session set "_ORACLE_SCRIPT"=true;

------------------------------------------------------------------------------------------------------------------
-- tablespaces
create tablespace katran_tablespace
    datafile 'C:\katran_db\tablespaces\katran_tablespace.dbf'
    size 15 m
    autoextend on next 1m
    maxsize unlimited;

create temporary tablespace katran_tablespace_temp
    tempfile 'C:\katran_db\tablespaces\katran_tablespace_temp.dbf'
    size 15 m
    autoextend on next 1m
    maxsize unlimited;

create undo tablespace katran_tablespace_undo
    datafile 'C:\katran_db\tablespaces\katran_tablespace_undo.dbf'
    size 5 m
    autoextend on next 1m
    maxsize unlimited;
------------------------------------------------------------------------------------------------------------------

------------------------------------------------------------------------------------------------------------------
-- security profiles
create profile katran_admin_security_profile limit
    password_life_time 180 -- кол-во дней жизни пароля
    sessions_per_user unlimited  -- кол-во сессий для пользователя
    failed_login_attempts 7 -- кол-во попыток входа
    password_lock_time 1 -- кол-во дней блокировки после ощибки
    password_reuse_time 1 -- через сколько дней можно повторить пароль
    password_grace_time DEFAULT -- кол-во дней предупреждений о смене пароля
    connect_time unlimited -- время соединения
    idle_time 60; -- минут простоя перед отключением

create profile katran_user_security_profile limit
    password_life_time 180 -- кол-во дней жизни пароля
    sessions_per_user unlimited -- кол-во сессий для пользователя
    failed_login_attempts 7 -- кол-во попыток входа
    password_lock_time 1 -- кол-во дней блокировки после ощибки
    password_reuse_time 1 -- через сколько дней можно повторить пароль
    password_grace_time DEFAULT -- кол-во дней предупреждений о смене пароля
    connect_time unlimited -- время соединения
    idle_time 60; -- минут простоя перед отключением
------------------------------------------------------------------------------------------------------------------

------------------------------------------------------------------------------------------------------------------
-- roles
create role katran_admin_role;
grant execute on sys.dbms_crypto to katran_admin_role; -- sys
grant all privileges to katran_admin_role;

create role katran_user_role;
grant create session,
      read any table,
      select any table,
      insert any table,
      update any table,
      execute any procedure,
      delete any table to katran_user_role;

grant execute on sys.dbms_crypto to katran_user_role; -- sys
grant execute on katran_package to katran_user_role;
grant debug on katran_package to katran_user_role;
------------------------------------------------------------------------------------------------------------------

------------------------------------------------------------------------------------------------------------------
-- users
create user katran_admin identified by qw2SF24xvGGS
default tablespace katran_tablespace quota unlimited on katran_tablespace
temporary tablespace katran_tablespace_temp
profile katran_admin_security_profile
account unlock;

grant katran_admin_role to katran_admin;

create user katran_user identified by fu6djHH763
default tablespace katran_tablespace quota unlimited on katran_tablespace
temporary tablespace katran_tablespace_temp
profile katran_user_security_profile
account unlock;

grant katran_user_role to katran_user;

------------------------------------------------------------------------------------------------------------------

------------------------------------------------------------------------------------------------------------------
-- tables

create table users
(
	id int generated as identity constraint users_id_pk primary key,
	auth_login varchar2(20) unique,
	password varchar2(32) not null,
    law_status varchar2(5) default 'User' check(law_status in('Admin', 'User'))
) tablespace katran_tablespace
partition by range (id) interval (1000)
( partition part_1 values less than (1000) ) enable row movement;

create table users_info
(
	id int constraint users_info_users_id_fk references users(id) on delete cascade enable,
	app_name varchar2(20),
	email varchar2(50),
	user_description varchar2(200),
	image blob,
	status varchar2(8) default 'Offline' check(status in('Offline', 'Online')),
	is_blocked number(1) default 0
) tablespace katran_tablespace
partition by range (id) interval (1000)
( partition part_1 values less than (1000) ) enable row movement;

create table contacts
(
	contact_owner int constraint contacts_contact_owner_id_fk references users(id),
	contact int constraint contacts_contact_id_fk references users(id)
) tablespace katran_tablespace
partition by range (contact_owner) interval (1000)
( partition part_1 values less than (1000) ) enable row movement;

create table chats
(
    chat_id int generated as identity constraint chats_chat_id_pk primary key,
    chat_title varchar2(40) unique, --для чатов 1 на 1 паттерн названия [id]_[id], для бесед [название беседы]
    chat_kind varchar2(12) default 'Chat' check(chat_kind in('Chat', 'Conversation')),
    chat_avatar blob
) tablespace katran_tablespace
partition by range (chat_id) interval (1000)
( partition part_1 values less than (1000) ) enable row movement;

create table chat_members
(
    chat_id int constraint chat_members_chat_id_fk references chats (chat_id),
    member_id int constraint chat_members_member_id_fk references users (id)
) tablespace katran_tablespace
partition by range (chat_id) interval (1000)
( partition part_1 values less than (1000) ) enable row movement;

/* template for dynamic tableof messages for each Chat creating
create table ChatMessages_{chatId}
(
    message_id int generated as identity primary key,
    sender_id int references users(id),
    message blob,
    message_type varchar2(4) check(message_type in ('File', 'Text')),
    file_name varchar2(200),
    time date,
    message_status varchar2(8) check(message_status in ('Readed', 'Sended', 'Unreaded', 'Unsended'))
) tablespace katran_tablespace
partition by range (message_id) interval (1000)
subpartition by hash(message_id) subpartitions 10
( partition part_1 values less than (1000) ) enable row movement;
*/

------------------------------------------------------------------------------------------------------------------

------------------------------------------------------------------------------------------------------------------
-- procedures and functions

create or replace package katran_package AS

    procedure GetUserIdByLogin(inAuthLogin in users.auth_login%type,
                               outFoundUserId out users.id%type);

    procedure AddUser(inAuthLogin in users.auth_login%type,
                      inPassword in users.password%type,
                      inLawStatus in users.law_status%type,
                      outAddedUserId out users.id%type);

    procedure AddUserInfo(inId in users_info.id%type,
                          inAppName in users_info.app_name%type,
                          inEmail in users_info.email%type,
                          inUserDescription in users_info.user_description%type,
                          inImage in users_info.image%type,
                          inStatus in users_info.status%type,
                          outAddedUserId out users_info.id%type);

    procedure CheckUserByLoginAndPassword(inAuthLogin in users.auth_login%type,
                                          inPassword in users.password%type,
                                          outFoundUserId out users.id%type);

    procedure GetUserInfoById(in_outUserId in out int,
                              in_outAppName in out varchar2,
                              in_outEmail in out varchar2,
                              in_outUserDescription in out varchar2,
                              in_outImage in out blob,
                              in_outStatus in out varchar2,
                              in_outLawStatus in out varchar2,
                              in_outIsBlocked in out number);

    procedure ChangeUserStatusById( inNewUserStatus in users_info.status%type,
                                    in_outUserId in out users_info.id%type);

    procedure AddContact( in_outContactOwner in out users.id%type,
                          inTargetContact in users.id%type);

    procedure RemoveContact( in_outContactOwner in out users.id%type,
                             inTargetContact in users.id%type);

    procedure AddChat( out_AddedChatId out chats.chat_id%type,
                       inChatTitle in chats.chat_title%type,
                       inChatKind in chats.chat_kind%type default 'Chat',
                       inChatAvatar in chats.chat_avatar%type default null);

    procedure AddChatMember( inChatId in chat_members.chat_id%type,
                             in_outMemberId in out chat_members.member_id%type);

    procedure RemoveChatMember( inChatId in chat_members.chat_id%type,
                                inMemberId in chat_members.member_id%type);

    procedure RemoveAllChatMembers( inChatId in chat_members.chat_id%type);

    procedure GetChatMembersCount( inChatId in chat_members.chat_id%type,
                                   outMembersCount out number);

    procedure GetChatMembersIds( outChatMembersIds out sys_refcursor,
                                 inChatId in chat_members.chat_id%type);

    procedure RemoveChat( in_outChatId in out chat_members.chat_id%type);

    procedure GetConvsInfo( outResult out sys_refcursor,
                                  inMemberId in chat_members.member_id%type);

    procedure GetConvMembersInfo( outResult out sys_refcursor,
                                  inChatId in chat_members.member_id%type);

    procedure BlockUnblockUser( in_outUserId in out users_info.id%type,
                                inIsBlocked in users_info.is_blocked%type);

    procedure GetUserLawStatusById( in_outUserId in out users.id%type,
                                    in_outUserLawStatus in out varchar2);

    procedure AdminSearch( outResult out sys_refcursor,
                           inAdminId in users_info.id%type,
                           inSearchPattern in varchar2);

    procedure SearchOutOfContacts(outResult out sys_refcursor,
                                  inPattern in varchar2,
                                  inSearcherUserID in users_info.id%type);

    procedure GetUserContacts(outResult out sys_refcursor,
                              inContactsOwner in users_info.id%type);

    function EncryptBlob (enc_value in blob,
                          crypt_key in varchar2)
    return blob;  -- should be 24 symbols length

    function DecryptBlob (enc_value in blob,
                          crypt_key in varchar2)
    return blob;  -- should be 24 symbols length

END;

create or replace package body katran_package as

------------------------------------------------- Is user already registered

    procedure GetUserIdByLogin( inAuthLogin in users.auth_login%type,
                                outFoundUserId out users.id%type)
    as
    begin
        select id into outFoundUserId from users where users.auth_login = inAuthLogin and ROWNUM <= 1;
    exception
        when no_data_found then
            outFoundUserId := -1;
    end;

------------------------------------------------- Is user already registered

------------------------------------------------- AddUser

    procedure AddUser(inAuthLogin in users.auth_login%type,
                      inPassword in users.password%type,
                      inLawStatus in users.law_status%type,
                      outAddedUserId out users.id%type)
    as
    begin

        insert into users (auth_login, password, law_status)
        values (inAuthLogin, inPassword, inLawStatus)
        returning  id into outAddedUserId;
        commit;
    exception
        when others then
            outAddedUserId := -1;
            rollback;
    end;

------------------------------------------------- AddUserInfo

    procedure AddUserInfo(inId in users_info.id%type,
                          inAppName in users_info.app_name%type,
                          inEmail in users_info.email%type,
                          inUserDescription in users_info.user_description%type,
                          inImage in users_info.image%type,
                          inStatus in users_info.status%type,
                          outAddedUserId out users_info.id%type)
    as
    begin
        insert into users_info (id, app_name, email, user_description, image, status)
        values(inId, inAppName, inEmail, inUserDescription, inImage, inStatus)
        returning  id into outAddedUserId;
        commit;
    exception
        when others then
            outAddedUserId := -1;
            rollback;
    end;

------------------------------------------------- AddUserInfo

------------------------------------------------- CheckUserByLoginAndPassword

    procedure CheckUserByLoginAndPassword(inAuthLogin in users.auth_login%type,
                                          inPassword in users.password%type,
                                          outFoundUserId out users.id%type)
    as
    begin
        select id into outFoundUserId from users where auth_login = inAuthLogin and inPassword = inPassword;
    exception
        when others then
            outFoundUserId := -1;
    end;

------------------------------------------------- CheckUserByLoginAndPassword

------------------------------------------------- GetUserInfoById

    procedure GetUserInfoById(in_outUserId in out int,
                              in_outAppName in out varchar2,
                              in_outEmail in out varchar2,
                              in_outUserDescription in out varchar2,
                              in_outImage in out blob,
                              in_outStatus in out varchar2,
                              in_outLawStatus in out varchar2,
                              in_outIsBlocked in out number)
    as
        cursor query_cursor is
        select ui.id, ui.app_name, ui.email, ui.user_description, ui.image, ui.status, u.law_status, ui.is_blocked
        from users_info ui join users u on ui.id = u.id where ui.id = in_outUserId;
    begin
        open query_cursor;
            fetch query_cursor into in_outUserId, in_outAppName, in_outEmail, in_outUserDescription, in_outImage, in_outStatus, in_outLawStatus, in_outIsBlocked;
        close query_cursor;
    exception
        when others then
            in_outUserId := -1;
    end;

------------------------------------------------- GetUserInfoById

------------------------------------------------- ChangeUserStatusById

    procedure ChangeUserStatusById( inNewUserStatus in users_info.status%type,
                                    in_outUserId in out users_info.id%type)
    as
    begin
        update users_info set status = inNewUserStatus where id = in_outUserId
        returning id into in_outUserId;
        commit;
    exception
        when others then
            in_outUserId := -1;
            rollback;
    end;

------------------------------------------------- ChangeUserStatusById

------------------------------------------------- AddContact

    procedure AddContact( in_outContactOwner in out users.id%type,
                          inTargetContact in users.id%type)
    as
    begin

        insert into contacts (contact_owner, contact)
        values(in_outContactOwner, inTargetContact);
        commit;
        insert into contacts (contact_owner, contact)
        values(inTargetContact, in_outContactOwner);
        commit;
    exception
        when others then
            in_outContactOwner := -1;
            rollback;
    end;

------------------------------------------------- AddContact

------------------------------------------------- RemoveContact

    procedure RemoveContact( in_outContactOwner in out users.id%type,
                             inTargetContact in users.id%type)
    as
    begin
        delete from contacts where contact_owner = in_outContactOwner and contact = inTargetContact;
        commit;
    exception
        when others then
            in_outContactOwner := -1;
            rollback;
    end;

------------------------------------------------- RemoveContact

------------------------------------------------- AddChat

    procedure AddChat( out_AddedChatId out chats.chat_id%type,
                       inChatTitle in chats.chat_title%type,
                       inChatKind in chats.chat_kind%type default 'Chat',
                       inChatAvatar in chats.chat_avatar%type default null)
    as
    begin

        insert into chats (chat_title, chat_kind, chat_avatar)
        values(inChatTitle, inChatKind, inChatAvatar)
        returning chat_id into out_AddedChatId;
        commit;
    exception
        when others then
            out_AddedChatId := -1;
            rollback;
    end;

------------------------------------------------- AddChat

------------------------------------------------- AddChatMember

    procedure AddChatMember( inChatId in chat_members.chat_id%type,
                             in_outMemberId in out chat_members.member_id%type)
    as
    begin

        insert into chat_members (chat_id, member_id)
        values(inChatId, in_outMemberId)
        returning member_id into in_outMemberId;
        commit;
    exception
        when others then
            in_outMemberId := -1;
            rollback;
    end;

------------------------------------------------- AddChatMember

------------------------------------------------- RemoveChatMember

    procedure RemoveChatMember( inChatId in chat_members.chat_id%type,
                                inMemberId in chat_members.member_id%type)
    as
    begin
        delete from chat_members where chat_id = inChatId and member_id = inMemberId;
        commit;
    end;

------------------------------------------------- RemoveChatMember

------------------------------------------------- RemoveAllChatMembers

    procedure RemoveAllChatMembers( inChatId in chat_members.chat_id%type)
    as
    begin
        delete from chat_members where chat_id = inChatId;
        commit;
    end;

------------------------------------------------- RemoveAllChatMembers

------------------------------------------------- GetChatMembersCount

    procedure GetChatMembersCount( inChatId in chat_members.chat_id%type,
                                   outMembersCount out number)
    as
    begin
        select count(chat_id) into outMembersCount from chat_members where chat_id = inChatId;
    exception
        when others then
            outMembersCount := 0;
    end;

------------------------------------------------- GetChatMembersCount

------------------------------------------------- GetChatMembersIds

    procedure GetChatMembersIds( outChatMembersIds out sys_refcursor,
                                 inChatId in chat_members.chat_id%type)
    as
    begin
        open outChatMembersIds for
        select member_id from chat_members where chat_id = inChatId;
    end;

------------------------------------------------- GetChatMembersIds

------------------------------------------------- RemoveChat

    procedure RemoveChat( in_outChatId in out chat_members.chat_id%type)
    as
    begin
        delete from chats where chat_id = in_outChatId;
        commit;
    exception
        when others then
            in_outChatId := -1;
            rollback;
    end;

------------------------------------------------- RemoveChat

------------------------------------------------- GetConvsInfo

    procedure GetConvsInfo( outResult out sys_refcursor,
                            inMemberId in chat_members.member_id%type)
    as
    begin
        open outResult for
        select chat.chat_id, chat.chat_title, chat.chat_avatar
        from Chats chat
        where chat.chat_id in (select cm.chat_id from chat_members cm where cm.member_id = inMemberId) and chat.chat_kind = 'Conversation';
    end;

------------------------------------------------- GetConvsInfo

------------------------------------------------- GetConvMembersInfo

    procedure GetConvMembersInfo( outResult out sys_refcursor,
                                  inChatId in chat_members.member_id%type)
    as
    begin
        open outResult for
        select ui.id, ui.app_name, ui.image, ui.status
        from Users_info ui join chat_members cm
        on ui.id = cm.member_id and cm.chat_id = inChatId;
    end;

------------------------------------------------- GetConvMembersInfo

------------------------------------------------- GetUserLawStatusById

    procedure GetUserLawStatusById( in_outUserId in out users.id%type,
                                    in_outUserLawStatus in out varchar2)
    as
    begin
        select law_status into in_outUserLawStatus from users where id = in_outUserId;
    exception
        when others then
            in_outUserLawStatus := '';
    end;

------------------------------------------------- GetUserLawStatusById

------------------------------------------------- BlockUnblockUser

    procedure BlockUnblockUser( in_outUserId in out users_info.id%type,
                                inIsBlocked in users_info.is_blocked%type)
    as
    begin
        update users_info set is_blocked = inIsBlocked where id = in_outUserId;
        commit;
    exception
        when others then
            in_outUserId := -1;
            rollback;
    end;

------------------------------------------------- BlockUnblockUser

------------------------------------------------- AdminSearch

    procedure AdminSearch( outResult out sys_refcursor,
                           inAdminId in users_info.id%type,
                           inSearchPattern in varchar2)
    as
    begin
        open outResult for
        select ui.id, ui.app_name, ui.image, ui.status, ui.is_blocked
        from Users_info ui
        where instr(ui.app_name, inSearchPattern) > 0 and ui.id != inAdminId;
    end;

------------------------------------------------- AdminSearch

------------------------------------------------- SearchOutOfContacts

    procedure SearchOutOfContacts(outResult out sys_refcursor,
                                  inPattern in varchar2,
                                  inSearcherUserID in users_info.id%type)
    as
    begin
        open outResult for
        select ui.id, ui.app_name, ui.image, ui.status, ui.is_blocked
        from Users_info ui
        where instr(ui.app_name, inPattern) > 0 and ui.id != inSearcherUserID and
        ui.id NOT IN(select ui.id from Users_info ui join Contacts c on ui.id = c.contact and c.contact_owner = inSearcherUserID);
    end;

------------------------------------------------- SearchOutOfContacts

------------------------------------------------- GetUserContacts

    procedure GetUserContacts(outResult out sys_refcursor,
                              inContactsOwner in users_info.id%type)
    as
    begin
        open outResult for
        select ui.id, ui.app_name, ui.image, ui.status, ui.is_blocked
        from Users_info ui
        join Contacts c
        on ui.id = c.contact and c.contact_owner = inContactsOwner;
    end;

------------------------------------------------- GetUserContacts

------------------------------------------------- Crypto

    function EncryptBlob (enc_value in blob,
                          crypt_key in varchar2)  -- should be 24 symbols length
    return blob
    is
        encrypted_blob blob;
        l_mod number := dbms_crypto.encrypt_aes192 + dbms_crypto.chain_cbc + dbms_crypto.pad_pkcs5;
    begin
        encrypted_blob := dbms_crypto.encrypt(enc_value, l_mod, utl_i18n.string_to_raw(crypt_key, 'AL32UTF8'));
    return encrypted_blob;
    end EncryptBlob;

    function DecryptBlob (enc_value in blob,
                          crypt_key in varchar2) -- should be 24 symbols length
    return blob
    is
        decrypted_blob blob;
        l_mod number := dbms_crypto.encrypt_aes192 + dbms_crypto.chain_cbc + dbms_crypto.pad_pkcs5;
    begin
        decrypted_blob := dbms_crypto.decrypt(enc_value, l_mod, utl_i18n.string_to_raw(crypt_key, 'AL32UTF8'));
    return decrypted_blob;
    end DecryptBlob;

------------------------------------------------- Crypto

end katran_package;
------------------------------------------------------------------------------------------------------------------