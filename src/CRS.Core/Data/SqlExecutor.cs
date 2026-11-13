using System.Data;
using Microsoft.Data.SqlClient;
using CRS.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRS.Core.Data;

public sealed class DatabaseOptions
{
    public string CallRating { get; set; } = "";
    public string Users { get; set; } = "";
}
public sealed record SqlRequest(string Text, bool StoredProcedure, IReadOnlyDictionary<string, object?> Parameters, bool Users = false);
public interface ISqlExecutor
{
    Task<DataTable> QueryAsync(SqlRequest request, CancellationToken cancellationToken);
}
public sealed class SqlExecutor(IOptions<DatabaseOptions> options, ILogger<SqlExecutor> logger) : ISqlExecutor
{
    public async Task<DataTable> QueryAsync(SqlRequest request, CancellationToken ct)
    {
        var connectionString = request.Users ? options.Value.Users : options.Value.CallRating;
        if (string.IsNullOrWhiteSpace(connectionString)) throw new DependencyException("The database connection is not configured. Contact IT.");
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = request.Text;
            command.CommandType = request.StoredProcedure ? CommandType.StoredProcedure : CommandType.Text;
            command.CommandTimeout = 30;
            foreach (var (name, value) in request.Parameters)
                command.Parameters.Add(new SqlParameter(name, value ?? DBNull.Value));
            await using var reader = await command.ExecuteReaderAsync(ct);
            var table = new DataTable();
            // Materialize asynchronously so cancellation also applies during row retrieval.
            for (var i = 0; i < reader.FieldCount; i++) table.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
            while (await reader.ReadAsync(ct))
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                table.Rows.Add(values);
            }
            return table;
        }
        catch (SqlException ex)
        {
            logger.LogError("Database operation failed with SQL error {SqlError}", ex.Number);
            throw new DependencyException("The database operation failed. Please try again or contact IT.");
        }
    }
}
