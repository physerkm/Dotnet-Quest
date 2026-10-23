using DotnetQuest.Api.Common;
using DotnetQuest.Api.Data;
using DotnetQuest.Api.Domain;
using DotnetQuest.Api.Features.Categories;
using DotnetQuest.Api.Features.Leaderboard;
using DotnetQuest.Api.Features.Players;
using DotnetQuest.Api.Features.Sessions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDotnetQuestApi();

var app = builder.Build();

await app.InitializeDatabaseAsync();

// --- Middleware pipeline: SIRA ÖNEMLİ! ---
// 1) Exception handler en başta: sonraki tüm middleware'lerin hatalarını yakalar.
app.UseExceptionHandler();
// 2) Gövdesi boş 4xx/5xx yanıtlarını (ör. 404, 429) ProblemDetails'e çevirir.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // OpenAPI dokümanı: /openapi/v1.json, interaktif arayüz: /scalar
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("DotNet Quest API"));
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// 3) Production'da React build'i (wwwroot) buradan sunulur; statik dosyalar auth'a girmeden kısa devre yapar.
app.UseDefaultFiles();
app.UseStaticFiles();

// 4) Önce "kimsin?" (authentication), sonra "yetkin var mı?" (authorization).
app.UseAuthentication();
app.UseAuthorization();

// 5) Rate limiter kullanıcı id'sine göre bölümlediği için authentication'dan SONRA gelir.
app.UseRateLimiter();
app.UseOutputCache();

var api = app.MapGroup("/api");

var auth = api.MapGroup("/auth").WithTags("Auth").RequireRateLimiting(RateLimitPolicies.Auth);
auth.MapIdentityApi<Player>();
auth.MapLogout();

api.MapCategoryEndpoints();
api.MapSessionEndpoints();
api.MapLeaderboardEndpoints();
api.MapPlayerEndpoints();

app.MapHealthChecks("/health");

// Bilinmeyen /api/* adresleri React'e değil 404'e düşsün; diğer her şey React router'a gitsin.
api.MapFallback(() => TypedResults.NotFound());
app.MapFallbackToFile("index.html");

app.Run();

// Integration testlerdeki WebApplicationFactory<Program> için.
public partial class Program;
