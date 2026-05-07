-- ============================================================
--  Post-Deployment 06 — CustomerAddresses.GeographyPoint
--
--  Adds the computed geography column derived from Latitude and
--  Longitude. This CANNOT live in dbo/Tables/CustomerAddresses.sql
--  because SSDT does not support computed columns on temporal tables
--  (SQL71610 error). It must be applied as a post-deployment step.
--
--  Safe to re-run: each batch is independently guarded.
--  GO separators are required so SSDT accepts this as a valid
--  :r include without SQL71006 errors.
-- ============================================================

SET NOCOUNT ON;

-- Step 1: Disable system versioning (only if the column does not yet exist)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.CustomerAddresses')
      AND  name = 'GeographyPoint'
)
BEGIN
    ALTER TABLE dbo.CustomerAddresses
        SET (SYSTEM_VERSIONING = OFF);
    PRINT '06: System versioning disabled.';
END
ELSE
    PRINT '06: CustomerAddresses.GeographyPoint already exists — skipped.';
GO

-- Step 2: Add the computed column to the main table and a matching column
--         to the history table so column counts stay in sync for SYSTEM_VERSIONING.
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
    PRINT '06: CustomerAddresses.GeographyPoint column added.';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.CustomerAddresses_History')
      AND  name = 'GeographyPoint'
)
BEGIN
    ALTER TABLE dbo.CustomerAddresses_History
        ADD [GeographyPoint] [geography] NULL;
    PRINT '06: CustomerAddresses_History.GeographyPoint column added.';
END
GO

-- Step 3: Re-enable system versioning if it was disabled above
IF OBJECTPROPERTY(OBJECT_ID('dbo.CustomerAddresses'), 'TableTemporalType') <> 2
BEGIN
    ALTER TABLE dbo.CustomerAddresses
        SET (SYSTEM_VERSIONING = ON (
            HISTORY_TABLE          = dbo.CustomerAddresses_History,
            DATA_CONSISTENCY_CHECK = ON
        ));
    PRINT '06: System versioning re-enabled.';
END
GO
