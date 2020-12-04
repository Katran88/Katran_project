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
grant all privileges to katran_admin_role;

create role katran_user_role;
grant create session,
      read any table,
      select any table,
      insert any table,
      update any table,
      execute any procedure,
      delete any table to katran_user_role;

grant execute on katran_procedures to katran_user_role;
grant debug on katran_procedures to katran_user_role;

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
) tablespace katran_tablespace;

create table users_info
(
	id int constraint users_info_users_id_fk references users(id) on delete cascade enable,
	app_name varchar2(20),
	email varchar2(50),
	user_description varchar2(200),
	image blob,
	status varchar2(8) default 'Offline' check(status in('Offline', 'Online')),
	is_blocked number(1) default 0
) tablespace katran_tablespace;

create table contacts
(
	contact_owner int constraint contacts_contact_owner_id_fk references users(id),
	contact int constraint contacts_contact_id_fk references users(id)
) tablespace katran_tablespace;

create table chats
(
    chat_id int generated as identity constraint chats_chat_id_pk primary key,
    chat_title varchar2(40) unique, --для чатов 1 на 1 паттерн названия [id]_[id], для бесед [название беседы]
    chat_kind varchar2(12) default 'Chat' check(chat_kind in('Chat', 'Conversation')),
    chat_avatar blob
) tablespace katran_tablespace;

create table chat_members
(
    chat_id int constraint chat_members_chat_id_fk references chats (chat_id),
    member_id int constraint chat_members_member_id_fk references users (id)
) tablespace katran_tablespace;

/* template for dynamic tableof messages for each Chat creating
create table ChatMessages_{chat_id}
( message_id int generated as identity primary key,
    sender_id int,
    message blob,
    message_type varchar2(4) check(message_type in ('File', 'Text')),
    file_name varchar2(200),
    time date,
    message_status varchar2(8) check(message_status in ('Readed', 'Sended', 'Unreaded', 'Unsended'))
) tablespace katran_tablespace;
*/

------------------------------------------------------------------------------------------------------------------

------------------------------------------------------------------------------------------------------------------
-- views

------------------------------------------------------------------------------------------------------------------

------------------------------------------------------------------------------------------------------------------
-- procedures
create public synonym katran_procedures for katran_admin.katran_procedures;
CREATE OR REPLACE PACKAGE katran_procedures AS

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

    procedure AddChat( inChatTitle in chats.chat_title%type,
                       out_AddedChatId out chats.chat_id%type);

    procedure AddChatMember( inChatId in chat_members.chat_id%type,
                             in_outMemberId in out chat_members.member_id%type);

    procedure RemoveAllChatMembers( inChatId in chat_members.chat_id%type);

    procedure GetChatMembersCount( inChatId in chat_members.chat_id%type,
                                   outMembersCount out number);

    procedure RemoveChat( in_outChatId in out chat_members.chat_id%type);

    procedure BlockUnblockUser( in_outUserId in out users_info.id%type,
                                inIsBlocked in users_info.is_blocked%type);

    procedure GetUserLawStatusById( inUserId in users.id%type,
                                    outUserLawStatus out users.law_status%type);

        procedure AdminSearch( outResult out sys_refcursor,
                           inAdminId in users_info.id%type,
                           inSearchPattern in varchar2);

END;

create or replace package body katran_procedures as

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
    exception
        when others then
            outAddedUserId := -1;
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
    exception
        when others then
            outAddedUserId := -1;
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
    exception
        when others then
            in_outUserId := -1;
    end;

------------------------------------------------- ChangeUserStatusById

------------------------------------------------- AddContact

    procedure AddContact( in_outContactOwner in out users.id%type,
                          inTargetContact in users.id%type)
    as
    begin

        insert into contacts (contact_owner, contact)
        values(in_outContactOwner, inTargetContact);

        insert into contacts (contact, contact_owner)
        values(inTargetContact, in_outContactOwner);

    exception
        when others then
            in_outContactOwner := -1;
    end;

------------------------------------------------- AddContact

------------------------------------------------- RemoveContact

    procedure RemoveContact( in_outContactOwner in out users.id%type,
                             inTargetContact in users.id%type)
    as
    begin
        delete from contacts where contact_owner = in_outContactOwner and contact = inTargetContact;
    exception
        when others then
            in_outContactOwner := -1;
    end;

------------------------------------------------- RemoveContact

------------------------------------------------- AddChat

    procedure AddChat( inChatTitle in chats.chat_title%type,
                       out_AddedChatId out chats.chat_id%type)
    as
    begin

        insert into chats (chat_title)
        values(inChatTitle)
        returning chat_id into out_AddedChatId;

    exception
        when others then
            out_AddedChatId := -1;
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
    exception
        when others then
            in_outMemberId := -1;
    end;

------------------------------------------------- AddChatMember

------------------------------------------------- RemoveAllChatMembers

    procedure RemoveAllChatMembers( inChatId in chat_members.chat_id%type)
    as
    begin
        delete from chat_members where chat_id = inChatId;
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

------------------------------------------------- RemoveChat

    procedure RemoveChat( in_outChatId in out chat_members.chat_id%type)
    as
    begin
        delete from chats where chat_id = in_outChatId;
    exception
        when others then
            in_outChatId := -1;
    end;

------------------------------------------------- RemoveChat

------------------------------------------------- GetUserLawStatusById

    procedure GetUserLawStatusById( inUserId in users.id%type,
                                    outUserLawStatus out users.law_status%type)
    as
    begin
        select law_status into outUserLawStatus from users where id = inUserId;
    exception
        when others then
            outUserLawStatus := '';
    end;

------------------------------------------------- GetUserLawStatusById

------------------------------------------------- BlockUnblockUser

    procedure BlockUnblockUser( in_outUserId in out users_info.id%type,
                                inIsBlocked in users_info.is_blocked%type)
    as
    begin
        update users_info set is_blocked = inIsBlocked where id = in_outUserId;
    exception
        when others then
            in_outUserId := -1;
    end;

------------------------------------------------- BlockUnblockUser

------------------------------------------------- AdminSearch

    procedure AdminSearch( outResult out sys_refcursor,
                           inAdminId in users_info.id%type,
                           inSearchPattern in varchar2
                           )
    as
    begin
        open outResult for
        select ui.id, ui.app_name, ui.image, ui.status, ui.is_blocked
        from Users_info ui
        where instr(ui.app_name, inSearchPattern) > 0 and ui.id != inAdminId;
    end;

------------------------------------------------- AdminSearch

end;
------------------------------------------------------------------------------------------------------------------
