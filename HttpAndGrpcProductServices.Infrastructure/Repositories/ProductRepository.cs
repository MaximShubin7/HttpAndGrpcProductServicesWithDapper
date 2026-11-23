using Dapper;
using HttpAndGrpcProductServices.Domain.Abstractions;
using HttpAndGrpcProductServices.Domain.Enums;
using HttpAndGrpcProductServices.Domain.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace HttpAndGrpcProductServices.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly string _connectionString;

    public ProductRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found");
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<Guid> AddAsync(Product product, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO products (id, name, price, weight, type, created_at, warehouse_number)
            VALUES (@Id, @Name, @Price, @Weight, @Type, @CreatedAt, @WarehouseNumber)
            """;

        var parameters = new
        {
            product.Id,
            product.Name,
            product.Price,
            product.Weight,
            Type = (int)product.Type,
            product.CreatedAt,
            product.WarehouseNumber
        };

        using var connection = CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return product.Id;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id            AS Id,
                   name          AS Name,
                   price         AS Price,
                   weight        AS Weight,
                   type          AS Type,
                   created_at    AS CreatedAt,
                   warehouse_number AS WarehouseNumber
            FROM products
            WHERE id = @Id
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Product>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetFilteredAsync(
        DateTime? createdFrom,
        DateTime? createdTo,
        ProductType? type,
        int? warehouseNumber,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (createdFrom.HasValue)
        {
            where.Add("created_at >= @CreatedFrom");
            parameters.Add("CreatedFrom", createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            where.Add("created_at <= @CreatedTo");
            parameters.Add("CreatedTo", createdTo.Value);
        }

        if (type.HasValue)
        {
            where.Add("type = @Type");
            parameters.Add("Type", (int)type.Value);
        }

        if (warehouseNumber.HasValue)
        {
            where.Add("warehouse_number = @WarehouseNumber");
            parameters.Add("WarehouseNumber", warehouseNumber.Value);
        }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        var countSql = $"SELECT COUNT(*) FROM products {whereClause}";
        var itemsSql = $"""
            SELECT id            AS Id,
                   name          AS Name,
                   price         AS Price,
                   weight        AS Weight,
                   type          AS Type,
                   created_at    AS CreatedAt,
                   warehouse_number AS WarehouseNumber
            FROM products
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @Limit OFFSET @Offset
            """;

        parameters.Add("Limit", pageSize);
        parameters.Add("Offset", (pageNumber - 1) * pageSize);

        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));
        var items = (await connection.QueryAsync<Product>(
            new CommandDefinition(itemsSql, parameters, cancellationToken: ct))).ToList();

        return (items, total);
    }

    public async Task<bool> UpdateAsync(Product product, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE products
            SET name = @Name,
                price = @Price,
                weight = @Weight,
                type = @Type,
                warehouse_number = @WarehouseNumber
            WHERE id = @Id
            """;

        var parameters = new
        {
            product.Id,
            product.Name,
            product.Price,
            product.Weight,
            Type = (int)product.Type,
            product.WarehouseNumber
        };

        using var connection = CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: ct));
        return rows > 0;
    }
}