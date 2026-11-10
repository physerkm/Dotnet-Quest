using DotnetQuest.Api.Data;
using Npgsql;

namespace DotnetQuest.Api.Tests;

public sealed class PostgresConnectionStringTests
{
    [Fact]
    public void Normalize_ConvertsNeonStyleUri()
    {
        var result = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(
            "postgresql://neondb_owner:p%40ss@ep-cool-name-123.eu-central-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require"));

        Assert.Equal("ep-cool-name-123.eu-central-1.aws.neon.tech", result.Host);
        Assert.Equal(5432, result.Port);
        Assert.Equal("neondb", result.Database);
        Assert.Equal("neondb_owner", result.Username);
        Assert.Equal("p@ss", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
        Assert.Equal(ChannelBinding.Require, result.ChannelBinding);
    }

    [Fact]
    public void Normalize_ReadsPortAndHyphenatedSslMode()
    {
        var result = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(
            "postgres://user:secret@db.example.com:6543/game?sslmode=verify-full"));

        Assert.Equal(6543, result.Port);
        Assert.Equal("game", result.Database);
        Assert.Equal(SslMode.VerifyFull, result.SslMode);
    }

    [Fact]
    public void Normalize_LeavesNpgsqlFormatUnchanged()
    {
        const string connectionString = "Host=localhost;Database=dotnetquest;Username=postgres";

        Assert.Equal(connectionString, PostgresConnectionString.Normalize(connectionString));
    }
}
