using Microsoft.AspNetCore.Identity;

namespace DotnetQuest.Api.Domain;

public sealed class Player : IdentityUser
{
    public string? DisplayName { get; set; }
    public int TotalXp { get; set; }
}
