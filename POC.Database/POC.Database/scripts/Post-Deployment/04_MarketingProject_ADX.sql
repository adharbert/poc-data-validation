SET NOCOUNT ON

-- Insert marketing project for OrganizationId 3CFDCADA-ADC0-F011-B692-A0B339B26E42
-- ProjectName: ADX Data Validation
-- Term: 2026-05-05 through 2027-01-08

BEGIN TRANSACTION;
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM dbo.MarketingProjects
        WHERE ProjectName  = 'ADX Data Validation'
          AND OrganizationId = '3CFDCADA-ADC0-F011-B692-A0B339B26E42'
    )
    BEGIN
        INSERT INTO dbo.MarketingProjects
            (OrganizationId, ProjectName, MarketingStartDate, MarketingEndDate, IsActive, CreatedBy)
        VALUES
            (
                '3CFDCADA-ADC0-F011-B692-A0B339B26E42',
                'ADX Data Validation',
                '2026-05-05',
                '2027-01-08',
                1,
                'andrew.harbert@publishingconcepts.com'
            );

        PRINT 'Marketing project ADX Data Validation inserted.';
    END
    ELSE
        PRINT 'Marketing project ADX Data Validation already exists — skipped.';

    COMMIT TRANSACTION;

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Insert FAILED — transaction rolled back.';
    THROW;
END CATCH;
