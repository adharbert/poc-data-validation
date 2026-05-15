CREATE TABLE [dbo].[Customer]
(
    [CustomerId]   INT            NOT NULL IDENTITY(1,1),
    [FirstName]    NVARCHAR(100)  NOT NULL,
    [LastName]     NVARCHAR(100)  NOT NULL,
    [Email]        NVARCHAR(255)  NOT NULL,
    [CreatedDate]  DATETIME2(7)   NOT NULL CONSTRAINT [DF_Customer_CreatedDate] DEFAULT SYSUTCDATETIME(),
    [IsActive]     BIT            NOT NULL CONSTRAINT [DF_Customer_IsActive] DEFAULT 1,

    CONSTRAINT [PK_Customer] PRIMARY KEY CLUSTERED ([CustomerId] ASC),
    CONSTRAINT [UQ_Customer_Email] UNIQUE ([Email])
);
