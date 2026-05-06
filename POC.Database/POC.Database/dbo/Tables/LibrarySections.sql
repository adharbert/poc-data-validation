-- ============================================================
--  LIBRARY_SECTIONS
--  Named groups of reusable fields (e.g. "Personal Information",
--  "Address", "Education").  Sections are global — not scoped
--  to any organisation.  Admins import selected sections into
--  an org's Inputs page, which copies the fields at that point
--  in time (no ongoing reference back to the library).
-- ============================================================
CREATE TABLE LibrarySections (
	[Id]			[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[SectionName]	nvarchar(200)		NOT NULL,
	[Description]	nvarchar(500)		NULL,
	[DisplayOrder]	int					NOT NULL	DEFAULT (0),
	[IsActive]		bit					NOT NULL	DEFAULT (1),
	[CreatedDt]		datetime			NOT NULL	DEFAULT (GETUTCDATE()),
	[ModifiedDt]	datetime			NOT NULL	DEFAULT (GETUTCDATE()),

	CONSTRAINT [PK_LibrarySections] PRIMARY KEY CLUSTERED (Id),

	CONSTRAINT [UQ_LibrarySections_Name] UNIQUE ([SectionName])
)
GO
