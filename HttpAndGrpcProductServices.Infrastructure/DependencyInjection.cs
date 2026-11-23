using FluentMigrator.Runner;
using HttpAndGrpcProductServices.Domain.Abstractions;
using HttpAndGrpcProductServices.Infrastructure.Migrations;
using HttpAndGrpcProductServices.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HttpAndGrpcProductServices.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found");

        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(cs)
                .ScanIn(typeof(CreateProductsTable).Assembly).For.Migrations());

        return services;
    }
}
