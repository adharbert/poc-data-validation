-- ============================================================
--  DEV / LOCAL SEED ONLY — NOT included in PostDeploy.sql
--
--  Inserts 2 field sections, 10 field definitions, 50 customers,
--  and 500 field values for local development and testing.
--
--  INSTRUCTIONS:
--    1. Ensure an Organization row exists first.
--    2. Set @OrgId to that Organization's Id.
--    3. Run this script manually from SSMS or sqlcmd.
--
--  This script is IDEMPOTENT within the same @OrgId:
--    - Field sections and definitions are inserted only if the
--      FieldKey does not already exist for the org.
--    - Customers and field values are inserted only if
--      CustomerCode does not already exist.
-- ============================================================

SET NOCOUNT ON;

DECLARE @OrgId UNIQUEIDENTIFIER = '';   -- <-- set your OrganizationId here

-- ============================================================
--  Validate org exists
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Organizations] WHERE [Id] = @OrgId)
BEGIN
    RAISERROR('OrganizationId does not exist in Organizations. Script aborted.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

BEGIN TRY

    -- ============================================================
    --  FIELD SECTIONS
    -- ============================================================
    DECLARE @SectionPersonal   UNIQUEIDENTIFIER;
    DECLARE @SectionEmployment UNIQUEIDENTIFIER;

    SELECT @SectionPersonal = [Id]
    FROM   [dbo].[FieldSections]
    WHERE  [OrganizationId] = @OrgId AND [SectionName] = 'Personal Information';

    IF @SectionPersonal IS NULL
    BEGIN
        SET @SectionPersonal = NEWID();
        INSERT INTO [dbo].[FieldSections] ([Id], [OrganizationId], [SectionName], [DisplayOrder], [IsActive])
        VALUES (@SectionPersonal, @OrgId, 'Personal Information', 1, 1);
    END

    SELECT @SectionEmployment = [Id]
    FROM   [dbo].[FieldSections]
    WHERE  [OrganizationId] = @OrgId AND [SectionName] = 'Employment Details';

    IF @SectionEmployment IS NULL
    BEGIN
        SET @SectionEmployment = NEWID();
        INSERT INTO [dbo].[FieldSections] ([Id], [OrganizationId], [SectionName], [DisplayOrder], [IsActive])
        VALUES (@SectionEmployment, @OrgId, 'Employment Details', 2, 1);
    END

    -- ============================================================
    --  FIELD DEFINITIONS  (upsert by FieldKey per org)
    -- ============================================================
    DECLARE @FD_DOB      UNIQUEIDENTIFIER;
    DECLARE @FD_Phone    UNIQUEIDENTIFIER;
    DECLARE @FD_Address  UNIQUEIDENTIFIER;
    DECLARE @FD_City     UNIQUEIDENTIFIER;
    DECLARE @FD_State    UNIQUEIDENTIFIER;
    DECLARE @FD_Zip      UNIQUEIDENTIFIER;
    DECLARE @FD_Degree   UNIQUEIDENTIFIER;
    DECLARE @FD_Employer UNIQUEIDENTIFIER;
    DECLARE @FD_YearsExp UNIQUEIDENTIFIER;
    DECLARE @FD_FullTime UNIQUEIDENTIFIER;

    -- Resolve existing or create new Id per field key
    SELECT @FD_DOB      = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'date_of_birth';
    SELECT @FD_Phone    = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'phone_number';
    SELECT @FD_Address  = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'street_address';
    SELECT @FD_City     = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'city';
    SELECT @FD_State    = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'state';
    SELECT @FD_Zip      = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'zip_code';
    SELECT @FD_Degree   = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'highest_degree';
    SELECT @FD_Employer = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'current_employer';
    SELECT @FD_YearsExp = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'years_experience';
    SELECT @FD_FullTime = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'full_time';

    IF @FD_DOB IS NULL      SET @FD_DOB      = NEWID();
    IF @FD_Phone IS NULL    SET @FD_Phone    = NEWID();
    IF @FD_Address IS NULL  SET @FD_Address  = NEWID();
    IF @FD_City IS NULL     SET @FD_City     = NEWID();
    IF @FD_State IS NULL    SET @FD_State    = NEWID();
    IF @FD_Zip IS NULL      SET @FD_Zip      = NEWID();
    IF @FD_Degree IS NULL   SET @FD_Degree   = NEWID();
    IF @FD_Employer IS NULL SET @FD_Employer = NEWID();
    IF @FD_YearsExp IS NULL SET @FD_YearsExp = NEWID();
    IF @FD_FullTime IS NULL SET @FD_FullTime = NEWID();

    -- Merge field definitions
    MERGE [dbo].[FieldDefinitions] AS target
    USING (VALUES
        (@FD_DOB,      @OrgId, @SectionPersonal,   'date_of_birth',     'Date of Birth',                 'date',       NULL,               'Your date of birth as it appears on your ID.',       1, 1,  1, NULL, NULL, NULL, NULL, NULL),
        (@FD_Phone,    @OrgId, @SectionPersonal,   'phone_number',      'Phone Number',                  'text',       '(555) 555-5555',   'Primary contact number including area code.',        0, 1,  2, NULL, NULL,    7,   20, NULL),
        (@FD_Address,  @OrgId, @SectionPersonal,   'street_address',    'Street Address',                'text',       '123 Main St',      NULL,                                                 1, 1,  3, NULL, NULL,    5,  200, NULL),
        (@FD_City,     @OrgId, @SectionPersonal,   'city',              'City',                          'text',       'e.g. Springfield', NULL,                                                 1, 1,  4, NULL, NULL,    2,  100, NULL),
        (@FD_State,    @OrgId, @SectionPersonal,   'state',             'State',                         'dropdown',   'Select a state',   NULL,                                                 1, 1,  5, NULL, NULL, NULL, NULL, NULL),
        (@FD_Zip,      @OrgId, @SectionPersonal,   'zip_code',          'ZIP Code',                      'text',       '12345',            NULL,                                                 1, 1,  6, NULL, NULL,    5,   10, N'^\d{5}(-\d{4})?$'),
        (@FD_Degree,   @OrgId, @SectionEmployment, 'highest_degree',    'Highest Degree',                'dropdown',   'Select your degree','Select the highest level of education completed.', 0, 1,  7, NULL, NULL, NULL, NULL, NULL),
        (@FD_Employer, @OrgId, @SectionEmployment, 'current_employer',  'Current Employer',              'text',       'Company name',     NULL,                                                 0, 1,  8, NULL, NULL,    2,  200, NULL),
        (@FD_YearsExp, @OrgId, @SectionEmployment, 'years_experience',  'Years of Experience',           'number',     NULL,               'Total years of relevant work experience.',           0, 1,  9,    0,   50, NULL, NULL, NULL),
        (@FD_FullTime, @OrgId, @SectionEmployment, 'full_time',         'Currently Employed Full Time',  'checkbox',   NULL,               'Check if you are currently employed full time.',     0, 1, 10, NULL, NULL, NULL, NULL, NULL)
    ) AS source (
        Id, OrganizationId, FieldSectionId, FieldKey, FieldLabel, FieldType,
        PlaceHolderText, HelpText, IsRequired, IsActive, DisplayOrder,
        MinValue, MaxValue, MinLength, MaxLength, RegExPattern
    )
        ON target.[OrganizationId] = source.OrganizationId
       AND target.[FieldKey]       = source.FieldKey
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Id], [OrganizationId], [FieldSectionId], [FieldKey], [FieldLabel], [FieldType],
                [PlaceHolderText], [HelpText], [IsRequired], [IsActive], [DisplayOrder],
                [MinValue], [MaxValue], [MinLength], [MaxLength], [RegExPattern])
        VALUES (source.Id, source.OrganizationId, source.FieldSectionId, source.FieldKey,
                source.FieldLabel, source.FieldType, source.PlaceHolderText, source.HelpText,
                source.IsRequired, source.IsActive, source.DisplayOrder,
                source.MinValue, source.MaxValue, source.MinLength, source.MaxLength, source.RegExPattern);

    -- Re-resolve Ids in case some already existed before the merge
    SELECT @FD_DOB      = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'date_of_birth';
    SELECT @FD_Phone    = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'phone_number';
    SELECT @FD_Address  = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'street_address';
    SELECT @FD_City     = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'city';
    SELECT @FD_State    = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'state';
    SELECT @FD_Zip      = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'zip_code';
    SELECT @FD_Degree   = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'highest_degree';
    SELECT @FD_Employer = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'current_employer';
    SELECT @FD_YearsExp = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'years_experience';
    SELECT @FD_FullTime = [Id] FROM [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId AND [FieldKey] = 'full_time';

    -- ============================================================
    --  CUSTOMERS  (50 records — skip those with matching CustomerCode)
    -- ============================================================
    DECLARE @OrgCode NVARCHAR(6);
    SELECT @OrgCode = LEFT(UPPER(REPLACE(REPLACE(REPLACE([OrganizationCode], ' ', ''), '-', ''), '_', '')), 6)
    FROM   [dbo].[Organizations]
    WHERE  [Id] = @OrgId;

    DECLARE @Customers TABLE (CustomerId UNIQUEIDENTIFIER, CustomerCode NVARCHAR(26));

    ;WITH Names AS (
        SELECT * FROM (VALUES
            ( 1, 'James',     'Anderson',   'A'), ( 2, 'Mary',      'Thompson',   'B'),
            ( 3, 'Robert',    'Martinez',   'C'), ( 4, 'Patricia',  'Garcia',     'D'),
            ( 5, 'John',      'Wilson',     'E'), ( 6, 'Jennifer',  'Davis',      'F'),
            ( 7, 'Michael',   'Rodriguez',  'G'), ( 8, 'Linda',     'Lee',        'H'),
            ( 9, 'William',   'Walker',     'I'), (10, 'Barbara',   'Hall',       'J'),
            (11, 'David',     'Allen',      'K'), (12, 'Susan',     'Young',      'L'),
            (13, 'Richard',   'Hernandez',  'M'), (14, 'Jessica',   'King',       'N'),
            (15, 'Joseph',    'Wright',     'O'), (16, 'Sarah',     'Lopez',      'P'),
            (17, 'Thomas',    'Hill',       'Q'), (18, 'Karen',     'Scott',      'R'),
            (19, 'Charles',   'Green',      'S'), (20, 'Lisa',      'Adams',      'T'),
            (21, 'Daniel',    'Baker',      'U'), (22, 'Nancy',     'Nelson',     'V'),
            (23, 'Mark',      'Carter',     'W'), (24, 'Betty',     'Mitchell',   'X'),
            (25, 'Paul',      'Perez',      'Y'), (26, 'Dorothy',   'Roberts',    'Z'),
            (27, 'Steven',    'Turner',     'A'), (28, 'Sandra',    'Phillips',   'B'),
            (29, 'Kenneth',   'Campbell',   'C'), (30, 'Ashley',    'Parker',     'D'),
            (31, 'George',    'Evans',      'E'), (32, 'Kimberly',  'Edwards',    'F'),
            (33, 'Edward',    'Collins',    'G'), (34, 'Donna',     'Stewart',    'H'),
            (35, 'Brian',     'Sanchez',    'I'), (36, 'Carol',     'Morris',     'J'),
            (37, 'Ronald',    'Rogers',     'K'), (38, 'Michelle',  'Reed',       'L'),
            (39, 'Anthony',   'Cook',       'M'), (40, 'Amanda',    'Morgan',     'N'),
            (41, 'Kevin',     'Bell',       'O'), (42, 'Melissa',   'Murphy',     'P'),
            (43, 'Jason',     'Bailey',     'Q'), (44, 'Deborah',   'Rivera',     'R'),
            (45, 'Matthew',   'Cooper',     'S'), (46, 'Stephanie', 'Richardson', 'T'),
            (47, 'Gary',      'Cox',        'U'), (48, 'Rebecca',   'Howard',     'V'),
            (49, 'Timothy',   'Ward',       'W'), (50, 'Sharon',    'Torres',     'X')
        ) AS n (RowNum, FirstName, LastName, Initial)
    ),
    Generated AS (
        SELECT
            NEWID()                                                           AS NewId,
            n.FirstName,
            n.LastName,
            n.Initial,
            @OrgCode + RIGHT('000' + CAST(n.RowNum AS NVARCHAR(3)), 3)
                + LEFT(UPPER(n.LastName), 4)                                  AS CustomerCode,
            LOWER(n.FirstName) + '.' + LOWER(n.LastName)
                + CAST(n.RowNum AS NVARCHAR(3)) + '@example.com'              AS Email
        FROM Names n
    )
    INSERT INTO [dbo].[Customers] ([Id], [FirstName], [LastName], [MiddleName], [CustomerCode], [OrganizationId], [Email], [IsActive])
    OUTPUT inserted.[Id], inserted.[CustomerCode] INTO @Customers ([CustomerId], [CustomerCode])
    SELECT g.NewId, g.FirstName, g.LastName, g.Initial, g.CustomerCode, @OrgId, g.Email, 1
    FROM   Generated g
    WHERE  NOT EXISTS (
               SELECT 1 FROM [dbo].[Customers] WHERE [CustomerCode] = g.CustomerCode
           );

    -- ============================================================
    --  FIELD VALUES  (one row per customer per field)
    --  Only inserts rows that do not already exist.
    -- ============================================================
    DECLARE @Cities TABLE (Seq INT, City NVARCHAR(100), State NVARCHAR(50), Zip NVARCHAR(10));
    INSERT INTO @Cities VALUES
        (1,'New York','NY','10001'),(2,'Los Angeles','CA','90001'),
        (3,'Chicago','IL','60601'),(4,'Houston','TX','77001'),
        (5,'Phoenix','AZ','85001'),(6,'Philadelphia','PA','19101'),
        (7,'San Antonio','TX','78201'),(8,'San Diego','CA','92101'),
        (9,'Dallas','TX','75201'),(10,'Jacksonville','FL','32099');

    ;WITH CustomerList AS (
        SELECT CustomerId, CustomerCode,
               ROW_NUMBER() OVER (ORDER BY CustomerId) AS RowNum
        FROM   @Customers
    )
    INSERT INTO [dbo].[FieldValues] (
        [Id], [CustomerId], [FieldDefinitionId],
        [ValueText], [ValueNumber], [ValueDate], [ValueDatetime], [ValueBoolean],
        [ConfirmedAt], [ConfirmedBy], [FlaggedAt], [FlagNote]
    )
    SELECT
        NEWID(),
        c.CustomerId,
        fd.[Id],
        CASE fd.[Id]
            WHEN @FD_Phone   THEN
                '(' + RIGHT('000' + CAST(ABS(CHECKSUM(NEWID())) % 900 + 100 AS NVARCHAR(3)), 3) + ') ' +
                      RIGHT('000' + CAST(ABS(CHECKSUM(NEWID())) % 900 + 100 AS NVARCHAR(3)), 3) + '-' +
                      RIGHT('0000'+ CAST(ABS(CHECKSUM(NEWID())) % 9000 + 1000 AS NVARCHAR(4)), 4)
            WHEN @FD_Address THEN
                CAST((ABS(CHECKSUM(NEWID())) % 9900 + 100) AS NVARCHAR(10)) + ' ' +
                CASE (ABS(CHECKSUM(c.CustomerId)) % 8)
                    WHEN 0 THEN 'Main St'  WHEN 1 THEN 'Oak Ave'
                    WHEN 2 THEN 'Maple Dr' WHEN 3 THEN 'Cedar Ln'
                    WHEN 4 THEN 'Pine Rd'  WHEN 5 THEN 'Elm St'
                    WHEN 6 THEN 'Washington Blvd' ELSE 'Park Ave'
                END
            WHEN @FD_City    THEN (SELECT City  FROM @Cities WHERE Seq = (ABS(CHECKSUM(c.CustomerId)) % 10) + 1)
            WHEN @FD_State   THEN (SELECT State FROM @Cities WHERE Seq = (ABS(CHECKSUM(c.CustomerId)) % 10) + 1)
            WHEN @FD_Zip     THEN (SELECT Zip   FROM @Cities WHERE Seq = (ABS(CHECKSUM(c.CustomerId)) % 10) + 1)
            WHEN @FD_Degree  THEN
                CASE (ABS(CHECKSUM(c.CustomerId)) % 6)
                    WHEN 0 THEN 'none' WHEN 1 THEN 'hs'   WHEN 2 THEN 'assoc'
                    WHEN 3 THEN 'bach' WHEN 4 THEN 'mast' ELSE 'phd'
                END
            WHEN @FD_Employer THEN
                CASE (ABS(CHECKSUM(c.CustomerId)) % 10)
                    WHEN 0 THEN 'Acme Industries'        WHEN 1 THEN 'Bright Solutions LLC'
                    WHEN 2 THEN 'Cornerstone Group'      WHEN 3 THEN 'Delta Dynamics'
                    WHEN 4 THEN 'Eagle Consulting'       WHEN 5 THEN 'Frontier Technologies'
                    WHEN 6 THEN 'Global Ventures'        WHEN 7 THEN 'Horizon Partners'
                    WHEN 8 THEN 'Ironclad Services'      ELSE        'Jasper & Associates'
                END
            ELSE NULL
        END,
        CASE fd.[Id] WHEN @FD_YearsExp THEN CAST(ABS(CHECKSUM(NEWID())) % 36 AS DECIMAL(10,4)) ELSE NULL END,
        CASE fd.[Id] WHEN @FD_DOB THEN DATEADD(DAY, ABS(CHECKSUM(c.CustomerId)) % 18262, '1950-01-01') ELSE NULL END,
        NULL,
        CASE fd.[Id] WHEN @FD_FullTime THEN CAST(ABS(CHECKSUM(c.CustomerId)) % 2 AS BIT) ELSE NULL END,
        CASE WHEN ABS(CHECKSUM(c.CustomerId, fd.[Id])) % 2 = 0
             THEN DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 30), GETUTCDATE()) ELSE NULL END,
        CASE WHEN ABS(CHECKSUM(c.CustomerId, fd.[Id])) % 2 = 0 THEN 'system.seed' ELSE NULL END,
        CASE WHEN ABS(CHECKSUM(c.CustomerId, fd.[Id])) % 10 = 0
             THEN DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 10), GETUTCDATE()) ELSE NULL END,
        CASE WHEN ABS(CHECKSUM(c.CustomerId, fd.[Id])) % 10 = 0
             THEN CASE (ABS(CHECKSUM(c.CustomerId)) % 3)
                      WHEN 0 THEN 'Value appears incorrect, please review.'
                      WHEN 1 THEN 'Information has changed since last submission.'
                      ELSE        'Unable to verify against records on file.'
                  END
             ELSE NULL
        END
    FROM   CustomerList c
    CROSS  JOIN [dbo].[FieldDefinitions] fd
    WHERE  fd.[OrganizationId] = @OrgId
      AND  NOT EXISTS (
               SELECT 1 FROM [dbo].[FieldValues] fv
               WHERE fv.[CustomerId] = c.CustomerId AND fv.[FieldDefinitionId] = fd.[Id]
           );

    COMMIT TRANSACTION;

    -- Summary
    SELECT 'Field Sections'    AS [Table], COUNT(*) AS [Rows]
    FROM   [dbo].[FieldSections]    WHERE [OrganizationId] = @OrgId
    UNION ALL
    SELECT 'Field Definitions', COUNT(*)
    FROM   [dbo].[FieldDefinitions] WHERE [OrganizationId] = @OrgId
    UNION ALL
    SELECT 'Customers',         COUNT(*)
    FROM   [dbo].[Customers]        WHERE [OrganizationId] = @OrgId
    UNION ALL
    SELECT 'Field Values',      COUNT(*)
    FROM   [dbo].[FieldValues]
    WHERE  [CustomerId] IN (SELECT [Id] FROM [dbo].[Customers] WHERE [OrganizationId] = @OrgId);

    PRINT 'Dev seed data completed successfully.';

END TRY
BEGIN CATCH

    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

    DECLARE @ErrMessage  NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT            = ERROR_SEVERITY();
    DECLARE @ErrState    INT            = ERROR_STATE();
    DECLARE @ErrLine     INT            = ERROR_LINE();

    PRINT '============================================================';
    PRINT 'SEED SCRIPT FAILED — all changes have been rolled back.';
    PRINT 'Line    : ' + CAST(@ErrLine AS NVARCHAR(10));
    PRINT 'Message : ' + @ErrMessage;
    PRINT '============================================================';

    RAISERROR(@ErrMessage, @ErrSeverity, @ErrState);

END CATCH;
