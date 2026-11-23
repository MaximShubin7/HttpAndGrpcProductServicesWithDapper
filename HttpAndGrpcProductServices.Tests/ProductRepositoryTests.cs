using Dapper;
using HttpAndGrpcProductServices.Domain.Enums;
using HttpAndGrpcProductServices.Domain.Models;
using HttpAndGrpcProductServices.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Tests.Integration;

public class ProductRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer db = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("test_db")
        .WithUsername("postgres")
        .WithPassword("qwerty123")
        .Build();

    private ProductRepository repository = null!;

    public async Task InitializeAsync()
    {
        await db.StartAsync();
        var cs = db.GetConnectionString();

        await using var conn = new NpgsqlConnection(cs);
        await conn.ExecuteAsync("""
            CREATE TABLE products (
                id UUID PRIMARY KEY,
                name VARCHAR(500) NOT NULL,
                price NUMERIC(18,2) NOT NULL,
                weight DOUBLE PRECISION NOT NULL,
                type INTEGER NOT NULL,
                created_at TIMESTAMP NOT NULL,
                warehouse_number INTEGER NOT NULL
            );
            """);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = cs })
            .Build();

        repository = new ProductRepository(config);
    }

    public async Task DisposeAsync() => await db.DisposeAsync();

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsProductWithEnum()
    {
        var p = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Порошок",
            Price = 299.99m,
            Weight = 1.5,
            Type = ProductType.HouseholdChemistry,
            CreatedAt = DateTime.UtcNow,
            WarehouseNumber = 3
        };

        await repository.AddAsync(p);
        var loaded = await repository.GetByIdAsync(p.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Порошок", loaded!.Name);
        Assert.Equal(ProductType.HouseholdChemistry, loaded.Type);
        Assert.Equal(3, loaded.WarehouseNumber);
        Assert.Equal(299.99m, loaded.Price);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        var result = await repository.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByTypeAndWarehouse()
    {
        for (int i = 0; i < 5; i++)
        {
            await repository.AddAsync(new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Товар {i}",
                Price = 100 + i,
                Weight = 1,
                Type = i % 2 == 0 ? ProductType.Technics : ProductType.Food,
                CreatedAt = DateTime.UtcNow.AddMinutes(i),
                WarehouseNumber = i % 2 == 0 ? 5 : 9
            });
        }

        var (items, total) = await repository.GetFilteredAsync(null, null, ProductType.Technics, 5, 1, 10);

        Assert.Equal(3, total);
        Assert.All(items, p => Assert.Equal(ProductType.Technics, p.Type));
        Assert.All(items, p => Assert.Equal(5, p.WarehouseNumber));
    }

    [Fact]
    public async Task GetFilteredAsync_Paginates()
    {
        for (int i = 0; i < 5; i++)
        {
            await repository.AddAsync(new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Товар {i}",
                Price = 100,
                Weight = 1,
                Type = ProductType.General,
                CreatedAt = DateTime.UtcNow.AddMinutes(i),
                WarehouseNumber = 1
            });
        }

        var (items, total) = await repository.GetFilteredAsync(null, null, null, null, pageNumber: 2, pageSize: 2);

        Assert.Equal(5, total);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var p = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Старое",
            Price = 10,
            Weight = 1,
            Type = ProductType.General,
            CreatedAt = DateTime.UtcNow,
            WarehouseNumber = 1
        };
        await repository.AddAsync(p);

        p.Name = "Новое";
        p.Price = 99.99m;
        p.Type = ProductType.Food;
        p.WarehouseNumber = 42;

        var updated = await repository.UpdateAsync(p);
        var reloaded = await repository.GetByIdAsync(p.Id);

        Assert.True(updated);
        Assert.Equal("Новое", reloaded!.Name);
        Assert.Equal(99.99m, reloaded.Price);
        Assert.Equal(ProductType.Food, reloaded.Type);
        Assert.Equal(42, reloaded.WarehouseNumber);
    }

    [Fact]
    public async Task UpdateAsync_MissingId_ReturnsFalse()
    {
        var p = new Product
        {
            Id = Guid.NewGuid(),
            Name = "X",
            Price = 1,
            Weight = 1,
            Type = ProductType.General,
            CreatedAt = DateTime.UtcNow,
            WarehouseNumber = 1
        };

        var updated = await repository.UpdateAsync(p);

        Assert.False(updated);
    }
}