using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetQuest.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DotnetQuest.Api.Tests.Infrastructure;

/// <summary>
/// Gerçek uygulamayı bellekte ayağa kaldırır (gerçek pipeline, gerçek EF Core + PostgreSQL).
/// Bağlantı bilgisi uygulamanın kendi ayarından (yerelde user-secrets) alınır, sadece veritabanı adı değiştirilir:
/// her test çalıştırması kendi geçici veritabanını oluşturur ve sonunda siler.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private string? _connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var baseConnectionString = context.Configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException(
                    "Testler için ConnectionStrings:Default ayarı gerekli. README'deki 'Çalıştırma' adımlarına bakın.");

            _connectionString = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(baseConnectionString))
            {
                Database = $"dotnetquest_tests_{Guid.NewGuid():N}",
            }.ConnectionString;

            config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = _connectionString });
        });

        // Testler hızlı ve çok istek attığı için rate limit'i gevşetiyoruz.
        builder.UseSetting("RateLimiting:GameplayPermitLimit", "100000");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100000");
    }

    /// <summary>Kayıt olup cookie ile giriş yapmış bir istemci döner.</summary>
    public async Task<HttpClient> CreateLoggedInClientAsync(string? displayName = null)
    {
        var client = CreateClient();
        var email = $"player-{Guid.NewGuid():N}@example.com";
        const string password = "sifre12345";

        (await client.PostAsJsonAsync("/api/auth/register", new { email, password })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email, password })).EnsureSuccessStatusCode();

        if (displayName is not null)
        {
            (await client.PutAsJsonAsync("/api/players/me", new { displayName })).EnsureSuccessStatusCode();
        }

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DropTestDatabase();
    }

    private void DropTestDatabase()
    {
        if (_connectionString is null)
        {
            return;
        }

        // Kendi veritabanımızı silerken ona bağlı olamayız; sunucudaki varsayılan "postgres" veritabanına bağlanıyoruz.
        var connection = new NpgsqlConnectionStringBuilder(_connectionString);
        var testDatabase = connection.Database;
        connection.Database = "postgres";

        NpgsqlConnection.ClearAllPools();
        using var db = new NpgsqlConnection(connection.ConnectionString);
        db.Open();
        using var command = db.CreateCommand();

        // WITH (FORCE): açık kalmış bağlantı olsa bile siler (PostgreSQL 13+).
        command.CommandText = $"DROP DATABASE IF EXISTS \"{testDatabase}\" WITH (FORCE)";
        command.ExecuteNonQuery();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
