# Coding Guidelines

## Code Comments

**Avoid comments in source code and tests.** Comments should only be added if absolutely necessary.

### Why Avoid Comments?

1. **Self-documenting code**: Well-named methods, variables, and classes should make the code's intent clear without
   comments
2. **Comments become outdated**: Code changes, but comments often don't, leading to misleading information
3. **Noise reduction**: Comments add visual clutter and make code harder to scan
4. **Better abstraction**: If code needs a comment to explain it, it likely needs refactoring

### When Comments Are Acceptable

- **XML documentation comments** (`///`) on public APIs, interfaces, and classes are valuable for IntelliSense
- **Suppression pragmas** (`#pragma warning disable`) with explanation when necessary
- **Complex algorithms** where the "why" is not obvious from the code itself (rare)
- **Legal notices** or licensing headers

### Placeholder Implementation

**Prefer throwing `NotImplementedException` over TODO comments.**

❌ **Don't do this:**
```csharp
private async Task HandleEditBooking(long bookingId)
{
    // TODO: Implement edit functionality
}
```

✅ **Do this instead:**
```csharp
private async Task HandleEditBooking(long bookingId)
{
    throw new NotImplementedException();
}
```

This makes incomplete features fail fast and clearly, rather than silently doing nothing. The compiler and runtime will help you track down unimplemented features.

### Alternative to Comments: Extract Methods

Instead of adding comments to explain what code does, extract that code into a well-named method.

❌ **Don't do this:**

```csharp
private async Task<Results<NoContent, NotFound>> UpdateBooking(...)
{
    // Load the tour that owns this booking
    var tour = await tourStore.GetByBookingId(id, ct);
    if (tour is null)
    {
        return TypedResults.NotFound();
    }

    // Update through the Tour aggregate
    tour.UpdateBooking(...);
    
    // Save changes
    await unitOfWork.SaveEntities(ct);
    
    return TypedResults.NoContent();
}
```

✅ **Do this instead:**

```csharp
private async Task<Results<NoContent, NotFound>> UpdateBooking(...)
{
    var tour = await tourStore.GetByBookingId(id, ct);
    if (tour is null)
    {
        return TypedResults.NotFound();
    }

    tour.UpdateBooking(...);
    await unitOfWork.SaveEntities(ct);

    return TypedResults.NoContent();
}
```

The method names, variable names, and code structure should be clear enough that comments are unnecessary.

### Test Comments

Tests should use `// Arrange`, `// Act`, `// Assert` comments to clearly separate the three sections of each test
method. This is one of the few acceptable uses of comments as it provides a standard, widely-recognized structure that
improves test readability.

## CQRS Pattern

This codebase follows the **CQRS (Command Query Responsibility Segregation)** pattern:

### Queries (Read Operations)

- Use **`IQueryService`** only
- Never use stores (ITourStore, ICustomerStore, etc.)
- Return DTOs optimized for presentation
- Endpoints: GET requests

### Commands (Write Operations)

- Use **Stores** (ITourStore, ICustomerStore, etc.) only
- Never use `IQueryService` except to retrieve DTOs for response bodies after persistence
- Work with full aggregate roots
- Endpoints: POST, PUT, DELETE, PATCH requests

### Benefits

- Optimized read and write models
- Better scalability
- Clear separation of concerns
- Easier to maintain and test

## Method Complexity

Prefer extracting code into well-named methods rather than using multiple levels of abstraction within a single method.

Each method should do **one thing** at an appropriate level of abstraction. If a method is doing too much or requires
comments to explain its sections, extract those sections into separate methods.
