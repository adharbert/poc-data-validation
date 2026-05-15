CREATE PROCEDURE [dbo].[usp_GetCustomerOrders]
    @CustomerId  INT,
    @Status      NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.[OrderId],
        o.[OrderDate],
        o.[TotalAmount],
        o.[Status]
    FROM [dbo].[Order] AS o
    WHERE o.[CustomerId] = @CustomerId
      AND (@Status IS NULL OR o.[Status] = @Status)
    ORDER BY o.[OrderDate] DESC;
END;
