-- Migration 0003: Add FileType, FileStoragePath, DuplicateStrategy to ImportBatches
-- These columns were missing from the original 0001_FullSchema.sql and are required
-- by the import service. Safe to run multiple times (IF COL_LENGTH guards).

SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.ImportBatches', 'FileType') IS NULL
    ALTER TABLE dbo.ImportBatches
        ADD [FileType] nvarchar(10) NOT NULL CONSTRAINT [DF_ImportBatches_FileType] DEFAULT ('csv');
GO

IF COL_LENGTH('dbo.ImportBatches', 'FileStoragePath') IS NULL
    ALTER TABLE dbo.ImportBatches
        ADD [FileStoragePath] nvarchar(500) NULL;
GO

IF COL_LENGTH('dbo.ImportBatches', 'DuplicateStrategy') IS NULL
    ALTER TABLE dbo.ImportBatches
        ADD [DuplicateStrategy] nvarchar(20) NOT NULL CONSTRAINT [DF_ImportBatches_DuplicateStrategy] DEFAULT ('skip');
GO
