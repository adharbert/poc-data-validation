CREATE TABLE [dbo].[Customers] (
	[Id]					[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[OriginalId]			nvarchar(100)		NULL,
	[FirstName]				nvarchar(30)		NOT NULL,
	[LastName]				nvarchar(50)		NOT NULL,
	[MiddleName]			nvarchar(30)		NULL,
	[MaidenName]			nvarchar(50)		NULL,
	[DateOfBirth]			date				NULL,
	[CustomerCode]			[nvarchar](26)		NOT NULL,
	[OrganizationId]		[uniqueidentifier]	NOT NULL,
	[Email]					nvarchar(150)		NULL,
	[Phone]					nvarchar(11)		NULL,
	[IsActive]				bit					NULL		DEFAULT(1),
	[CreatedDt]				datetime			NOT NULL	DEFAULT(GETUTCDATE()),
	[ModifiedDt]			datetime			NOT NULL	DEFAULT(GETUTCDATE()),
	[ValidFrom]				DATETIME2 (2)		GENERATED ALWAYS AS ROW START HIDDEN CONSTRAINT [DF_Customers_ValidFrom]	NOT NULL	DEFAULT (getutcdate()), 
	[ValidTo]				DATETIME2 (2)		GENERATED ALWAYS AS ROW END HIDDEN   CONSTRAINT [DF_Customers_ValidTo]		NOT NULL	DEFAULT ('9999.12.31 23:59:59.99'),
 
	CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED (Id),
	PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo]),

	CONSTRAINT [FK_Customers_Organizations] 
		FOREIGN KEY([OrganizationId])
		REFERENCES [dbo].[Organizations] ([Id]),
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[Customers_History], DATA_CONSISTENCY_CHECK=ON));
GO

CREATE UNIQUE INDEX IX_CustomerCode ON [dbo].[Customers]([CustomerCode]);
GO

CREATE INDEX [IX_Customers_OriginalId] ON dbo.Customers (OrganizationId, OriginalId)
    WHERE OriginalId IS NOT NULL;
GO