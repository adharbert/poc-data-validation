SET NOCOUNT ON;
GO

-- Widen Customers.Phone from nvarchar(11) to nvarchar(20) to accommodate
-- international phone numbers (E.164 allows up to 15 digits).

IF COL_LENGTH('dbo.Customers', 'Phone') = 22  -- nvarchar(11) = 22 bytes
BEGIN
    ALTER TABLE dbo.Customers ALTER COLUMN [Phone] nvarchar(20) NULL;
END
GO
