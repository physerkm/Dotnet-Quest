using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DotnetQuest.Api.Common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Giriş yapmış oyuncunun id'si; anonim isteklerde null.</summary>
    public static string? GetPlayerId(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier);
}

/// <summary>Tutarlı hata yanıtları üretmek için küçük yardımcılar.</summary>
public static class ApiProblems
{
    public static ProblemHttpResult Conflict(string detail) =>
        TypedResults.Problem(title: "Çakışma", detail: detail, statusCode: StatusCodes.Status409Conflict);

    public static ValidationProblem Validation(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
