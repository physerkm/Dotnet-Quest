using Npgsql;

namespace DotnetQuest.Api.Data;

/// <summary>
/// Neon, Render ve Railway gibi servisler bağlantı bilgisini URI olarak verir:
/// <c>postgresql://kullanici:sifre@sunucu:5432/veritabani?sslmode=require</c>.
/// Npgsql ise <c>Host=...;Username=...</c> biçimini bekler. Bu sınıf sayesinde ikisi de kabul edilir.
/// </summary>
public static class PostgresConnectionString
{
    public static string Normalize(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return value; // Zaten Npgsql biçiminde.
        }

        var uri = new Uri(value);
        var credentials = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
        };

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = parameter.Split('=', 2);
            var name = Uri.UnescapeDataString(parts[0]).ToLowerInvariant();

            // "verify-full" → "verifyfull": Npgsql'deki enum adlarıyla eşleşsin.
            var setting = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]).Replace("-", "") : "";

            switch (name)
            {
                case "sslmode":
                    builder.SslMode = Enum.Parse<SslMode>(setting, ignoreCase: true);
                    break;
                case "channel_binding":
                    builder.ChannelBinding = Enum.Parse<ChannelBinding>(setting, ignoreCase: true);
                    break;

                // Diğer parametrelerin Npgsql'de karşılığı yok; yok sayılır.
            }
        }

        return builder.ConnectionString;
    }
}
