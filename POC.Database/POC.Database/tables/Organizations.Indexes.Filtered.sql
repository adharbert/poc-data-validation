CREATE UNIQUE INDEX [UQ_Organizations_Abbreviation] ON [dbo].[Organizations] ([Abbreviation]) WHERE [Abbreviation] IS NOT NULL;
