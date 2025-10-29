# Coding Guidelines

## Domain-Driven Design Principles

### Use Domain Language in Methods

Domain objects should express business operations using **ubiquitous language** from the domain, not generic CRUD terms.

❌ **Don't do this:**
```csharp
booking.Update(totalPrice, notes, BookingStatus.Confirmed, paymentStatus);
```

✅ **Do this instead:**
```csharp
booking.Confirm();
```

### Why Domain Language Matters

1. **Intent is clear**: `booking.Confirm()` clearly expresses what business operation is happening
2. **Encapsulation**: Business rules for confirming a booking are encapsulated within the method
3. **Maintainability**: If confirmation rules change, there's one place to update
4. **Prevents invalid states**: Generic setters allow invalid state transitions; domain methods enforce invariants

### Behavior-Driven Endpoints

API endpoints should represent **business operations**, not generic CRUD actions.

❌ **Avoid overly generic endpoints:**
```csharp
PUT /bookings/{id}  // with status field in body
```

✅ **Prefer explicit domain operations:**
```csharp
PATCH /bookings/{id}/confirm
PATCH /bookings/{id}/cancel
PATCH /bookings/{id}/complete
```

### Benefits

- **Self-documenting API**: Endpoint names describe what they do
- **Type safety**: Each endpoint accepts only the data needed for that specific operation
- **Clear permissions**: Different operations can have different authorization rules
- **Easier testing**: Each operation is independently testable
- **Audit trails**: Business events are explicit in logs and monitoring

### Granular Domain Methods for Events

Even when endpoints group related updates (e.g., price and notes together), domain methods should remain **granular** to enable domain events.

❌ **Don't do this:**
```csharp
public void UpdateDetails(decimal price, string? notes)
{
    TotalPrice = price;
    Notes = notes;
}
```

✅ **Do this instead:**
```csharp
public void UpdatePrice(decimal newPrice)
{
    TotalPrice = newPrice;
    // Can raise PriceChangedEvent here
}

public void UpdateNotes(string? notes)
{
    Notes = notes;
    // Can raise NotesUpdatedEvent here
}
```

Then compose them in the aggregate root or application layer:
```csharp
public void UpdateBookingDetails(long bookingId, decimal newPrice, string? notes)
{
    var booking = GetBooking(bookingId);
    booking.UpdatePrice(newPrice);  // Raises PriceChangedEvent
    booking.UpdateNotes(notes);     // Raises NotesUpdatedEvent
}
```

**Benefits:**
- Each method can raise its own domain event
- Fine-grained auditing (know exactly what changed)
- Flexible composition for different use cases
- Better testability
- Supports eventual consistency patterns

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
