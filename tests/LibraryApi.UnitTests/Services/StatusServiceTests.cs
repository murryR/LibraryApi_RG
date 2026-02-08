using LibraryApi.Application.Common.Interfaces;
using LibraryApi.Application.Status.Services;
using LibraryApi.Domain.Entities;
using LibraryApi.Infrastructure.Persistence;
using LibraryApi.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApi.UnitTests.Services;

public class StatusServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly IStatusRepository _statusRepository;
    private readonly ILogger<StatusService> _logger;

    public StatusServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new AppDbContext(options);
        _statusRepository = new StatusRepository(_dbContext);
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<StatusService>.Instance;
    }

    [Fact]
    public async Task GetFirstStatusAsync_WhenStatusExists_ReturnsStatusEntity()
    {
        // Arrange
        _dbContext.Statuses.Add(new Status { Id = 1, Value = "OK" });
        _dbContext.Statuses.Add(new Status { Id = 2, Value = "Test" });
        await _dbContext.SaveChangesAsync();

        var service = new StatusService(_statusRepository, _logger);

        // Act
        var result = await service.GetFirstStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("OK", result.Value);
    }

    [Fact]
    public async Task GetFirstStatusAsync_WhenNoStatusExists_ReturnsStatusWithEmptyValue()
    {
        // Arrange
        var service = new StatusService(_statusRepository, _logger);

        // Act
        var result = await service.GetFirstStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Value);
    }
}
