CREATE TABLE [dbo].[Order]
(
    [OrderId]      INT            NOT NULL IDENTITY(1,1),
    [CustomerId]   INT            NOT NULL,
    [OrderDate]    DATETIME2(7)   NOT NULL CONSTRAINT [DF_Order_OrderDate] DEFAULT SYSUTCDATETIME(),
    [TotalAmount]  DECIMAL(18,2)  NOT NULL CONSTRAINT [DF_Order_TotalAmount] DEFAULT 0.00,
    [Status]       NVARCHAR(50)   NOT NULL CONSTRAINT [DF_Order_Status] DEFAULT N'Pending',

    CONSTRAINT [PK_Order] PRIMARY KEY CLUSTERED ([OrderId] ASC),
    CONSTRAINT [FK_Order_Customer] FOREIGN KEY ([CustomerId])
        REFERENCES [dbo].[Customer] ([CustomerId])
);
