using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Orders;
using CanvasArt.API.Models;
using CanvasArt.API.Models.Entities;
using CanvasArt.API.Models.Enums;
using Dapper;

namespace CanvasArt.API.Repository;

public sealed class OrderRepository : RepositoryBase, IOrderRepository
{
    private const string OrderColumns = """
        Id, OrderNumber, FirstName, LastName, Email, Phone, AddressLine, City, Country, PostalCode,
        Notes, Status, SubTotal, DiscountTotal, ShippingCost, GrandTotal, CreatedAt, UpdatedAt
        """;

    public OrderRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<PagedResult<OrderListItemDto>> QueryAsync(OrderQuery query, CancellationToken cancellationToken = default)
    {
        var sortColumn = query.SortBy?.ToLowerInvariant() switch
        {
            "total" => "o.GrandTotal",
            "status" => "o.Status",
            "number" => "o.OrderNumber",
            _ => "o.CreatedAt"
        };
        var direction = query.IsDescending ? "DESC" : "ASC";

        var filters = """
            WHERE (@Status IS NULL OR o.Status = @Status)
              AND (@FromDate IS NULL OR o.CreatedAt >= @FromDate)
              AND (@ToDate IS NULL OR o.CreatedAt < @ToDate)
              AND (@Search IS NULL OR o.OrderNumber LIKE @Like OR o.Email LIKE @Like OR o.FirstName LIKE @Like OR o.LastName LIKE @Like)
            """;

        var sql = $"""
            SELECT o.Id, o.OrderNumber, (o.FirstName + ' ' + o.LastName) AS CustomerName, o.Email, o.Status, o.GrandTotal,
                   (SELECT COUNT(1) FROM dbo.OrderItems oi WHERE oi.OrderId = o.Id) AS ItemCount, o.CreatedAt
            FROM dbo.Orders o
            {filters}
            ORDER BY {sortColumn} {direction}, o.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1) FROM dbo.Orders o {filters};
            """;

        var parameters = new
        {
            Status = (int?)query.Status,
            query.FromDate,
            query.ToDate,
            query.Search,
            Like = $"%{query.Search}%",
            query.Offset,
            query.PageSize
        };

        using var conn = await OpenAsync(cancellationToken);
        using var multi = await conn.QueryMultipleAsync(Command(sql, parameters, cancellationToken));
        var items = (await multi.ReadAsync<OrderListItemDto>()).ToList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<OrderListItemDto>(items, total, query.Page, query.PageSize);
    }

    public Task<OrderAggregate?> GetAggregateByIdAsync(int id, CancellationToken cancellationToken = default) =>
        GetAggregateAsync("o.Id = @Key", id, cancellationToken);

    public Task<OrderAggregate?> GetAggregateByNumberAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        GetAggregateAsync("o.OrderNumber = @Key", orderNumber, cancellationToken);

    private async Task<OrderAggregate?> GetAggregateAsync(string whereClause, object key, CancellationToken ct)
    {
        var sql = $"""
            SELECT {OrderColumns} FROM dbo.Orders o WHERE {whereClause};

            SELECT oi.Id, oi.OrderId, oi.PaintingId, oi.PaintingSizeId, oi.FrameId, oi.FrameSizeId,
                   oi.PaintingCode, oi.PaintingName, oi.SizeLabel, oi.FrameName, oi.FrameSizeLabel, oi.ThumbnailPath,
                   oi.UnitPrice, oi.FramePrice, oi.DiscountAmount, oi.Quantity, oi.LineTotal, oi.AppliedPromotionId, oi.AppliedCombinationPromotionId
            FROM dbo.OrderItems oi
            INNER JOIN dbo.Orders o ON o.Id = oi.OrderId
            WHERE {whereClause}
            ORDER BY oi.Id;

            SELECT h.Id, h.OrderId, h.FromStatus, h.ToStatus, h.Note, h.ChangedByUserId, h.CreatedAt,
                   CASE WHEN u.Id IS NULL THEN NULL ELSE (u.FirstName + ' ' + u.LastName) END AS ChangedByName
            FROM dbo.OrderStatusHistories h
            INNER JOIN dbo.Orders o ON o.Id = h.OrderId
            LEFT JOIN dbo.Users u ON u.Id = h.ChangedByUserId
            WHERE {whereClause}
            ORDER BY h.CreatedAt, h.Id;
            """;

        using var conn = await OpenAsync(ct);
        using var multi = await conn.QueryMultipleAsync(Command(sql, new { Key = key }, ct));

        var order = await multi.ReadSingleOrDefaultAsync<Order>();
        if (order is null)
            return null;

        var items = (await multi.ReadAsync<OrderItem>()).ToList();
        var history = (await multi.ReadAsync<OrderStatusHistory>()).ToList();
        return new OrderAggregate(order, items, history);
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = $"SELECT {OrderColumns} FROM dbo.Orders WHERE Id = @Id;";
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Order>(Command(sql, new { Id = id }, cancellationToken));
    }

    public async Task<int> CreateAsync(Order order, IReadOnlyList<OrderItem> items, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        using var tx = conn.BeginTransaction();
        try
        {
            const string orderSql = """
                INSERT INTO dbo.Orders
                    (OrderNumber, FirstName, LastName, Email, Phone, AddressLine, City, Country, PostalCode, Notes, Status, SubTotal, DiscountTotal, ShippingCost, GrandTotal, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.Id
                VALUES
                    (@OrderNumber, @FirstName, @LastName, @Email, @Phone, @AddressLine, @City, @Country, @PostalCode, @Notes, @Status, @SubTotal, @DiscountTotal, @ShippingCost, @GrandTotal, @CreatedAt, @UpdatedAt);
                """;
            var orderId = await conn.ExecuteScalarAsync<int>(Command(orderSql, order, cancellationToken, tx));

            foreach (var item in items)
            {
                // Atomically decrement stock; a zero row-count means insufficient stock.
                var paintingRows = await conn.ExecuteAsync(Command(
                    "UPDATE dbo.PaintingSizes SET Stock = Stock - @Qty WHERE Id = @SizeId AND Stock >= @Qty;",
                    new { Qty = item.Quantity, SizeId = item.PaintingSizeId }, cancellationToken, tx));
                if (paintingRows == 0)
                    throw new ConflictException($"'{item.PaintingName}' ({item.SizeLabel}) is out of stock.");

                if (item.FrameSizeId is int frameSizeId)
                {
                    var frameRows = await conn.ExecuteAsync(Command(
                        "UPDATE dbo.FrameSizes SET Stock = Stock - @Qty WHERE Id = @SizeId AND Stock >= @Qty;",
                        new { Qty = item.Quantity, SizeId = frameSizeId }, cancellationToken, tx));
                    if (frameRows == 0)
                        throw new ConflictException($"Frame '{item.FrameName}' ({item.FrameSizeLabel}) is out of stock.");
                }

                const string itemSql = """
                    INSERT INTO dbo.OrderItems
                        (OrderId, PaintingId, PaintingSizeId, FrameId, FrameSizeId, PaintingCode, PaintingName, SizeLabel, FrameName, FrameSizeLabel, ThumbnailPath, UnitPrice, FramePrice, DiscountAmount, Quantity, LineTotal, AppliedPromotionId, AppliedCombinationPromotionId)
                    VALUES
                        (@OrderId, @PaintingId, @PaintingSizeId, @FrameId, @FrameSizeId, @PaintingCode, @PaintingName, @SizeLabel, @FrameName, @FrameSizeLabel, @ThumbnailPath, @UnitPrice, @FramePrice, @DiscountAmount, @Quantity, @LineTotal, @AppliedPromotionId, @AppliedCombinationPromotionId);
                    """;
                item.OrderId = orderId;
                await conn.ExecuteAsync(Command(itemSql, item, cancellationToken, tx));
            }

            const string historySql = """
                INSERT INTO dbo.OrderStatusHistories (OrderId, FromStatus, ToStatus, Note, ChangedByUserId, CreatedAt)
                VALUES (@OrderId, NULL, @ToStatus, @Note, NULL, @CreatedAt);
                """;
            await conn.ExecuteAsync(Command(historySql, new
            {
                OrderId = orderId,
                ToStatus = (int)OrderStatus.Pending,
                Note = "Order placed.",
                order.CreatedAt
            }, cancellationToken, tx));

            tx.Commit();
            return orderId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> ChangeStatusAsync(
        int orderId, OrderStatus newStatus, string? note, int? changedByUserId, DateTime whenUtc, CancellationToken cancellationToken = default)
    {
        using var conn = await OpenAsync(cancellationToken);
        using var tx = conn.BeginTransaction();
        try
        {
            var current = await conn.ExecuteScalarAsync<int?>(Command(
                "SELECT Status FROM dbo.Orders WHERE Id = @Id;", new { Id = orderId }, cancellationToken, tx));
            if (current is null)
            {
                tx.Rollback();
                return false;
            }

            await conn.ExecuteAsync(Command(
                "UPDATE dbo.Orders SET Status = @Status, UpdatedAt = @When WHERE Id = @Id;",
                new { Id = orderId, Status = (int)newStatus, When = whenUtc }, cancellationToken, tx));

            await conn.ExecuteAsync(Command("""
                INSERT INTO dbo.OrderStatusHistories (OrderId, FromStatus, ToStatus, Note, ChangedByUserId, CreatedAt)
                VALUES (@OrderId, @FromStatus, @ToStatus, @Note, @ChangedByUserId, @CreatedAt);
                """, new
            {
                OrderId = orderId,
                FromStatus = current,
                ToStatus = (int)newStatus,
                Note = note,
                ChangedByUserId = changedByUserId,
                CreatedAt = whenUtc
            }, cancellationToken, tx));

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<OrderStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                COUNT(1) AS Total,
                SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS Pending,
                SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) AS Processing,
                SUM(CASE WHEN Status = 5 THEN 1 ELSE 0 END) AS Delivered,
                SUM(CASE WHEN Status = 6 THEN 1 ELSE 0 END) AS Cancelled,
                ISNULL(SUM(CASE WHEN Status = 5 THEN GrandTotal ELSE 0 END), 0) AS RevenueDelivered
            FROM dbo.Orders;
            """;
        using var conn = await OpenAsync(cancellationToken);
        return await conn.QuerySingleAsync<OrderStatsDto>(Command(sql, null, cancellationToken));
    }
}
