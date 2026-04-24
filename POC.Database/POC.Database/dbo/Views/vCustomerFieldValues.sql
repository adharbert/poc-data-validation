CREATE VIEW [dbo].[vCustomerFieldValues] AS
SELECT
    c.Id                                        AS OrganizationId,
    c.Name                                      AS OrganizationName,
    cu.Id                                       AS CustomerId,
    cu.FirstName + ' ' + cu.LastName            AS customer_name,
    fd.Id                                       AS FieldId,
    fd.FieldKey,
    fd.FieldLabel,
    fd.FieldType,
    fd.DsiplayOrder                             AS DisplayOrder,
    fd.IsRequired,
    fs.SectionName,
    fs.DisplayOrder                             AS section_order,
    fv.Id                                       AS ValueId,
    fv.ValueText,
    fv.ValueNumber,
    fv.ValueDate,
    fv.ValueDatetime,
    fv.ValueBoolean,
    COALESCE(
        fv.ValueText,
        CAST(fv.ValueNumber AS nvarchar(50)),
        CONVERT(nvarchar(20), fv.ValueDate, 23),
        CONVERT(nvarchar(30), fv.ValueDatetime, 120),
        CASE fv.ValueBoolean
            WHEN 1 THEN 'Yes'
            WHEN 0 THEN 'No'
            ELSE NULL
        END
    )                                           AS display_value,
    fv.ConfirmedAt,
    fv.FlaggedAt,
    fv.FlagNote
FROM        [dbo].[FieldDefinitions]  fd
JOIN        [dbo].[Organizations]     c  ON c.Id             = fd.OrganizationId
JOIN        [dbo].[Customers]         cu ON cu.OrganizationId = fd.OrganizationId
LEFT JOIN   [dbo].[FieldSections]     fs ON fs.Id            = fd.FieldSectionId
LEFT JOIN   [dbo].[FieldValues]       fv ON fv.FieldDefinitionId = fd.Id
                                        AND fv.CustomerId        = cu.Id
WHERE fd.IsActive = 1
  AND cu.IsActive = 1;
