create database Katran
go
use Katran

drop table Users_info
drop table Users
drop table Chats
drop table Contacts
drop table ChatMembers

create table Users
(
	id int identity(1,1) primary key,
	auth_login varchar(20) unique,
	password char(32) not null,
    law_status nchar(5) default 'User' check(law_status in('Admin', 'User'))
)
go

create table Users_info
(
	id int constraint Users_info_Users_id_fk references Users (id),
	app_name varchar(20),
	email varchar(50),
	user_discription varchar(200),
	image varbinary(MAX),
	status varchar(8) default 'Offline' check(status in('Offline', 'Online'))
)
go

create table Contacts
(
	contact_owner int,
	contact int
)
go

create table Chats
(
    chat_id int identity(1,1) primary key,
    chat_title varchar(40) unique, --для чатов 1 на 1 паттерн названия [id]_[id], для бесед [название беседы]
)
go

create table ChatMembers
(
    chat_id int references Chats (chat_id),
    member_id int references Users (id)
)

command = new SqlCommand($"create table {String.Format("ChatMessages_{0}", chatId)} " +
                                    "( message varbinary(MAX)," +
                                    " message_type varchar(4) check(message_type in ('File', 'Text'))," +
                                    " file_name varchar(max)," +
                                    " time smalldatetime," +
                                    " message_status varchar(8) check(message_status in ('Readed', 'Sended', 'Unreaded', 'Unsended')))", Server.sql);

select top(100) message, message_type, file_name, time, message_status
from ChatMessages_11
order by time desc