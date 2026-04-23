
-- ============================================================
--  FIELD_SECTIONS  (optional grouping)
--  Organises fields into labelled sections within a form,
--  e.g. "Personal details", "Employment", "Preferences".
-- ============================================================

CREATE TABLE FieldSections (
	[Id]					[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[OrganizationId]		[uniqueidentifier]	NOT NULL,
	[SectionName]			nvarchar(150)		NOT NULL,
	[DisplayOrder]			int					NOT NULL	DEFAULT(0),
	[IsActive]				bit					NOT NULL	DEFAULT(1),

	CONSTRAINT [PK_FieldSections] PRIMARY KEY CLUSTERED (Id),
	
	CONSTRAINT [FK_FieldSections_Organizations] 
		FOREIGN KEY([OrganizationId])
		REFERENCES [dbo].[Organizations] ([Id]),
)

CREATE	INDEX IX_FieldSectionsOrganization ON FieldSections (OrganizationId)
