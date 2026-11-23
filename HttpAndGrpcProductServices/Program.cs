using HttpAndGrpcProductServices.Application;
using HttpAndGrpcProductServices.Infrastructure;
using HttpAndGrpcProductServices.Grpc;
using HttpAndGrpcProductServices.Http;
using FluentMigrator.Runner;

namespace HttpAndGrpcProductServices
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddHttpPresentation();
            builder.Services.AddGrpcPresentation();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                runner.MigrateUp();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/http/swagger.json", "Product HTTP API");
                c.SwaggerEndpoint("/swagger/grpc/swagger.json", "Product gRPC transcoding API");
            });

            app.UseHttpPresentation();
            app.UseGrpcPresentation();

            app.Run();
        }
    }
}
