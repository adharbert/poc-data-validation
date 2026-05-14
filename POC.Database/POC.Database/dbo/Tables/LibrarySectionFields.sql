-- ============================================================
--  LIBRARY_SECTION_FIELDS
--  Junction table linking library fields to library sections.
--  DisplayOrder controls the order fields appear within a section.
-- ============================================================
CREATE TABLE LibrarySectionFields (
	[Id]				[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[LibrarySectionId]	[uniqueidentifier]	NOT NULL,
	[LibraryFieldId]	[uniqueidentifier]	NOT NULL,
	[DisplayOrder]		int					NOT NULL	DEFAULT (0),

	CONSTRAINT [PK_LibrarySectionFields] PRIMARY KEY CLUSTERED (Id),

	CONSTRAINT [UQ_LibrarySectionFields] UNIQUE ([LibrarySectionId], [LibraryFieldId]),

	CONSTRAINT [FK_LibrarySectionFields_Section]
		FOREIGN KEY ([LibrarySectionId])
		REFERENCES [dbo].[LibrarySections] ([Id]),

	CONSTRAINT [FK_LibrarySectionFields_Field]
		FOREIGN KEY ([LibraryFieldId])
		REFERENCES [dbo].[LibraryFields] ([Id])
)
GO

CREATE INDEX IX_LibrarySectionFields_Section ON LibrarySectionFields ([LibrarySectionId], [DisplayOrder]);
GO
