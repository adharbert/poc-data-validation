-- Migration 0004: Allow one CSV column to map to multiple destination tables
-- Drops the header uniqueness constraint and widens the destination table/field
-- check constraints to include customer_email and customer_phone.
-- Safe to run multiple times.

SET NOCOUNT ON;
GO

-- 1. Drop unique constraint so the same CSV header can map to multiple destinations
--    (e.g. "Email" -> field_value AND customer_email simultaneously)
IF EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = 'UQ_ImportColumnMappings_Header'
      AND parent_object_id = OBJECT_ID('dbo.ImportColumnMappings')
)
    ALTER TABLE dbo.ImportColumnMappings DROP CONSTRAINT [UQ_ImportColumnMappings_Header];
GO

-- 2. Widen destination table check to include customer_email and customer_phone
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_ImportColumnMappings_DestinationTable'
      AND parent_object_id = OBJECT_ID('dbo.ImportColumnMappings')
)
    ALTER TABLE dbo.ImportColumnMappings DROP CONSTRAINT [CK_ImportColumnMappings_DestinationTable];
GO

ALTER TABLE dbo.ImportColumnMappings
    ADD CONSTRAINT [CK_ImportColumnMappings_DestinationTable] CHECK (
        DestinationTable IN (
            'customer', 'customer_address', 'customer_email', 'customer_phone',
            'field_value', 'skip'
        )
    );
GO

-- 3. Widen destination field check to allow customer_email / customer_phone rows
--    (those rows may have NULL DestinationField when the column maps to the primary field)
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_ImportColumnMappings_DestinationField'
      AND parent_object_id = OBJECT_ID('dbo.ImportColumnMappings')
)
    ALTER TABLE dbo.ImportColumnMappings DROP CONSTRAINT [CK_ImportColumnMappings_DestinationField];
GO

ALTER TABLE dbo.ImportColumnMappings
    ADD CONSTRAINT [CK_ImportColumnMappings_DestinationField] CHECK (
        DestinationTable IN ('skip', 'field_value', 'customer_email', 'customer_phone')
        OR TransformType <> 'direct'
        OR (DestinationTable = 'customer' AND DestinationField IN (
                'FirstName', 'LastName', 'MiddleName', 'MaidenName', 'DateOfBirth',
                'Phone', 'Email', 'OriginalId', 'CustomerCode'
            ))
        OR (DestinationTable = 'customer_address' AND DestinationField IN (
                'AddressLine1', 'AddressLine2', 'City', 'State', 'PostalCode',
                'Country', 'AddressType', 'Latitude', 'Longitude'
            ))
    );
GO
