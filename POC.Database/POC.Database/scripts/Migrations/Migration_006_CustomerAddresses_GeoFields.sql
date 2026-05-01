-- ============================================================
--  Migration 009 — CustomerAddresses: Latitude, Longitude, GeographyPoint
--
--  Adds geocoordinate columns to CustomerAddresses.
--
--  Latitude / Longitude — populated by the API after Melissa
--  address validation returns coordinates, or when an admin
--  manually geocodes an address.
--
--  GeographyPoint — non-persisted computed column (geography type,
--  SRID 4326). Used for spatial queries (radius search, nearest
--  address, etc.). Not mapped in application entities; query via
--  SQL directly using STDistance(), STWithin(), etc.
--
--  Temporal versioning stays on; nullable ADD columns propagate
--  to the history table automatically.
-- ============================================================
SET NOCOUNT ON;

BEGIN TRANSACTION;
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE  object_id = OBJECT_ID('dbo.CustomerAddresses')
          AND  name = 'Latitude'
    )
    BEGIN
        ALTER TABLE dbo.CustomerAddresses
            ADD [Latitude] FLOAT NULL;

        PRINT 'CustomerAddresses.Latitude column added.';
    END
    ELSE
        PRINT 'CustomerAddresses.Latitude already exists — skipped.';

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE  object_id = OBJECT_ID('dbo.CustomerAddresses')
          AND  name = 'Longitude'
    )
    BEGIN
        ALTER TABLE dbo.CustomerAddresses
            ADD [Longitude] FLOAT NULL;

        PRINT 'CustomerAddresses.Longitude column added.';
    END
    ELSE
        PRINT 'CustomerAddresses.Longitude already exists — skipped.';

    -- GeographyPoint requires disabling temporal versioning and cannot go in this script.
    -- Run Migration_009b_CustomerAddresses_GeographyPoint.sql separately after this one.

    COMMIT TRANSACTION;
    PRINT 'Migration 009 completed successfully.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Migration 009 FAILED — transaction rolled back.';
    THROW;
END CATCH;
