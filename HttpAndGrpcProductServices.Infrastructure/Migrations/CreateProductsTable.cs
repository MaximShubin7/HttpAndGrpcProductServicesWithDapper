using FluentMigrator;

namespace HttpAndGrpcProductServices.Infrastructure.Migrations;

[Migration(202501010001)]
public class CreateProductsTable : Migration
{
    public override void Up()
    {
        Create.Table("products")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(500).NotNullable()
            .WithColumn("price").AsDecimal(18, 2).NotNullable()
            .WithColumn("weight").AsDouble().NotNullable()
            .WithColumn("type").AsInt32().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("warehouse_number").AsInt32().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("products");
    }
}