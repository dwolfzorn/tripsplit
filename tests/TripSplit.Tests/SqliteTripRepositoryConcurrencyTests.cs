using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TripSplit.Server.Data;
using TripSplit.Server.Repositories;
using TripSplit.Shared.Models;

namespace TripSplit.Tests;

public class SqliteTripRepositoryConcurrencyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TripDbContext _db;
    private readonly SqliteTripRepository _repository;

    public SqliteTripRepositoryConcurrencyTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TripDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TripDbContext(options);
        _db.Database.EnsureCreated();
        _repository = new SqliteTripRepository(_db);
    }

    [Fact]
    public async Task UpdateAsync_WithMatchingRowVersion_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var tripId = await _repository.CreateAsync(new TripDto(), ownerId);

        var (result, envelope) = await _repository.UpdateAsync(tripId, new TripDto(), expectedRowVersion: 0);

        Assert.Equal(UpdateResult.Success, result);
        Assert.Equal(1, envelope!.RowVersion);
    }

    [Fact]
    public async Task UpdateAsync_WithStaleRowVersion_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var tripId = await _repository.CreateAsync(new TripDto(), ownerId);

        // First update succeeds and moves the row to version 1.
        await _repository.UpdateAsync(tripId, new TripDto(), expectedRowVersion: 0);

        // A second editor still holding version 0 tries to save - must conflict,
        // not silently overwrite the first editor's change.
        var (result, current) = await _repository.UpdateAsync(tripId, new TripDto(), expectedRowVersion: 0);

        Assert.Equal(UpdateResult.Conflict, result);
        Assert.Equal(1, current!.RowVersion);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
