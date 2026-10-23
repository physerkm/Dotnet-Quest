using DotnetQuest.Api.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DotnetQuest.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<Player>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<SessionQuestion> SessionQuestions => Set<SessionQuestion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Her entity'nin ayarları kendi IEntityTypeConfiguration sınıfında (Data/Configurations).
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
