-- ============================================================
--  Migration 009 — Global Field Library
--
--  Adds four tables for the boilerplate field library:
--    LibrarySections      — named groups (Personal Info, Address, etc.)
--    LibraryFields        — reusable field templates (no org FK)
--    LibrarySectionFields — junction: which fields belong to which section
--    LibraryFieldOptions  — dropdown/multiselect options for library fields
--
--  Seed data included:
--    Personal Information, Contact Information, Address, Education
-- ============================================================
SET NOCOUNT ON;

BEGIN TRANSACTION;
BEGIN TRY

-- -------------------------------------------------------
-- Tables
-- -------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.LibrarySections'))
BEGIN
    CREATE TABLE [dbo].[LibrarySections] (
        [Id]            [uniqueidentifier]   NOT NULL    DEFAULT (newsequentialid()),
        [SectionName]   [nvarchar](200)      NOT NULL,
        [Description]   [nvarchar](500)      NULL,
        [DisplayOrder]  [int]                NOT NULL    DEFAULT (0),
        [IsActive]      [bit]                NOT NULL    DEFAULT (1),
        [CreatedDt]     [datetime]           NOT NULL    DEFAULT (GETUTCDATE()),
        [ModifiedDt]    [datetime]           NOT NULL    DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_LibrarySections] PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT [UQ_LibrarySections_Name] UNIQUE ([SectionName])
    );
    PRINT 'LibrarySections table created.';
END
ELSE PRINT 'LibrarySections already exists — skipped.';


IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.LibraryFields'))
BEGIN
    CREATE TABLE [dbo].[LibraryFields] (
        [Id]                [uniqueidentifier]   NOT NULL    DEFAULT (newsequentialid()),
        [FieldKey]          [nvarchar](100)      NOT NULL,
        [FieldLabel]        [nvarchar](200)      NOT NULL,
        [FieldType]         [nvarchar](20)       NOT NULL,
        [PlaceHolderText]   [nvarchar](200)      NULL,
        [HelpText]          [nvarchar](500)      NULL,
        [IsRequired]        [bit]                NOT NULL    DEFAULT (0),
        [DisplayOrder]      [int]                NOT NULL    DEFAULT (0),
        [MinValue]          [decimal](18,4)      NULL,
        [MaxValue]          [decimal](18,4)      NULL,
        [MinLength]         [int]                NULL,
        [MaxLength]         [int]                NULL,
        [RegExPattern]      [nvarchar](500)      NULL,
        [DisplayFormat]     [nvarchar](20)       NULL,
        [IsActive]          [bit]                NOT NULL    DEFAULT (1),
        [CreatedDt]         [datetime]           NOT NULL    DEFAULT (GETUTCDATE()),
        [ModifiedDt]        [datetime]           NOT NULL    DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_LibraryFields] PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT [UQ_LibraryFields_Key] UNIQUE ([FieldKey]),
        CONSTRAINT [CK_LibraryFields_Type] CHECK (
            [FieldType] IN ('text','number','date','datetime','checkbox','dropdown','multiselect','phone')
        )
    );
    PRINT 'LibraryFields table created.';
END
ELSE PRINT 'LibraryFields already exists — skipped.';


IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.LibrarySectionFields'))
BEGIN
    CREATE TABLE [dbo].[LibrarySectionFields] (
        [Id]                [uniqueidentifier]   NOT NULL    DEFAULT (newsequentialid()),
        [LibrarySectionId]  [uniqueidentifier]   NOT NULL,
        [LibraryFieldId]    [uniqueidentifier]   NOT NULL,
        [DisplayOrder]      [int]                NOT NULL    DEFAULT (0),
        CONSTRAINT [PK_LibrarySectionFields] PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT [UQ_LibrarySectionFields] UNIQUE ([LibrarySectionId], [LibraryFieldId]),
        CONSTRAINT [FK_LibrarySectionFields_Section]
            FOREIGN KEY ([LibrarySectionId]) REFERENCES [dbo].[LibrarySections] ([Id]),
        CONSTRAINT [FK_LibrarySectionFields_Field]
            FOREIGN KEY ([LibraryFieldId]) REFERENCES [dbo].[LibraryFields] ([Id])
    );
    CREATE INDEX [IX_LibrarySectionFields_Section]
        ON [dbo].[LibrarySectionFields] ([LibrarySectionId], [DisplayOrder]);
    PRINT 'LibrarySectionFields table created.';
END
ELSE PRINT 'LibrarySectionFields already exists — skipped.';


IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.LibraryFieldOptions'))
BEGIN
    CREATE TABLE [dbo].[LibraryFieldOptions] (
        [Id]                [uniqueidentifier]   NOT NULL    DEFAULT (newsequentialid()),
        [LibraryFieldId]    [uniqueidentifier]   NOT NULL,
        [OptionKey]         [nvarchar](100)      NOT NULL,
        [OptionLabel]       [nvarchar](200)      NOT NULL,
        [DisplayOrder]      [int]                NOT NULL    DEFAULT (0),
        [IsActive]          [bit]                NOT NULL    DEFAULT (1),
        CONSTRAINT [PK_LibraryFieldOptions] PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT [UQ_LibraryFieldOptions_Key] UNIQUE ([LibraryFieldId], [OptionKey]),
        CONSTRAINT [FK_LibraryFieldOptions_Field]
            FOREIGN KEY ([LibraryFieldId]) REFERENCES [dbo].[LibraryFields] ([Id])
    );
    CREATE INDEX [IX_LibraryFieldOptions_Field]
        ON [dbo].[LibraryFieldOptions] ([LibraryFieldId], [DisplayOrder]);
    PRINT 'LibraryFieldOptions table created.';
END
ELSE PRINT 'LibraryFieldOptions already exists — skipped.';


-- -------------------------------------------------------
-- Seed: Sections
-- -------------------------------------------------------

DECLARE @SecPersonal    uniqueidentifier = 'A1000000-0000-0000-0000-000000000001';
DECLARE @SecContact     uniqueidentifier = 'A1000000-0000-0000-0000-000000000002';
DECLARE @SecAddress     uniqueidentifier = 'A1000000-0000-0000-0000-000000000003';
DECLARE @SecEducation   uniqueidentifier = 'A1000000-0000-0000-0000-000000000004';

IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySections WHERE Id = @SecPersonal)
    INSERT INTO dbo.LibrarySections (Id, SectionName, Description, DisplayOrder)
    VALUES (@SecPersonal, 'Personal Information', 'Core demographic fields: name, date of birth, gender.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySections WHERE Id = @SecContact)
    INSERT INTO dbo.LibrarySections (Id, SectionName, Description, DisplayOrder)
    VALUES (@SecContact, 'Contact Information', 'Email address and phone number fields.', 2);

IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySections WHERE Id = @SecAddress)
    INSERT INTO dbo.LibrarySections (Id, SectionName, Description, DisplayOrder)
    VALUES (@SecAddress, 'Address', 'Mailing and physical address fields.', 3);

IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySections WHERE Id = @SecEducation)
    INSERT INTO dbo.LibrarySections (Id, SectionName, Description, DisplayOrder)
    VALUES (@SecEducation, 'Education', 'Academic background fields.', 4);


-- -------------------------------------------------------
-- Seed: Fields
-- -------------------------------------------------------

DECLARE @FldFirstName       uniqueidentifier = 'B1000000-0000-0000-0000-000000000001';
DECLARE @FldLastName        uniqueidentifier = 'B1000000-0000-0000-0000-000000000002';
DECLARE @FldMiddleName      uniqueidentifier = 'B1000000-0000-0000-0000-000000000003';
DECLARE @FldMaidenName      uniqueidentifier = 'B1000000-0000-0000-0000-000000000004';
DECLARE @FldDateOfBirth     uniqueidentifier = 'B1000000-0000-0000-0000-000000000005';
DECLARE @FldGender          uniqueidentifier = 'B1000000-0000-0000-0000-000000000006';
DECLARE @FldEmail           uniqueidentifier = 'B1000000-0000-0000-0000-000000000007';
DECLARE @FldPhone           uniqueidentifier = 'B1000000-0000-0000-0000-000000000008';
DECLARE @FldAltPhone        uniqueidentifier = 'B1000000-0000-0000-0000-000000000009';
DECLARE @FldAddr1           uniqueidentifier = 'B1000000-0000-0000-0000-000000000010';
DECLARE @FldAddr2           uniqueidentifier = 'B1000000-0000-0000-0000-000000000011';
DECLARE @FldCity            uniqueidentifier = 'B1000000-0000-0000-0000-000000000012';
DECLARE @FldState           uniqueidentifier = 'B1000000-0000-0000-0000-000000000013';
DECLARE @FldPostalCode      uniqueidentifier = 'B1000000-0000-0000-0000-000000000014';
DECLARE @FldCountry         uniqueidentifier = 'B1000000-0000-0000-0000-000000000015';
DECLARE @FldDegree          uniqueidentifier = 'B1000000-0000-0000-0000-000000000016';
DECLARE @FldGradYear        uniqueidentifier = 'B1000000-0000-0000-0000-000000000017';
DECLARE @FldFieldOfStudy    uniqueidentifier = 'B1000000-0000-0000-0000-000000000018';

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldFirstName)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldFirstName, 'lib_first_name', 'First Name', 'text', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldLastName)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldLastName, 'lib_last_name', 'Last Name', 'text', 1, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldMiddleName)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldMiddleName, 'lib_middle_name', 'Middle Name', 'text', 0, 3);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldMaidenName)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldMaidenName, 'lib_maiden_name', 'Maiden Name', 'text', 0, 4);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldDateOfBirth)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldDateOfBirth, 'lib_date_of_birth', 'Date of Birth', 'date', 0, 5);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldGender)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldGender, 'lib_gender', 'Gender', 'dropdown', 0, 6);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldEmail)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, HelpText, IsRequired, DisplayOrder)
    VALUES (@FldEmail, 'lib_email', 'Email Address', 'text', 'Primary email address.', 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldPhone)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder, DisplayFormat)
    VALUES (@FldPhone, 'lib_phone', 'Primary Phone', 'phone', 0, 2, '(XXX) XXX-XXXX');

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldAltPhone)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder, DisplayFormat)
    VALUES (@FldAltPhone, 'lib_alt_phone', 'Alternate Phone', 'phone', 0, 3, '(XXX) XXX-XXXX');

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldAddr1)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldAddr1, 'lib_address_line1', 'Address Line 1', 'text', 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldAddr2)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldAddr2, 'lib_address_line2', 'Address Line 2', 'text', 0, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldCity)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldCity, 'lib_city', 'City', 'text', 0, 3);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldState)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldState, 'lib_state', 'State', 'dropdown', 0, 4);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldPostalCode)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder, MaxLength)
    VALUES (@FldPostalCode, 'lib_postal_code', 'ZIP / Postal Code', 'text', 0, 5, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldCountry)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldCountry, 'lib_country', 'Country', 'text', 0, 6);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldDegree)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldDegree, 'lib_highest_degree', 'Highest Degree', 'dropdown', 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldGradYear)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder, MinValue, MaxValue)
    VALUES (@FldGradYear, 'lib_graduation_year', 'Graduation Year', 'number', 0, 2, 1900, 2100);

IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFields WHERE Id = @FldFieldOfStudy)
    INSERT INTO dbo.LibraryFields (Id, FieldKey, FieldLabel, FieldType, IsRequired, DisplayOrder)
    VALUES (@FldFieldOfStudy, 'lib_field_of_study', 'Field of Study', 'text', 0, 3);


-- -------------------------------------------------------
-- Seed: Section ↔ Field assignments
-- -------------------------------------------------------

-- Personal Information
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecPersonal AND LibraryFieldId = @FldFirstName)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecPersonal, @FldFirstName, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecPersonal AND LibraryFieldId = @FldLastName)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecPersonal, @FldLastName, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecPersonal AND LibraryFieldId = @FldMiddleName)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecPersonal, @FldMiddleName, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecPersonal AND LibraryFieldId = @FldMaidenName)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecPersonal, @FldMaidenName, 4);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecPersonal AND LibraryFieldId = @FldDateOfBirth)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecPersonal, @FldDateOfBirth, 5);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecPersonal AND LibraryFieldId = @FldGender)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecPersonal, @FldGender, 6);

-- Contact Information
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecContact AND LibraryFieldId = @FldEmail)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecContact, @FldEmail, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecContact AND LibraryFieldId = @FldPhone)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecContact, @FldPhone, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecContact AND LibraryFieldId = @FldAltPhone)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecContact, @FldAltPhone, 3);

-- Address
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecAddress AND LibraryFieldId = @FldAddr1)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecAddress, @FldAddr1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecAddress AND LibraryFieldId = @FldAddr2)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecAddress, @FldAddr2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecAddress AND LibraryFieldId = @FldCity)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecAddress, @FldCity, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecAddress AND LibraryFieldId = @FldState)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecAddress, @FldState, 4);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecAddress AND LibraryFieldId = @FldPostalCode)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecAddress, @FldPostalCode, 5);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecAddress AND LibraryFieldId = @FldCountry)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecAddress, @FldCountry, 6);

-- Education
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecEducation AND LibraryFieldId = @FldDegree)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecEducation, @FldDegree, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecEducation AND LibraryFieldId = @FldGradYear)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecEducation, @FldGradYear, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SecEducation AND LibraryFieldId = @FldFieldOfStudy)
    INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder) VALUES (@SecEducation, @FldFieldOfStudy, 3);


-- -------------------------------------------------------
-- Seed: Field Options
-- -------------------------------------------------------

-- Gender options
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldGender AND OptionKey = 'male')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldGender, 'male', 'Male', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldGender AND OptionKey = 'female')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldGender, 'female', 'Female', 2);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldGender AND OptionKey = 'nonbinary')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldGender, 'nonbinary', 'Non-binary', 3);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldGender AND OptionKey = 'prefer_not')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldGender, 'prefer_not', 'Prefer not to say', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldGender AND OptionKey = 'other')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldGender, 'other', 'Other', 5);

-- State options (all 50 US states)
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'AL') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'AL', 'Alabama', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'AK') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'AK', 'Alaska', 2);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'AZ') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'AZ', 'Arizona', 3);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'AR') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'AR', 'Arkansas', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'CA') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'CA', 'California', 5);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'CO') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'CO', 'Colorado', 6);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'CT') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'CT', 'Connecticut', 7);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'DE') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'DE', 'Delaware', 8);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'FL') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'FL', 'Florida', 9);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'GA') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'GA', 'Georgia', 10);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'HI') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'HI', 'Hawaii', 11);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'ID') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'ID', 'Idaho', 12);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'IL') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'IL', 'Illinois', 13);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'IN') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'IN', 'Indiana', 14);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'IA') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'IA', 'Iowa', 15);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'KS') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'KS', 'Kansas', 16);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'KY') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'KY', 'Kentucky', 17);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'LA') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'LA', 'Louisiana', 18);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'ME') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'ME', 'Maine', 19);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'MD') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'MD', 'Maryland', 20);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'MA') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'MA', 'Massachusetts', 21);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'MI') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'MI', 'Michigan', 22);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'MN') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'MN', 'Minnesota', 23);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'MS') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'MS', 'Mississippi', 24);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'MO') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'MO', 'Missouri', 25);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'MT') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'MT', 'Montana', 26);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'NE') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'NE', 'Nebraska', 27);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'NV') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'NV', 'Nevada', 28);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'NH') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'NH', 'New Hampshire', 29);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'NJ') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'NJ', 'New Jersey', 30);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'NM') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'NM', 'New Mexico', 31);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'NY') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'NY', 'New York', 32);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'NC') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'NC', 'North Carolina', 33);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'ND') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'ND', 'North Dakota', 34);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'OH') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'OH', 'Ohio', 35);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'OK') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'OK', 'Oklahoma', 36);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'OR') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'OR', 'Oregon', 37);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'PA') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'PA', 'Pennsylvania', 38);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'RI') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'RI', 'Rhode Island', 39);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'SC') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'SC', 'South Carolina', 40);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'SD') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'SD', 'South Dakota', 41);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'TN') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'TN', 'Tennessee', 42);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'TX') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'TX', 'Texas', 43);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'UT') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'UT', 'Utah', 44);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'VT') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'VT', 'Vermont', 45);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'VA') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'VA', 'Virginia', 46);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'WA') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'WA', 'Washington', 47);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'WV') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'WV', 'West Virginia', 48);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'WI') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'WI', 'Wisconsin', 49);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldState AND OptionKey = 'WY') INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldState, 'WY', 'Wyoming', 50);

-- Degree options
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldDegree AND OptionKey = 'hs_ged')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldDegree, 'hs_ged', 'High School Diploma / GED', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldDegree AND OptionKey = 'associate')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldDegree, 'associate', 'Associate Degree', 2);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldDegree AND OptionKey = 'bachelor')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldDegree, 'bachelor', 'Bachelor''s Degree', 3);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldDegree AND OptionKey = 'master')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldDegree, 'master', 'Master''s Degree', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldDegree AND OptionKey = 'doctoral')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldDegree, 'doctoral', 'Doctoral Degree', 5);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldDegree AND OptionKey = 'professional')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldDegree, 'professional', 'Professional Degree (JD, MD, etc.)', 6);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldDegree AND OptionKey = 'certificate')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldDegree, 'certificate', 'Certificate', 7);
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryFieldOptions WHERE LibraryFieldId = @FldDegree AND OptionKey = 'other')
    INSERT INTO dbo.LibraryFieldOptions (LibraryFieldId, OptionKey, OptionLabel, DisplayOrder) VALUES (@FldDegree, 'other', 'Other', 8);


    COMMIT TRANSACTION;
    PRINT 'Migration 009 completed successfully.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Migration 009 FAILED — transaction rolled back.';
    THROW;
END CATCH;
