using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;
using WelfareSystem.Api.Services;

if (args.Length != 3) throw new ArgumentException("Supply the local configuration file, isolated staging database and private credential output file.");
var settings = JsonDocument.Parse(File.ReadAllText(args[0]));
var source = new NpgsqlConnectionStringBuilder(settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString());
var staging = args[1];
if (!Regex.IsMatch(staging, "^welfare_azure_prepare_[a-f0-9]{32}$") || staging == source.Database)
    throw new InvalidOperationException("Credential preparation is restricted to a new isolated staging database.");
var output = Path.GetFullPath(args[2]);
if (File.Exists(output)) throw new InvalidOperationException("The credential file already exists; it will not be overwritten.");
source.Database = staging;
await using var db = new NpgsqlConnection(source.ConnectionString);
await db.OpenAsync();
await using var transaction = await db.BeginTransactionAsync();
var accounts = new List<(int Id, string Email, string Role, string Hash)>();
await using (var query = new NpgsqlCommand("SELECT u.\"Id\", u.\"Email\", r.\"Name\", u.\"PasswordHash\" FROM \"Users\" u JOIN \"Roles\" r ON r.\"Id\"=u.\"RoleId\" WHERE u.\"IsActive\" ORDER BY u.\"Id\" FOR UPDATE OF u", db, transaction))
await using (var reader = await query.ExecuteReaderAsync())
    while (await reader.ReadAsync()) accounts.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
var hasher = new PasswordHasher();
var credentials = new List<object>();
foreach (var account in accounts)
{
    if (!hasher.VerifyPassword("Password123!", account.Hash)) continue;
    var password = "Wf!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(18)) + "a9";
    var hash = hasher.HashPassword(password);
    await using var update = new NpgsqlCommand("UPDATE \"Users\" SET \"PasswordHash\"=@hash, \"TokenVersion\"=\"TokenVersion\"+1, \"UpdatedAt\"=now() WHERE \"Id\"=@id; INSERT INTO \"AuditLogs\" (\"UserId\",\"Action\",\"EntityType\",\"EntityId\",\"Timestamp\",\"Details\") VALUES (NULL,'HostedDemoCredentialsPrepared','User',@entity,now(),@details)", db, transaction);
    update.Parameters.AddWithValue("hash", hash);
    update.Parameters.AddWithValue("id", account.Id);
    update.Parameters.AddWithValue("entity", account.Id.ToString());
    update.Parameters.AddWithValue("details", "{\"reason\":\"Inherited demonstration credential replaced in the isolated Azure presentation copy\"}");
    await update.ExecuteNonQueryAsync();
    credentials.Add(new { account.Email, account.Role, Password = password });
}
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true }));
await transaction.CommitAsync();
Console.WriteLine($"Replaced {credentials.Count} inherited demonstration credentials in the isolated staging copy. Source database unchanged; private passwords were not printed.");
