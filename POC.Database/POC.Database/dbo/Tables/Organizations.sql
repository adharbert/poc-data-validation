CREATE TABLE [dbo].[Organizations](
	[Id]							[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[Name]							[nvarchar](200)		NOT NULL,
	[FilingName]					[nvarchar](200)		NULL,
	[MarketingName]					[nvarchar](100)		NULL,
	[Abbreviation]					[nvarchar](50)		NULL,
	[OrganizationCode]				[nvarchar](26)		NOT NULL,	
	[Website]						[nvarchar](800)		NULL,
	[Phone]							[nvarchar](11)		NULL,
	[CompanyEmail]					[nvarchar](255)		NULL,
	[IsActive]						[bit]				NULL		DEFAULT(1),
	[CreateUtcDt]					[datetime]			NULL		DEFAULT (getutcdate()),
	[CreatedBy]						[nvarchar](50)		NOT NULL,
	[ModifiedUtcDt]					[datetime]			NULL		DEFAULT (getutcdate()),
	[ModifiedBy]					[nvarchar](50)		NULL,
	[RequiresIsolatedDatabase]		BIT					NOT NULL	DEFAULT (0),
	[IsolatedConnectionString]		NVARCHAR(500)		NULL,
	[DatabaseProvisioningStatus]	NVARCHAR(20)		NULL,
    [ValidFrom]						DATETIME2 (2)		GENERATED ALWAYS AS ROW START HIDDEN CONSTRAINT [DF_Organizations_ValidFrom]	DEFAULT (getutcdate())				NOT NULL,
    [ValidTo]						DATETIME2 (2)		GENERATED ALWAYS AS ROW END HIDDEN   CONSTRAINT [DF_Organizations_ValidTo]		DEFAULT ('9999.12.31 23:59:59.99')	NOT NULL,
 
	CONSTRAINT [PK_Organizations] PRIMARY KEY CLUSTERED (Id),
	
	CONSTRAINT CK_Organizations_Abbreviation_Length CHECK (LEN(Abbreviation) <= 4),
	
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo])

) 
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[Organizations_History], DATA_CONSISTENCY_CHECK=ON));
GO

CREATE UNIQUE INDEX IX_OrganizationsCode ON [dbo].[Organizations]([OrganizationCode])
GO

CREATE UNIQUE INDEX UQ_Organizations_Abbreviation ON dbo.Organizations (Abbreviation) WHERE Abbreviation IS NOT NULL
GO