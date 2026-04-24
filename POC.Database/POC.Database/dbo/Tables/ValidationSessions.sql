

CREATE TABLE ValidationSessions (
	[Id]					[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[CustomerId]			[uniqueidentifier]	NOT NULL,
	[StartedAt]				datetime			NOT NULL	DEFAULT(GETUTCDATE()),
	[CompletedAt]			datetime			NULL,		-- null while in progress
	[Status]				nvarchar(30)		NOT NULL	DEFAULT('in progress'),
	[TotalFields]			int					NULL,
	[ConfirmedFields]		int					NULL		DEFAULT(0),
	[FlaggedFields]			int					NULL		DEFAULT(0),
	[IpAddress]				nvarchar(45)		NULL,
	[UserAgent]				nvarchar(500)		NULL,

	CONSTRAINT [PK_ValidationSessions] PRIMARY KEY CLUSTERED (Id),
		
	CONSTRAINT [FK_ValidationSessions_Customers] 
		FOREIGN KEY([CustomerId])
		REFERENCES [dbo].[Customers] ([Id]),

	CONSTRAINT [CK_ValidationSessions_Status] CHECK (
        [Status] IN ('in progress','completed','abandoned')
    )
)
GO

CREATE INDEX IX_ValidationSessions_Customer ON ValidationSessions ([CustomerId], [StartedAt] DESC)
GO