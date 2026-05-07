USE [master];
GO

IF DB_ID(N'QLThuVien') IS NOT NULL
BEGIN
    ALTER DATABASE [QLThuVien] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [QLThuVien];
END
GO

CREATE DATABASE [QLThuVien];
GO

USE [QLThuVien];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);
GO

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    RoleId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY(RoleId) REFERENCES Roles(Id)
);
GO

CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500)
);
GO

CREATE TABLE Authors (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AuthorName NVARCHAR(100) NOT NULL,
    Biography NVARCHAR(MAX)
);
GO

CREATE TABLE Publishers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PublisherName NVARCHAR(150) NOT NULL,
    Address NVARCHAR(255),
    Phone NVARCHAR(20)
);
GO

CREATE TABLE Books (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BookCode NVARCHAR(50) NOT NULL UNIQUE,
    BookName NVARCHAR(200) NOT NULL,
    CategoryId INT NOT NULL,
    AuthorId INT NOT NULL,
    PublisherId INT NOT NULL,
    PublishYear INT,
    Quantity INT NOT NULL CHECK(Quantity >= 0),
    AvailableQuantity INT NOT NULL CHECK(AvailableQuantity >= 0),
    RentalFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    BookPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
    ImageUrl NVARCHAR(255),
    Description NVARCHAR(MAX),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Books_Categories FOREIGN KEY(CategoryId) REFERENCES Categories(Id),
    CONSTRAINT FK_Books_Authors FOREIGN KEY(AuthorId) REFERENCES Authors(Id),
    CONSTRAINT FK_Books_Publishers FOREIGN KEY(PublisherId) REFERENCES Publishers(Id)
);
GO

CREATE TABLE Readers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReaderCode NVARCHAR(50) NOT NULL UNIQUE,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    Address NVARCHAR(255),
    DateOfBirth DATE,
    IdentityNumber NVARCHAR(20),
    Gender NVARCHAR(10),
    Occupation NVARCHAR(100),
    Workplace NVARCHAR(150),
    MembershipType NVARCHAR(30) NOT NULL DEFAULT N'Standard',
    CardIssuedDate DATE,
    CardExpiryDate DATE,
    EmergencyContactName NVARCHAR(100),
    EmergencyContactPhone NVARCHAR(20),
    Notes NVARCHAR(500),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE UNIQUE INDEX IX_Readers_IdentityNumber
    ON Readers(IdentityNumber)
    WHERE IdentityNumber IS NOT NULL;
GO

CREATE TABLE BorrowTickets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BorrowCode NVARCHAR(50) NOT NULL UNIQUE,
    ReaderId INT NOT NULL,
    UserId INT NOT NULL,
    BorrowDate DATETIME NOT NULL DEFAULT GETDATE(),
    DueDate DATETIME NOT NULL,
    TotalRentalFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(50) NOT NULL DEFAULT N'Borrowing',
    CONSTRAINT FK_BorrowTickets_Readers FOREIGN KEY(ReaderId) REFERENCES Readers(Id),
    CONSTRAINT FK_BorrowTickets_Users FOREIGN KEY(UserId) REFERENCES Users(Id)
);
GO

CREATE TABLE BorrowTicketDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BorrowTicketId INT NOT NULL,
    BookId INT NOT NULL,
    Quantity INT NOT NULL CHECK(Quantity > 0),
    UnitRentalFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    LineRentalFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_BorrowDetails_BorrowTickets FOREIGN KEY(BorrowTicketId) REFERENCES BorrowTickets(Id),
    CONSTRAINT FK_BorrowDetails_Books FOREIGN KEY(BookId) REFERENCES Books(Id)
);
GO

CREATE TABLE ReturnTickets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReturnCode NVARCHAR(50) NOT NULL UNIQUE,
    BorrowTicketId INT NOT NULL UNIQUE,
    ReturnDate DATETIME NOT NULL DEFAULT GETDATE(),
    UserId INT NOT NULL,
    CONSTRAINT FK_ReturnTickets_BorrowTickets FOREIGN KEY(BorrowTicketId) REFERENCES BorrowTickets(Id),
    CONSTRAINT FK_ReturnTickets_Users FOREIGN KEY(UserId) REFERENCES Users(Id)
);
GO

CREATE TABLE ReturnTicketDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReturnTicketId INT NOT NULL,
    BookId INT NOT NULL,
    Quantity INT NOT NULL CHECK(Quantity > 0),
    NormalQuantity INT NOT NULL DEFAULT 0 CHECK(NormalQuantity >= 0),
    DamagedQuantity INT NOT NULL DEFAULT 0 CHECK(DamagedQuantity >= 0),
    LostQuantity INT NOT NULL DEFAULT 0 CHECK(LostQuantity >= 0),
    IsDamaged BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ReturnDetails_ReturnTickets FOREIGN KEY(ReturnTicketId) REFERENCES ReturnTickets(Id),
    CONSTRAINT FK_ReturnDetails_Books FOREIGN KEY(BookId) REFERENCES Books(Id)
);
GO

CREATE TABLE Fines (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReaderId INT NOT NULL,
    BorrowTicketId INT NOT NULL,
    LateDays INT NOT NULL CHECK(LateDays >= 0),
    Amount DECIMAL(18,2) NOT NULL CHECK(Amount >= 0),
    Reason NVARCHAR(255),
    IsPaid BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Fines_Readers FOREIGN KEY(ReaderId) REFERENCES Readers(Id),
    CONSTRAINT FK_Fines_BorrowTickets FOREIGN KEY(BorrowTicketId) REFERENCES BorrowTickets(Id)
);
GO

INSERT INTO Roles(RoleName)
VALUES (N'Admin'), (N'Librarian'), (N'Reader');
GO

-- Mật khẩu gốc: 123456
INSERT INTO Users(FullName, Email, PasswordHash, RoleId, IsActive)
VALUES
    (N'Quản trị viên', N'admin@gmail.com', N'AQAAAAIAAYagAAAAECdjQndpmmhPMndib9JAcuJYSUK8aTMpjongDaePSnyA4QpWmGREDh2H9BbJWhWzTQ==', 1, 1),
    (N'Thủ thư', N'librarian@gmail.com', N'AQAAAAIAAYagAAAAECdjQndpmmhPMndib9JAcuJYSUK8aTMpjongDaePSnyA4QpWmGREDh2H9BbJWhWzTQ==', 2, 1);
GO

INSERT INTO Categories(CategoryName, Description)
VALUES
    (N'Công nghệ thông tin', N'Sách CNTT'),
    (N'Kinh tế', N'Sách kinh tế'),
    (N'Văn học', N'Truyện, tiểu thuyết');
GO

INSERT INTO Authors(AuthorName, Biography)
VALUES
    (N'Nguyễn Văn A', N'Tác giả sách CNTT'),
    (N'Trần Thị B', N'Tác giả sách kinh tế');
GO

INSERT INTO Publishers(PublisherName, Address, Phone)
VALUES
    (N'NXB Giáo Dục', N'Hà Nội', N'0240000000'),
    (N'NXB Trẻ', N'TP.HCM', N'0280000000');
GO

INSERT INTO Readers(ReaderCode, FullName, Email, Phone, Address, MembershipType, IsActive)
VALUES
    (N'DG001', N'Nguyễn Minh Anh', N'anha@example.com', N'0901000001', N'Hà Nội', N'Standard', 1),
    (N'DG002', N'Trần Quốc Bình', N'binhtq@example.com', N'0901000002', N'Hà Nội', N'Standard', 1);
GO

INSERT INTO Books(BookCode, BookName, CategoryId, AuthorId, PublisherId, PublishYear, Quantity, AvailableQuantity, RentalFee, BookPrice, Description)
VALUES
    (N'S001', N'Lập trình ASP.NET MVC', 1, 1, 1, 2024, 10, 10, 12000, 120000, N'Sách hướng dẫn lập trình web với ASP.NET MVC'),
    (N'S002', N'Cơ sở dữ liệu SQL Server', 1, 1, 1, 2023, 8, 8, 10000, 100000, N'Sách nhập môn SQL Server'),
    (N'S003', N'Marketing căn bản', 2, 2, 2, 2022, 6, 6, 9000, 90000, N'Tài liệu kinh tế và marketing');
GO

PRINT N'Hoàn tất tạo CSDL QLThuVien (one-shot).';
PRINT N'Tài khoản quản trị mặc định: admin@gmail.com / 123456';
GO
