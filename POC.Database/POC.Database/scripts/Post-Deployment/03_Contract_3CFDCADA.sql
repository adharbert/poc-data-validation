SET NOCOUNT ON

-- Insert contract for OrganizationId 3CFDCADA-ADC0-F011-B692-A0B339B26E42
-- ContractNumber: PCI2600142 (10 chars)
-- Term: 2026-01-01 through 2026-08-15

BEGIN TRANSACTION;
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Contracts
        WHERE ContractNumber = 'PCI2600142'
          AND OrganizationId = '3CFDCADA-ADC0-F011-B692-A0B339B26E42'
    )
    BEGIN
        INSERT INTO dbo.Contracts
            (OrganizationId, ContractName, ContractNumber, StartDate, EndDate, IsActive, CreatedBy)
        VALUES
            (
                '3CFDCADA-ADC0-F011-B692-A0B339B26E42',
                'Annual Service Agreement 2026',
                'PCI2600142',
                '2026-01-01',
                '2026-08-15',
                1,
                'andrew.harbert@publishingconcepts.com'
            );

        PRINT 'Contract PCI2600142 inserted.';
    END
    ELSE
        PRINT 'Contract PCI2600142 already exists — skipped.';

    COMMIT TRANSACTION;

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Insert FAILED — transaction rolled back.';
    THROW;
END CATCH;
