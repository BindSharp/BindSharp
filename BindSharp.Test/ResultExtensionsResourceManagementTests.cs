using BindSharp;

namespace BindSharp.Test;

/// <summary>
/// Tests for ResultExtensions Using and UsingAsync methods (resource management)
/// </summary>
public class ResultExtensionsResourceManagementTests
{
    #region Using Tests - Success Cases

    [Fact]
    public void Using_WithSuccessfulOperation_DisposesResource()
    {
        // Arrange
        var resource = new DisposableResource();
        var result = Result<DisposableResource, string>.Success(resource);

        // Act
        var output = result.Using(r => 
        {
            Assert.False(r.IsDisposed);
            return Result<string, string>.Success($"Used {r.Value}");
        });

        // Assert
        Assert.True(resource.IsDisposed);
        Assert.True(output.IsSuccess);
        Assert.Equal("Used 42", output.Value);
    }

    [Fact]
    public void Using_WithSuccessfulOperation_ReturnsCorrectResult()
    {
        // Arrange
        var resource = new DisposableResource();
        var result = Result<DisposableResource, string>.Success(resource);

        // Act
        var output = result.Using(r => 
            Result<int, string>.Success(r.Value * 2)
        );

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(84, output.Value);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public void Using_WithChainedOperations_DisposesResource()
    {
        // Arrange
        var resource = new DisposableResource();
        var result = Result<DisposableResource, string>.Success(resource);

        // Act
        var output = result.Using(r => 
            Result<int, string>.Success(r.Value)
                .Map(x => x * 2)
                .Bind(x => x > 0 
                    ? Result<int, string>.Success(x) 
                    : Result<int, string>.Failure("Must be positive"))
        );

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(84, output.Value);
        Assert.True(resource.IsDisposed);
    }

    #endregion

    #region Using Tests - Failure Cases

    [Fact]
    public void Using_WhenResourceAcquisitionFails_DoesNotCallOperation()
    {
        // Arrange
        var result = Result<DisposableResource, string>.Failure("Resource not available");
        var operationCalled = false;

        // Act
        var output = result.Using(r => 
        {
            operationCalled = true;
            return Result<int, string>.Success(r.Value);
        });

        // Assert
        Assert.False(operationCalled);
        Assert.True(output.IsFailure);
        Assert.Equal("Resource not available", output.Error);
    }

    [Fact]
    public void Using_WhenOperationFails_StillDisposesResource()
    {
        // Arrange
        var resource = new DisposableResource();
        var result = Result<DisposableResource, string>.Success(resource);

        // Act
        var output = result.Using(r => 
            Result<int, string>.Failure("Operation failed")
        );

        // Assert
        Assert.True(resource.IsDisposed);
        Assert.True(output.IsFailure);
        Assert.Equal("Operation failed", output.Error);
    }

    [Fact]
    public void Using_WhenOperationThrows_StillDisposesResource()
    {
        // Arrange
        var resource = new DisposableResource();
        var result = Result<DisposableResource, string>.Success(resource);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            result.Using(r => 
            {
                throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162 // Unreachable code detected
                return Result<int, string>.Success(42);
#pragma warning restore CS0162
            });
        });

        Assert.True(resource.IsDisposed);
    }

    #endregion

    #region Using Tests - Multiple Resources

    [Fact]
    public void Using_NestedResources_DisposesAll()
    {
        // Arrange
        var resource1 = new DisposableResource { Value = 1 };
        var resource2 = new DisposableResource { Value = 2 };
        var result1 = Result<DisposableResource, string>.Success(resource1);
        var result2 = Result<DisposableResource, string>.Success(resource2);

        // Act
        var output = result1.Using(r1 =>
            result2.Using(r2 =>
                Result<int, string>.Success(r1.Value + r2.Value)
            )
        );

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(3, output.Value);
        Assert.True(resource1.IsDisposed);
        Assert.True(resource2.IsDisposed);
    }

    #endregion

    #region UsingAsync Tests - Success Cases

    [Fact]
    public async Task UsingAsync_WithSuccessfulOperation_DisposesResource()
    {
        // Arrange
        var resource = new DisposableResource();
        var result = Result<DisposableResource, string>.Success(resource);

        // Act
        var output = await result.UsingAsync(async r => 
        {
            await Task.Delay(1);
            Assert.False(r.IsDisposed);
            return Result<string, string>.Success($"Used {r.Value}");
        });

        // Assert
        Assert.True(resource.IsDisposed);
        Assert.True(output.IsSuccess);
        Assert.Equal("Used 42", output.Value);
    }

    [Fact]
    public async Task UsingAsync_WithAsyncOperation_ReturnsCorrectResult()
    {
        // Arrange
        var resource = new DisposableResource();
        var result = Result<DisposableResource, string>.Success(resource);

        // Act
        var output = await result.UsingAsync(async r => 
        {
            await Task.Delay(1);
            return Result<int, string>.Success(r.Value * 2);
        });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(84, output.Value);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task UsingAsync_WithChainedAsyncOperations_DisposesResource()
    {
        // Arrange
        var resource = new DisposableResource();
        var result = Result<DisposableResource, string>.Success(resource);

        // Act
        var output = await result.UsingAsync(async r => 
        {
            var initial = Result<int, string>.Success(r.Value);
            return await initial
                .MapAsync(async x => await MultiplyAsync(x, 2))
                .BindAsync(async x => x > 0 
                    ? Result<int, string>.Success(x) 
                    : Result<int, string>.Failure("Must be positive"));
        });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(84, output.Value);
        Assert.True(resource.IsDisposed);
    }

    #endregion

    #region UsingAsync Tests - Failure Cases

    [Fact]
    public async Task UsingAsync_WhenResourceAcquisitionFails_DoesNotCallOperation()
    {
        // Arrange
        var result = Result<DisposableResource, string>.Failure("Resource not available");
        var operationCalled = false;

        // Act
        var output = await result.UsingAsync(async r => 
        {
            await Task.Delay(1);
            operationCalled = true;
            return Result<int, string>.Success(r.Value);
        });

        // Assert
        Assert.False(operationCalled);
        Assert.True(output.IsFailure);
        Assert.Equal("Resource not available", output.Error);
    }

    [Fact]
    public async Task UsingAsync_WhenOperationFails_StillDisposesResource()
    {
        // Arrange
        var resource = new DisposableResource();
        var result = Result<DisposableResource, string>.Success(resource);

        // Act
        var output = await result.UsingAsync(async r => 
        {
            await Task.Delay(1);
            return Result<int, string>.Failure("Operation failed");
        });

        // Assert
        Assert.True(resource.IsDisposed);
        Assert.True(output.IsFailure);
        Assert.Equal("Operation failed", output.Error);
    }

    [Fact]
    public async Task UsingAsync_WhenOperationThrows_StillDisposesResource()
    {
        // Arrange
        var resource = new DisposableResource();
        var result = Result<DisposableResource, string>.Success(resource);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await result.UsingAsync(async r => 
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162 // Unreachable code detected
                return Result<int, string>.Success(42);
#pragma warning restore CS0162
            });
        });

        Assert.True(resource.IsDisposed);
    }

    #endregion

    #region Real-World Scenarios - File Operations

    [Fact]
    public void Using_WithFileStream_DisposesCorrectly()
    {
        // Arrange
        var stream = new FakeStream();
        var result = Result<FakeStream, string>.Success(stream);

        // Act
        var output = result.Using(s => 
        {
            var data = s.ReadData();
            return Result<string, string>.Success(data);
        });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal("File content", output.Value);
        Assert.True(stream.IsDisposed);
    }

    [Fact]
    public async Task UsingAsync_WithFileStream_DisposesCorrectly()
    {
        // Arrange
        var stream = new FakeStream();
        var result = Result<FakeStream, string>.Success(stream);

        // Act
        var output = await result.UsingAsync(async s => 
        {
            var data = await s.ReadDataAsync();
            return Result<string, string>.Success(data);
        });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal("File content", output.Value);
        Assert.True(stream.IsDisposed);
    }

    #endregion

    #region Real-World Scenarios - Database Connections

    [Fact]
    public void Using_WithDatabaseConnection_DisposesCorrectly()
    {
        // Arrange
        var connection = new FakeDatabaseConnection();
        var result = Result<FakeDatabaseConnection, string>.Success(connection);

        // Act
        var output = result.Using(conn => 
        {
            var data = conn.ExecuteQuery("SELECT * FROM Users");
            return Result<string, string>.Success(data);
        });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal("Query result", output.Value);
        Assert.True(connection.IsDisposed);
        Assert.False(connection.IsConnected);
    }

    [Fact]
    public async Task UsingAsync_WithDatabaseTransaction_DisposesCorrectly()
    {
        // Arrange
        var transaction = new FakeDatabaseTransaction();
        var result = Result<FakeDatabaseTransaction, string>.Success(transaction);

        // Act
        var output = await result.UsingAsync(async tx => 
        {
            await tx.ExecuteAsync("INSERT INTO Users VALUES (1, 'John')");
            await tx.CommitAsync();
            return Result<bool, string>.Success(true);
        });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.True(transaction.IsDisposed);
        Assert.True(transaction.IsCommitted);
    }

    [Fact]
    public async Task UsingAsync_DatabaseTransaction_RollbackOnFailure()
    {
        // Arrange
        var transaction = new FakeDatabaseTransaction();
        var result = Result<FakeDatabaseTransaction, string>.Success(transaction);

        // Act
        var output = await result.UsingAsync(async tx => 
        {
            await tx.ExecuteAsync("INSERT INTO Users VALUES (1, 'John')");
            // Operation fails
            await tx.RollbackAsync();
            return Result<bool, string>.Failure("Transaction failed");
        });

        // Assert
        Assert.True(output.IsFailure);
        Assert.True(transaction.IsDisposed);
        Assert.True(transaction.IsRolledBack);
    }

    #endregion

    #region Real-World Scenarios - HTTP Clients

    [Fact]
    public async Task UsingAsync_WithHttpClient_DisposesCorrectly()
    {
        // Arrange
        var httpClient = new FakeHttpClient();
        var result = Result<FakeHttpClient, string>.Success(httpClient);

        // Act
        var output = await result.UsingAsync(async client => 
        {
            var response = await client.GetAsync("https://api.example.com/users");
            return Result<string, string>.Success(response);
        });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal("HTTP response", output.Value);
        Assert.True(httpClient.IsDisposed);
    }

    #endregion

    #region Complete Integration Test

    [Fact]
    public async Task UsingAsync_CompleteWorkflow_WorksCorrectly()
    {
        // Arrange
        var connection = new FakeDatabaseConnection();

        // Act
        var result = await (await OpenConnectionAsync())
            .UsingAsync(async conn => 
                await ResultExtensions.TryAsync(
                    async () => await conn.ExecuteQueryAsync("SELECT * FROM Users WHERE Id = 1"),
                    ex => $"Query failed: {ex.Message}"
                )
                .EnsureAsync(data => !string.IsNullOrEmpty(data), "No data returned")
                .TapAsync(async data => await LogQueryAsync(data))
                .MapAsync(data => $"Processed: {data}")
            );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Processed", result.Value);
    }

    #endregion

    #region Test Helpers

    private class DisposableResource : IDisposable
    {
        public int Value { get; init; } = 42;
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private class FakeStream : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public string ReadData() => "File content";

        public Task<string> ReadDataAsync() => Task.FromResult("File content");

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private class FakeDatabaseConnection : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public bool IsConnected { get; private set; } = true;

        public string ExecuteQuery(string query) => "Query result";

        public Task<string> ExecuteQueryAsync(string query) => Task.FromResult("Query result");

        public void Dispose()
        {
            IsDisposed = true;
            IsConnected = false;
        }
    }

    private class FakeDatabaseTransaction : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public bool IsCommitted { get; private set; }
        public bool IsRolledBack { get; private set; }

        public Task ExecuteAsync(string command) => Task.CompletedTask;

        public Task CommitAsync()
        {
            IsCommitted = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync()
        {
            IsRolledBack = true;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private class FakeHttpClient : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public Task<string> GetAsync(string url) => Task.FromResult("HTTP response");

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private static Task<int> MultiplyAsync(int value, int multiplier) =>
        Task.FromResult(value * multiplier);

    private static Task<Result<FakeDatabaseConnection, string>> OpenConnectionAsync() =>
        Task.FromResult(Result<FakeDatabaseConnection, string>.Success(new FakeDatabaseConnection()));

    private static Task LogQueryAsync(string data) => Task.CompletedTask;

    #endregion
}