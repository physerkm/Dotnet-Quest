using DotnetQuest.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotnetQuest.Api.Data.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasMaxLength(32);
        builder.Property(c => c.Title).HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Icon).HasMaxLength(16);

        builder.HasMany(c => c.Questions)
            .WithOne(q => q.Category)
            .HasForeignKey(q => q.CategoryId);
    }
}

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.Property(q => q.Key).HasMaxLength(64);
        builder.HasIndex(q => q.Key).IsUnique();

        // Enum'ları string olarak saklamak veritabanını okunabilir kılar ve enum sırası değişirse veriyi bozmaz.
        builder.Property(q => q.Difficulty).HasConversion<string>().HasMaxLength(16);
        builder.Property(q => q.Type).HasConversion<string>().HasMaxLength(16);
        builder.Property(q => q.Prompt).HasMaxLength(1000);
        builder.Property(q => q.Code).HasMaxLength(4000);
        builder.Property(q => q.Explanation).HasMaxLength(2000);

        // Oyun başlatırken "kategori + zorluk" ile sorgulanıyor.
        builder.HasIndex(q => new { q.CategoryId, q.Difficulty });
    }
}

internal sealed class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Difficulty).HasConversion<string>().HasMaxLength(16);
        builder.Property(s => s.Version).IsConcurrencyToken();

        builder.HasOne(s => s.Category)
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Questions)
            .WithOne()
            .HasForeignKey(q => q.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.PlayerId);
    }
}

internal sealed class SessionQuestionConfiguration : IEntityTypeConfiguration<SessionQuestion>
{
    public void Configure(EntityTypeBuilder<SessionQuestion> builder)
    {
        builder.HasKey(q => new { q.SessionId, q.QuestionId });

        builder.HasOne(q => q.Question)
            .WithMany()
            .HasForeignKey(q => q.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.Property(p => p.DisplayName).HasMaxLength(24);

        // Liderlik tablosu XP'ye göre sıralanıyor.
        builder.HasIndex(p => p.TotalXp);
    }
}
