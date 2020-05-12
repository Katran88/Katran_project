create database Katran
go
use Katran

drop table Users_info
drop table Users
drop table Chats
drop table Contacts

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
    chat_title varchar(40) unique,
    chat_members_id varchar(max),
)
go

select ui.id, ui.app_name, ui.email, ui.user_discription, ui.image, ui.status, u.law_status, u.password
from Users_info ui join Users u on ui.id = u.id
where ui.id = (select u2.id from Users as u2 where u2.auth_login = 'Vasya')

select ui.id, ui.app_name, ui.image, ui.status
from Users_info as ui
where charindex('Aa', ui.app_name) > 0 and ui.id != 2 and
      ui.id NOT IN (select ui.id from Users_info as ui join Contacts as c on ui.id = c.contact and c.contact_owner = 2)

