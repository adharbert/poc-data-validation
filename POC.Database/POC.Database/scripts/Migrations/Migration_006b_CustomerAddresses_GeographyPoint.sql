-- ============================================================
--  Migration 006b — CustomerAddresses: GeographyPoint computed column
--
--  Adds a non-persisted computed column (geography type, SRID 4326)
--  derived from Latitude and Longitude. Adding a computed column to
--  a temporal table requires disabling system versioning first —
--  SSDT cannot model this (SQL71610), so it lives here as a manual
--  migration that also runs via Script.PostDeployment1.sql.
--
--  Safe to re-run: each batch is independently guarded.
--  Batches are separated by GO so SSDT accepts this as a valid
--  post-deploy :r include (avoids SQL71006).
-- ============================================================
SET NOCOUNT ON;

-- Step 1: Disable system versioning (only if column does not yet exist)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.CustomerAddresses')
      AND  name = 'GeographyPoint'
)
BEGIN
    ALTER TABLE dbo.CustomerAddresses
        SET (SYSTEM_VERSIONING = OFF);
    PRINT 'System versioning disabled.';
END
ELSE
    PRINT 'CustomerAddresses.GeographyPoint already exists — skipped.';
GO

-- Step 2: Add the computed column (only if absent)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.CustomerAddresses')
      AND  name = 'GeographyPoint'
)
BEGIN
    ALTER TABLE dbo.CustomerAddresses
        ADD [GeographyPoint] AS (
            CASE
                WHEN [Latitude]  IS NOT NULL
                 AND [Longitude] IS NOT NULL
                THEN geography::Point([Latitude], [Longitude], 4326)
                ELSE NULL
            END
        );
    PRINT 'CustomerAddresses.GeographyPoint column added.';
END
GO

-- Step 3: Re-enable system versioning if it was disabled by this script
IF OBJECTPROPERTY(OBJECT_ID('dbo.CustomerAddresses'), 'TableTemporalType') <> 2
BEGIN
    ALTER TABLE dbo.CustomerAddresses
        SET (SYSTEM_VERSIONING = ON (
            HISTORY_TABLE          = dbo.CustomerAddresses_History,
            DATA_CONSISTENCY_CHECK = ON
        ));
    PRINT 'System versioning re-enabled.';
    PRINT 'Migration 006b completed successfully.';
END
GO
