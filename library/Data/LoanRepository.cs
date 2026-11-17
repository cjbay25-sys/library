
using library.Models;
using Npgsql;

namespace library.Data;

// Every SQL statement about loans lives in this one class.
public class LoanRepository
{
    public const int MaxOpenLoans = 3;

    private readonly NpgsqlDataSource _dataSource;

    public LoanRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // Copies the current row into a LoanView.
    // Positions 0 to 7 match every loan SELECT.
    private static LoanView ReadLoan(NpgsqlDataReader reader)
    {
        return new LoanView
        {
            LoanId = reader.GetInt64(0),
            MemberId = reader.GetInt64(1),
            MemberName = reader.GetString(2),
            BookTitle = reader.GetString(3),
            LoanedOn = reader.GetFieldValue<DateOnly>(4),
            ReturnedOn = reader.IsDBNull(5)
                ? null
                : reader.GetFieldValue<DateOnly>(5),
            DaysAllowed = reader.GetInt32(6),
            Status = reader.GetString(7)
        };
    }

    // READ: every loan with its member and book, newest first.
    public async Task<List<LoanView>> GetAllAsync()
    {
        const string sql =
            "SELECT l.loan_id, l.member_id, m.full_name, b.title, l.loaned_on, l.returned_on, l.days_allowed, " +
            "CASE WHEN l.returned_on IS NOT NULL THEN 'Returned' " +
            "WHEN l.loaned_on + l.days_allowed < CURRENT_DATE THEN 'Overdue' " +
            "ELSE 'Out' END " +
            "FROM lending.loan l " +
            "JOIN lending.member m ON l.member_id = m.member_id " +
            "JOIN lending.book b ON l.book_id = b.book_id " +
            "ORDER BY l.loaned_on DESC, l.loan_id DESC;";

        var loans = new List<LoanView>();

        await using var command = _dataSource.CreateCommand(sql);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            loans.Add(ReadLoan(reader));
        }

        return loans;
    }

    // BORROW: lend one book unless the member already has 3 books out.
    public async Task<bool> BorrowAsync(long memberId, long bookId, int daysAllowed)
    {
        // 1. Open one connection to the database.
        await using var connection = await _dataSource.OpenConnectionAsync();

        // 2. Start a transaction.
        await using var transaction = await connection.BeginTransactionAsync();

        // 3. Lock this member's row.
        const string lockSql =
            "SELECT member_id FROM lending.member " +
            "WHERE member_id = @memberId FOR UPDATE;";

        await using (var lockCommand = new NpgsqlCommand(lockSql, connection, transaction))
        {
            lockCommand.Parameters.AddWithValue("memberId", memberId);

            var member = await lockCommand.ExecuteScalarAsync();

            if (member == null)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        // 4. Count the books this member currently has out.
        const string countSql =
            "SELECT COUNT(*) FROM lending.loan " +
            "WHERE member_id = @memberId AND returned_on IS NULL;";

        long openLoans;

        await using (var countCommand = new NpgsqlCommand(countSql, connection, transaction))
        {
            countCommand.Parameters.AddWithValue("memberId", memberId);

            openLoans = Convert.ToInt64(await countCommand.ExecuteScalarAsync());
        }

        // 5. Check whether the member has reached the limit.
        if (openLoans >= MaxOpenLoans)
        {
            // 6. Undo the transaction.
            await transaction.RollbackAsync();

            // 7. Report that nothing was borrowed.
            return false;
        }

        // 8. Record the loan.
        const string insertSql =
            "INSERT INTO lending.loan " +
            "(member_id, book_id, days_allowed) " +
            "VALUES (@member_id, @book_id, @days_allowed);";

        await using (var insert = new NpgsqlCommand(insertSql, connection, transaction))
        {
            insert.Parameters.AddWithValue("member_id", memberId);
            insert.Parameters.AddWithValue("book_id", bookId);
            insert.Parameters.AddWithValue("days_allowed", daysAllowed);

            await insert.ExecuteNonQueryAsync();
        }

        // 9. Commit the transaction.
        await transaction.CommitAsync();

        // 10. Report that the book was borrowed.
        return true;
    }

    // RETURN: mark a loan as returned today.
    public async Task ReturnAsync(long loanId)
    {
        const string sql =
            "UPDATE lending.loan SET returned_on = CURRENT_DATE " +
            "WHERE loan_id = @id AND returned_on IS NULL;";

        await using var command = _dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue("id", loanId);

        await command.ExecuteNonQueryAsync();
    }
}
