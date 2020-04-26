create database Katran
go
use Katran

create table Users
(
	id int identity(1,1) primary key,
	auth_login varchar(20) unique,
	password char(32) not null,
    law_status nchar(5) default 'user' check(law_status in('admin', 'user'))
)
go

create table Users_info
(
	id int constraint Users_info_Users_id_fk references Users (id),
	app_name varchar(20) constraint Users_info_pk primary key nonclustered,
	email varchar(50),
	user_description varchar(200),
	image varbinary(MAX),
	status varchar(8) default 'offline' check(status in('offline', 'online'))
)
go
