namespace HttpAndGrpcProductServices.Application.Dtos
{
    public class UpdateProductDto
    {
        public string Name { get; set; } = null!;

        public decimal Price { get; set; }

        public double Weight { get; set; }

        public int Type { get; set; }

        public int WarehouseNumber { get; set; }
    }
}
