-- ============================================================
--  Seed Data: Organizations (MERGE)
--  6 records with exact IDs preserved for FK consistency.
--
--  MERGE behaviour:
--    MATCHED     => updates all fields with latest values
--    NOT MATCHED => inserts as new record
--
--  Safe to run multiple times. Existing records are updated,
--  new records are inserted. ValidFrom / ValidTo are managed
--  automatically by SQL Server temporal versioning.
-- ============================================================

SET NOCOUNT ON

BEGIN TRANSACTION

BEGIN TRY

    -- Source data as a derived table
    ;WITH source AS (
        SELECT *
        FROM (VALUES
			(CAST('3CFDCADA-ADC0-F011-B692-A0B339B26E42' AS UNIQUEIDENTIFIER), 'Apex Dynamics Inc.'					, 'Apex Dynamics Incorporated'					, 'Apex Dynamics'		, 'ADX'	, 'ADX7K9P2M4Q8R5T1V3W6Y9Z0B2', 'https://www.apexdynamics.com'		, '5551234567', 'ApexDynamics@mailinator.com'	, CAST(1 AS BIT), CAST('2025-11-13 16:28:59.107' AS DATETIME), 'ADH', CAST('2025-11-13 16:28:59.107' AS DATETIME), 'ADH'),
			(CAST('3DFDCADA-ADC0-F011-B692-A0B339B26E42' AS UNIQUEIDENTIFIER), 'Nexus Solutions LLC'				, 'Nexus Solutions Limited Liability Company'	, 'Nexus Solutions'		, 'NSX'	, 'NSX3F8H1J5L9N2P4Q7R0T6U8V1', 'https://www.nexussolutions.io'		, '2025550123', 'NexSolLLC@mailinator.com'		, CAST(0 AS BIT), CAST('2025-11-13 16:28:59.107' AS DATETIME), 'ADH', CAST('2025-11-13 16:28:59.107' AS DATETIME), 'ADH'),
			(CAST('3EFDCADA-ADC0-F011-B692-A0B339B26E42' AS UNIQUEIDENTIFIER), 'The Cleveland Clinic Foundation'	, 'The Cleveland Clinic Foundation'				, 'Cleveland Clinic'	, 'CCF'	, 'CCF9V04GSASKGWYWJJNXR2Y3ES', 'https://my.clevelandclinic.org/'	, '8002232273', 'testCCF@mailinator.com'		, CAST(1 AS BIT), CAST('2025-11-13 16:28:59.107' AS DATETIME), 'ADH', CAST('2025-11-13 16:28:59.107' AS DATETIME), 'ADH'),
			(CAST('3FFDCADA-ADC0-F011-B692-A0B339B26E42' AS UNIQUEIDENTIFIER), 'Bethel University'					, 'Bethel University'							, 'Bethel University'	, 'BETH', 'BETHU04GSAUMS5YWJJNXR2Y3ES', 'https://www.bethel.edu/'			, '6516366400', 'testbu@mailinator.com'			, CAST(1 AS BIT), CAST('2025-11-13 16:28:59.107' AS DATETIME), 'ADH', CAST('2025-11-13 16:28:59.107' AS DATETIME), 'ADH'),
			(CAST('40FDCADA-ADC0-F011-B692-A0B339B26E42' AS UNIQUEIDENTIFIER), 'Horizon Ventures Group'				, 'Horizon Ventures Group'						, 'Horizon Ventures'	, 'HZV'	, 'HZV9A2C4E6G8J1K3M5P7R0T2V4', 'https://horizonventures.co'		, '8887770987', 'HorizonVentGr@mailinator.com'	, CAST(1 AS BIT), CAST('2025-11-13 16:28:59.107' AS DATETIME), 'ADH', CAST('2025-11-13 16:28:59.107' AS DATETIME), 'ADH'),
			(CAST('41FDCADA-ADC0-F011-B692-A0B339B26E42' AS UNIQUEIDENTIFIER), 'Quantum Edge Technologies Ltd.'		, 'Quantum Edge Technologies Ltd.'				, 'Quantum Edge'		, 'QET' , 'QET5B7D9F2H4J6L8N1P3R5T0V2', 'https://www.quantumedgetech.com'	, '3105551234', 'quantumedgetech@mailinator.com', CAST(1 AS BIT), CAST('2025-11-13 16:28:59.107' AS DATETIME), 'ADH', CAST('2025-11-13 16:28:59.107' AS DATETIME), 'ADH')
        ) AS s (
            Id, Name, FilingName, MarketingName, Abbreviation, OrganizationCode, Website, Phone, CompanyEmail, IsActive, CreateUtcDt, CreatedBy, ModifiedUtcDt, ModifiedBy
        )
    )
    MERGE dbo.Organizations AS target
    USING source
        ON target.Id = source.Id

    -- ---- Record exists: update all fields ----
    WHEN MATCHED THEN
        UPDATE SET
            target.Name             = source.Name,
            target.FilingName       = source.FilingName,
            target.MarketingName    = source.MarketingName,
            target.Abbreviation     = source.Abbreviation,
            target.OrganizationCode = source.OrganizationCode,
            target.Website          = source.Website,
            target.Phone            = source.Phone,
            target.CompanyEmail     = source.CompanyEmail,
            target.IsActive         = source.IsActive,
            target.ModifiedUtcDt    = SYSUTCDATETIME(),
            target.ModifiedBy       = source.ModifiedBy

    -- ---- Record does not exist: insert ----
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Id			, Name			, FilingName		, MarketingName			, Abbreviation			, OrganizationCode			, Website		, Phone			, CompanyEmail			, IsActive			, CreateUtcDt		, CreatedBy			, ModifiedUtcDt			, ModifiedBy)
        VALUES (source.Id	, source.Name	, source.FilingName	, source.MarketingName	, source.Abbreviation	, source.OrganizationCode	, source.Website, source.Phone	, source.CompanyEmail	, source.IsActive	, source.CreateUtcDt, source.CreatedBy	, source.ModifiedUtcDt	, source.ModifiedBy)

    -- Capture row counts per action for the summary
    OUTPUT
        $action                             AS [Action],
        COALESCE(inserted.Name, deleted.Name) AS [Organization],
        COALESCE(inserted.Abbreviation, deleted.Abbreviation) AS [Abbreviation],
        CAST(COALESCE(inserted.IsActive, deleted.IsActive) AS NVARCHAR(5)) AS [Active]
    ;

    COMMIT TRANSACTION

    PRINT 'Organizations MERGE completed successfully.'

END TRY
BEGIN CATCH

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION

    DECLARE @ErrMessage  NVARCHAR(4000) = ERROR_MESSAGE()
    DECLARE @ErrSeverity INT            = ERROR_SEVERITY()
    DECLARE @ErrState    INT            = ERROR_STATE()
    DECLARE @ErrLine     INT            = ERROR_LINE()

    PRINT '============================================================'
    PRINT 'MERGE FAILED — all changes have been rolled back.'
    PRINT '------------------------------------------------------------'
    PRINT 'Line    : ' + CAST(@ErrLine AS NVARCHAR(10))
    PRINT 'Message : ' + @ErrMessage
    PRINT '============================================================'

    RAISERROR(@ErrMessage, @ErrSeverity, @ErrState)

END CATCH