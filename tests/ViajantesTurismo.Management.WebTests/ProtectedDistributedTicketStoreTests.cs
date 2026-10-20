namespace ViajantesTurismo.Management.WebTests;

/// <summary>
/// Verifies protected server-side authentication ticket storage.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ProtectedDistributedTicketStoreTests
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public async Task Expires_tickets_at_the_exact_controlled_boundary(int ticksAfterExpiry, bool expired)
    {
        // Arrange
        var expiry = new DateTimeOffset(2090, 2, 3, 14, 0, 0, TimeSpan.Zero);
        var clock = new ViajantesTurismo.Admin.Testing.Fakes.FakeTimeProvider(expiry.AddTicks(-1));
        var context = new ProtectedDistributedTicketStoreTestContext(timeProvider: clock);
        var ticket = ProtectedDistributedTicketStoreTestContext.CreateTicket(expiry);
        // Act
        var key = await context.Store.StoreAsync(ticket);
        clock.CurrentInstant = expiry.AddTicks(ticksAfterExpiry);
        var retrieved = await context.Store.RetrieveAsync(key);
        var cachedValue = await context.Cache.GetAsync(key, Xunit.TestContext.Current.CancellationToken);
        // Assert
        (retrieved is null).ShouldBe(expired);
        (cachedValue is null).ShouldBe(expired);
        clock.GetUtcNow().ShouldBe(expiry.AddTicks(ticksAfterExpiry));
    }

    [Fact]
    public async Task Uses_relative_session_lifetime_when_the_ticket_has_no_explicit_expiry()
    {
        // Arrange
        var inner = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        var cache = new ThrowingSetDistributedCache(inner, static (_, _) => false);
        var context = new ProtectedDistributedTicketStoreTestContext(cache);
        var ticket = ProtectedDistributedTicketStoreTestContext.CreateTicket();
        ticket.Properties.ExpiresUtc = null;
        // Act
        var key = await context.Store.StoreAsync(ticket);
        var retrieved = await context.Store.RetrieveAsync(key);
        // Assert
        retrieved.ShouldNotBeNull();
        var options = cache.LastOptions.ShouldNotBeNull();
        options.AbsoluteExpiration.ShouldBeNull();
        options.AbsoluteExpirationRelativeToNow.ShouldBe(Web.ManagementAuthenticationDefaults.SessionLifetime);
    }

    [Fact]
    public async Task Stores_encrypted_ticket_and_retrieves_it()
    {
        // Arrange
        var context = new ProtectedDistributedTicketStoreTestContext();
        var ticket = ProtectedDistributedTicketStoreTestContext.CreateTicket();

        // Act
        var key = await context.Store.StoreAsync(ticket);
        var storedValue = await context.Cache.GetAsync(key, Xunit.TestContext.Current.CancellationToken);
        var retrieved = await context.Store.RetrieveAsync(key);

        // Assert
        key.ShouldStartWith("management-ticket:", StringComparison.Ordinal);
        var protectedTicket = storedValue ?? throw new InvalidOperationException("The ticket was not stored.");
        var serializedTicket = Microsoft.AspNetCore.Authentication.TicketSerializer.Default.Serialize(ticket);
        protectedTicket.SequenceEqual(serializedTicket).ShouldBeFalse();
        retrieved.ShouldNotBeNull();
        retrieved.Principal.Identity?.Name.ShouldBe("Test Administrator");
    }

    [Fact]
    public async Task Returns_null_when_ticket_does_not_exist()
    {
        // Arrange
        var context = new ProtectedDistributedTicketStoreTestContext();

        // Act
        var retrieved = await context.Store.RetrieveAsync("management-ticket:missing");

        // Assert
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task Removes_corrupted_ticket_before_returning_null()
    {
        // Arrange
        var context = new ProtectedDistributedTicketStoreTestContext();
        const string key = "management-ticket:corrupted";
        await context.Cache.SetAsync(
            key,
            [1, 2, 3],
            new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions(),
            Xunit.TestContext.Current.CancellationToken);

        // Act
        var retrieved = await context.Store.RetrieveAsync(key);
        var cachedValue = await context.Cache.GetAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        retrieved.ShouldBeNull();
        cachedValue.ShouldBeNull();
    }

    [Fact]
    public async Task Removes_stored_ticket()
    {
        // Arrange
        var context = new ProtectedDistributedTicketStoreTestContext();
        var key = await context.Store.StoreAsync(ProtectedDistributedTicketStoreTestContext.CreateTicket());

        // Act
        await context.Store.RemoveAsync(key, Xunit.TestContext.Current.CancellationToken);
        var retrieved = await context.Store.RetrieveAsync(key);

        // Assert
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task Retries_ticket_removal_when_the_cache_recovers()
    {
        // Arrange
        var innerCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(
            innerCache,
            shouldThrowOnRemove: static (_, removeCall) => removeCall == 1);
        var context = new ProtectedDistributedTicketStoreTestContext(cache);
        var key = await context.Store.StoreAsync(ProtectedDistributedTicketStoreTestContext.CreateTicket());

        // Act
        await context.Store.RemoveAsync(key, Xunit.TestContext.Current.CancellationToken);
        var retrieved = await context.Store.RetrieveAsync(key);

        // Assert
        retrieved.ShouldBeNull();
        cache.RemoveCalls.ShouldBe(2);
    }

    [Fact]
    public async Task Renews_an_expired_ticket_when_cache_cleanup_fails()
    {
        // Arrange
        var innerCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(innerCache);
        var context = new ProtectedDistributedTicketStoreTestContext(cache);
        var expiredTicket = ProtectedDistributedTicketStoreTestContext.CreateTicket(DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act
        await context.Store.RenewAsync("management-ticket:expired", expiredTicket);

        // Assert
        cache.RemoveCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Does_not_log_the_ticket_key_when_terminal_removal_fails()
    {
        // Arrange
        const string key = "management-ticket:confidential-session-key";
        var innerCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(
            innerCache,
            new InvalidOperationException($"The cache could not remove {key}."));
        var logger = new CapturingLogger<Web.ProtectedDistributedTicketStore>();
        var context = new ProtectedDistributedTicketStoreTestContext(cache, logger);

        // Act
        Func<Task> remove = () => context.Store.RemoveAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        var exception = await remove.ShouldThrow<InvalidOperationException>();
        var logEntry = logger.Entries.ShouldHaveSingleItem();

        logEntry.ShouldContain("Management ticket cache removal failed after 2 attempts.", StringComparison.Ordinal);
        logEntry.ShouldNotContain(key, StringComparison.Ordinal);
        exception.Message.ShouldNotContain(key, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejects_a_protected_ticket_transplanted_from_another_cache_key()
    {
        // Arrange
        var context = new ProtectedDistributedTicketStoreTestContext();
        const string firstKey = "management-ticket:first";
        const string secondKey = "management-ticket:second";
        await context.Store.RenewAsync(firstKey, ProtectedDistributedTicketStoreTestContext.CreateTicket());
        var transplantedTicket = await context.Cache.GetAsync(firstKey, Xunit.TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The source ticket was not stored.");
        await context.Cache.SetAsync(
            secondKey,
            transplantedTicket,
            new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions(),
            Xunit.TestContext.Current.CancellationToken);

        // Act
        var ticket = await context.Store.RetrieveAsync(secondKey);
        var remainingTicket = await context.Cache.GetAsync(secondKey, Xunit.TestContext.Current.CancellationToken);

        // Assert
        ticket.ShouldBeNull();
        remainingTicket.ShouldBeNull();
    }

    [Fact]
    public async Task Treats_a_corrupted_ticket_as_missing_when_cache_cleanup_fails()
    {
        // Arrange
        var innerCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(innerCache);
        var context = new ProtectedDistributedTicketStoreTestContext(cache);
        var key = await context.Store.StoreAsync(ProtectedDistributedTicketStoreTestContext.CreateTicket());
        await context.Cache.SetAsync(
            key,
            [0x01],
            new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions(),
            Xunit.TestContext.Current.CancellationToken);

        // Act
        var ticket = await context.Store.RetrieveAsync(key);

        // Assert
        ticket.ShouldBeNull();
        cache.RemoveCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Propagates_cooperative_cancellation_when_removing_a_ticket()
    {
        // Arrange
        const string key = "management-ticket:confidential-session-key";
        var innerCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var cache = new ThrowingRemoveDistributedCache(
            innerCache,
            new OperationCanceledException($"The cache could not remove {key}.", cancellationSource.Token));
        var context = new ProtectedDistributedTicketStoreTestContext(cache);

        // Act
        Func<Task> remove = () => context.Store.RemoveAsync(key, cancellationSource.Token);

        // Assert
        var exception = await remove.ShouldThrow<OperationCanceledException>();
        exception.Message.ShouldNotContain(key, StringComparison.Ordinal);
        exception.CancellationToken.ShouldBe(cancellationSource.Token);
        cache.LastRemoveCancellationToken.ShouldBe(cancellationSource.Token);
    }

    [Fact]
    public async Task Does_not_retrieve_an_expired_ticket()
    {
        // Arrange
        var context = new ProtectedDistributedTicketStoreTestContext();
        var expiredTicket = ProtectedDistributedTicketStoreTestContext.CreateTicket(DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act
        var key = await context.Store.StoreAsync(expiredTicket);
        var retrieved = await context.Store.RetrieveAsync(key);
        var cachedValue = await context.Cache.GetAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        retrieved.ShouldBeNull();
        cachedValue.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Rejects_blank_ticket_keys(string key)
    {
        // Arrange
        var context = new ProtectedDistributedTicketStoreTestContext();
        var ticket = ProtectedDistributedTicketStoreTestContext.CreateTicket();

        // Act
        Func<Task> retrieve = () => context.Store.RetrieveAsync(key);
        Func<Task> renew = () => context.Store.RenewAsync(key, ticket);
        Func<Task> remove = () => context.Store.RemoveAsync(key);

        // Assert
        await retrieve.ShouldThrow<ArgumentException>();
        await renew.ShouldThrow<ArgumentException>();
        await remove.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public async Task Rejects_a_null_ticket()
    {
        // Arrange
        var context = new ProtectedDistributedTicketStoreTestContext();

        // Act
        Func<Task> store = () => context.Store.StoreAsync(null!);

        // Assert
        await store.ShouldThrow<ArgumentNullException>();
    }
}
