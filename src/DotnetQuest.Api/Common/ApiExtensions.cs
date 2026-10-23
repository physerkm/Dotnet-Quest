using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DotnetQuest.Api.Data;
using DotnetQuest.Api.Domain;
using Microsoft.Extensions.Options;

namespace DotnetQuest.Api.Common;

public static class ApiExtensions
{
    public static IServiceCollection AddDotnetQuestApi(this IServiceCollection services)
    {
        // Hatalar ve boş 4xx/5xx yanıtları RFC 9457 "application/problem+json" formatında döner.
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // .NET 10: DataAnnotations ile Minimal API doğrulaması (geçersiz istek → 400 ValidationProblem).
        services.AddValidation();

        // Enum'lar JSON'da "Easy" gibi okunabilir string olarak gider/gelir.
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddOpenApi();

        // DateTime.UtcNow yerine TimeProvider: testlerde zamanı kontrol edebiliriz.
        services.AddSingleton(TimeProvider.System);

        services.AddDatabase();

        // Identity: kayıt/giriş endpoint'leri hazır gelir. React aynı origin'den çağırdığı için HttpOnly cookie kullanıyoruz
        // (token'ı localStorage'da saklamaktan daha güvenli: JavaScript cookie'yi okuyamaz).
        services.AddIdentityApiEndpoints<Player>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>();
        services.AddAuthorization();

        services.AddRateLimiting();
        services.AddOutputCache();
        services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

        return services;
    }

    private static void AddRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitSettings>().BindConfiguration(RateLimitSettings.SectionName);

        services.AddRateLimiter(options =>
        {
            // Varsayılan 503'tür; "çok fazla istek" için doğru kod 429.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                return ValueTask.CompletedTask;
            };

            options.AddPolicy(RateLimitPolicies.Gameplay, context =>
            {
                var settings = context.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.GameplayPermitLimit,
                    Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                });
            });

            // Brute-force saldırılarına karşı giriş/kayıt daha sıkı sınırlanır.
            options.AddPolicy(RateLimitPolicies.Auth, context =>
            {
                var settings = context.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.AuthPermitLimit,
                    Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                });
            });
        });
    }

    /// <summary>Giriş yapmış oyuncular kullanıcı id'sine, diğerleri IP adresine göre sınırlanır.</summary>
    private static string PartitionKey(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "anonymous";
}

public static class RateLimitPolicies
{
    public const string Gameplay = "gameplay";
    public const string Auth = "auth";
}

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public int GameplayPermitLimit { get; set; } = 60;
    public int AuthPermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
}
