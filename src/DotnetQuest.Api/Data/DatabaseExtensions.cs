using Microsoft.EntityFrameworkCore;

namespace DotnetQuest.Api.Data;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        // Connection string'i lambda içinde okumak, testlerin onu kolayca değiştirebilmesini sağlar.
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            // Şifre içerdiği için connection string repoda tutulmaz: yerelde "dotnet user-secrets",
            // sunucuda ConnectionStrings__Default ortam değişkeni ile verilir.
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Default")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:Default ayarı bulunamadı. Yerelde 'dotnet user-secrets' ile, sunucuda " +
                    "'ConnectionStrings__Default' ortam değişkeniyle tanımlayın (README'deki adımlara bakın).");

            options.UseNpgsql(PostgresConnectionString.Normalize(connectionString));

            // EF Core 9+: seed işlemi Migrate() sırasında otomatik çalışır.
            options.UseAsyncSeeding((context, _, cancellationToken) => ContentSeeder.SeedAsync(context, cancellationToken));
        });

        return services;
    }

    /// <summary>
    /// Bekleyen migration'ları uygular ve içeriği seed'ler.
    /// Not: Büyük, çok instance'lı production ortamlarında migration'ları deploy pipeline'ında
    /// (migration bundle veya idempotent SQL script) uygulamak daha doğrudur. Bu proje gibi tek instance'lı bir uygulama için yeterli.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
