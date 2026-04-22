CREATE TABLE [dbo].[Organizations] (
    [Id]               [uniqueidentifier] NOT NULL CONSTRAINT [DF_Organizations_Id] DEFAULT (newsequentialid()),
    [Name]             [nvarchar](200)    NOT NULL,
    [FilingName]       [nvarchar](200)    NULL,
    [MarketingName]    [nvarchar](100)    NULL,
    [Abbreviation]     [nvarchar](50)     NULL,
    [OrganizationCode] [nvarchar](26)     NOT NULL,
    [Website]          [nvarchar](800)    NULL,
    [Phone]            [nvarchar](11)     NULL,
    [CompanyEmail]     [nvarchar](255)    NULL,
    [IsActive]         [bit]              NULL     CONSTRAINT [DF_Organizations_IsActive] DEFAULT (1),
    [CreateUtcDt]      [datetime]         NULL     CONSTRAINT [DF_Organizations_CreateUtcDt] DEFAULT (getutcdate()),
    [CreatedBy]        [nvarchar](50)     NOT NULL,
    [ModifiedUtcDt]    [datetime]         NULL     CONSTRAINT [DF_Organizations_ModifiedUtcDt] DEFAULT (getutcdate()),
    [ModifiedBy]       [nvarchar](50)     NULL,
    [ValidFrom]        DATETIME2(2)       GENERATED ALWAYS AS ROW START HIDDEN CONSTRAINT [DF_Organizations_ValidFrom] DEFAULT (getutcdate()) NOT NULL,
    [ValidTo]          DATETIME2(2)       GENERATED ALWAYS AS ROW END HIDDEN   CONSTRAINT [DF_Organizations_ValidTo]   DEFAULT ('9999.12.31 23:59:59.99') NOT NULL,

    CONSTRAINT [PK_Organizations] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_Organizations_Abbreviation_Length] CHECK (LEN([Abbreviation]) <= 4),
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Organizations_History], DATA_CONSISTENCY_CHECK = ON));
