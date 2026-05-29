using System.Globalization;
using Microsoft.Data.Sqlite;
using SapAnalytics.Application.Ports;
using SapAnalytics.Domain;

namespace SapAnalytics.Infrastructure.Outbound.Sqlite;

// Outbound adapter that persists sales in a SQLite database via hand-written SQL (no ORM).
// This is the only class that knows the storage is SQLite, the table shape, and that decimals
// are stored as invariant text (SQLite has no decimal type; text avoids floating-point drift)
// and dates as ISO `yyyy-MM-dd`.
//
// Translates SqliteException at its edge into Error.Unavailable, so nothing above the adapter
// sees a storage exception. OperationCanceledException propagates (cancellation is not a failure).
public sealed class SqliteSalesStore(string connectionString) : ISalesStore
{
    private const string DateFormat = "yyyy-MM-dd";

    public async Task<Result<int>> SaveAsync(IReadOnlyList<Sale> sales, CancellationToken ct = default)
    {
        try
        {
            await using var connection = await OpenAsync(ct);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);

            await ExecuteAsync(connection, transaction, "DELETE FROM sales;", ct);

            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO sales (sale_date, customer_id, product_name, quantity, amount)
                VALUES ($date, $customer, $product, $quantity, $amount);
                """;
            var date = insert.Parameters.Add("$date", SqliteType.Text);
            var customer = insert.Parameters.Add("$customer", SqliteType.Text);
            var product = insert.Parameters.Add("$product", SqliteType.Text);
            var quantity = insert.Parameters.Add("$quantity", SqliteType.Integer);
            var amount = insert.Parameters.Add("$amount", SqliteType.Text);

            foreach (var sale in sales)
            {
                date.Value = sale.Date.ToString(DateFormat, CultureInfo.InvariantCulture);
                customer.Value = sale.CustomerId;
                product.Value = sale.ProductName;
                quantity.Value = sale.Quantity;
                amount.Value = sale.Amount.ToString(CultureInfo.InvariantCulture);
                await insert.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);
            return Result<int>.Success(sales.Count);
        }
        catch (SqliteException ex)
        {
            return Result<int>.Failure(Error.Unavailable($"Could not write to the sales store: {ex.Message}"));
        }
    }

    public async Task<Result<IReadOnlyList<Sale>>> ReadAllAsync(CancellationToken ct = default)
    {
        try
        {
            await using var connection = await OpenAsync(ct);
            await using var query = connection.CreateCommand();
            query.CommandText =
                "SELECT sale_date, customer_id, product_name, quantity, amount FROM sales;";

            var sales = new List<Sale>();
            await using var reader = await query.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var date = DateOnly.ParseExact(reader.GetString(0), DateFormat, CultureInfo.InvariantCulture);
                var amount = decimal.Parse(reader.GetString(4), NumberStyles.Number, CultureInfo.InvariantCulture);
                sales.Add(new Sale(date, reader.GetString(1), reader.GetString(2), reader.GetInt32(3), amount));
            }

            return Result<IReadOnlyList<Sale>>.Success(sales);
        }
        catch (SqliteException ex)
        {
            return Result<IReadOnlyList<Sale>>.Failure(
                Error.Unavailable($"Could not read from the sales store: {ex.Message}"));
        }
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(ct);
        await ExecuteAsync(connection, transaction: null,
            """
            CREATE TABLE IF NOT EXISTS sales (
                sale_date    TEXT    NOT NULL,
                customer_id  TEXT    NOT NULL,
                product_name TEXT    NOT NULL,
                quantity     INTEGER NOT NULL,
                amount       TEXT    NOT NULL
            );
            """, ct);
        return connection;
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection, SqliteTransaction? transaction, string sql, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(ct);
    }
}
