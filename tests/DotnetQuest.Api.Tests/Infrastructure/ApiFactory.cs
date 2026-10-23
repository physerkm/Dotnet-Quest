using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace DotnetQuest.Api.Tests.Infrastructure;

/// <summary>
/// Gerçek uygulamayı bellekte ayağa kaldırır (gerçek pipeline, gerçek EF Core + SQLite).
/// Her test çalıştırması kendi geçici veritabanını kullanır.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"dotnetquest-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");

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

        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_databasePath);
        }
        catch (IOException)
        {
            // Geçici dosya; silinemezse sorun değil.
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
