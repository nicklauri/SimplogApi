CREATE DATABASE Simplog;

USE Simplog;

CREATE TABLE Users (
	Id INT IDENTITY(1,1) PRIMARY KEY,
	Username Varchar(50) NOT NULL,
	Password Varchar(50) NOT NULL,
);

-- Add some fake account with unhashed password.
INSERT INTO Users (Username, Password)
VALUES 
	('admin', '1234'),
	('moderator', 'yeah!'),
	('apleb', 'hello'),
	('delete', 'this'),
	('fake_account', 'fake_pw');

-- Test get data from Users table.
SELECT * FROM [Users];

-- Truncating Users table.
TRUNCATE TABLE [Users];

DROP TABLE [Users];

DROP DATABASE Simplog;

