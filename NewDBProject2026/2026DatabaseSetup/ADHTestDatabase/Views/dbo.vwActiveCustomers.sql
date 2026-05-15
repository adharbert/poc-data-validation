CREATE VIEW [dbo].[vwActiveCustomers]
AS
SELECT
    c.[CustomerId],
    c.[FirstName],
    c.[LastName],
    c.[Email],
    c.[CreatedDate],
    COUNT(o.[OrderId])    AS [TotalOrders],
    SUM(o.[TotalAmount])  AS [TotalSpend]
FROM [dbo].[Customer] AS c
LEFT JOIN [dbo].[Order] AS o
    ON o.[CustomerId] = c.[CustomerId]
WHERE c.[IsActive] = 1
GROUP BY
    c.[CustomerId],
    c.[FirstName],
    c.[LastName],
    c.[Email],
    c.[CreatedDate];
