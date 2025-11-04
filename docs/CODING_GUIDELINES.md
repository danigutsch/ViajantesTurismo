# Coding Guidelines

## Domain-Driven Design Principles

### Use Domain Language in Methods

Domain objects should express business operations using **ubiquitous language** from the domain, not generic CRUD terms.

❌ **Don't do this:**
```csharp
booking.Status = BookingStatus.Confirmed;  // Direct property mutation
```

✅ **Do this instead:**
```csharp
booking.Confirm();  // Intention-revealing method
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
PATCH /bookings/{id}/notes
```

### Benefits

- **Self-documenting API**: Endpoint names describe what they do
- **Type safety**: Each endpoint accepts only the data needed for that specific operation
- **Clear permissions**: Different operations can have different authorization rules
- **Easier testing**: Each operation is independently testable
- **Audit trails**: Business events are explicit in logs and monitoring

### Granular Domain Methods for Events

Domain methods should be **granular** and focused on specific business operations to enable domain events and maintain
single responsibility.

❌ **Don't do this:**
```csharp
public void UpdateDetails(BookingStatus status, PaymentStatus payment, string? notes)
{
    Status = status;
    PaymentStatus = payment;
    Notes = notes;
}
```

✅ **Do this instead:**
```csharp
public Result Confirm()
{
    // Validation logic for state transition
    Status = BookingStatus.Confirmed;
    // Can raise BookingConfirmedEvent here
}

public void UpdatePaymentStatus(PaymentStatus status)
{
    PaymentStatus = status;
    // Can raise PaymentStatusChangedEvent here
}

public Result UpdateNotes(string? notes)
{
    // Validation logic
    Notes = notes;
    // Can raise NotesUpdatedEvent here
}
```

Then compose them in the aggregate root:
```csharp
public Result UpdateBookingNotes(long bookingId, string? notes)
{
    var booking = GetBooking(bookingId);
    return booking.UpdateNotes(notes);  // Raises NotesUpdatedEvent
}
```

**Benefits:**
- Each method can raise its own domain event
- Fine-grained auditing (know exactly what changed)
- Flexible composition for different use cases
- Better testability
- Supports eventual consistency patterns

## Strive for Rich Domain Models

A **rich domain model** encapsulates business logic within domain entities using intention-revealing methods. Avoid anemic domain models where entities are mere data containers.

### Granular, Intention-Revealing Methods

Domain methods should be **granular** and clearly express business intent. Each method should update a specific aspect of the entity.

✅ **Good - Granular methods:**
```csharp
tour.UpdateBasicInfo("CUBA2025", "Cuba Cycling Adventure");
tour.UpdateSchedule(startDate, endDate);
tour.UpdatePricing(2500m, 300m, 100m, 200m, Currency.EUR);
tour.UpdateIncludedServices(new[] { "Hotel", "Breakfast" });
```

❌ **Avoid - One coarse Update method:**
```csharp
// Don't use a single method that updates everything
tour.Update(identifier, name, startDate, endDate, price, ...);
```

### Why Granular Methods?

1. **Single Responsibility**: Each method has one focused purpose
2. **Domain Events**: Each method can raise specific domain events (e.g., `PriceChanged`, `ScheduleUpdated`)
3. **Validation**: Focused validation per business rule (e.g., `UpdateSchedule` validates end date > start date)
4. **Flexibility**: Compose methods for different use cases
5. **Clarity**: Code reads like business requirements

### State Transitions as Methods

Model entity lifecycle transitions as explicit methods, not property setters.

✅ **Good - Explicit state transitions:**
```csharp
booking.Confirm();     // Pending → Confirmed
booking.Cancel();      // Any → Cancelled  
booking.Complete();    // Confirmed → Completed
```

❌ **Avoid - Direct property mutation:**
```csharp
booking.Status = BookingStatus.Confirmed;  // Bypasses validation
```

State transition methods enforce business rules:
```csharp
internal void Confirm()
{
    if (Status == BookingStatus.Cancelled)
    {
        throw new InvalidOperationException("Cannot confirm a cancelled booking.");
    }
    Status = BookingStatus.Confirmed;
}
```

### Practical Examples

**Tour Updates:**
```csharp
// Scenario: Adjust pricing due to market conditions
var tour = await tourStore.GetById(tourId, ct);
tour.UpdateBasePrice(newPrice);

// Scenario: Reschedule due to venue change  
tour.UpdateSchedule(newStartDate, newEndDate);

// Scenario: Currency conversion
var rate = await currencyService.GetRate(Currency.EUR, Currency.USD);
tour.UpdatePricing(
    tour.Price * rate,
    tour.DoubleRoomSupplementPrice * rate,
    tour.RegularBikePrice * rate,
    tour.EBikePrice * rate,
    Currency.USD
);
```

**Customer Updates:**
```csharp
// Scenario: Customer changes phone number
var customer = await customerStore.GetById(customerId, ct);
customer.UpdateContactInfo(new ContactInfo(newPhone, email, instagram, facebook));

// Scenario: Customer moves
customer.UpdateAddress(new Address(street, city, state, postalCode, country));
```

**Booking Lifecycle:**
```csharp
// 1. Create booking (pending)
var result = tour.AddBooking(customerId, BikeType.Regular, companionId, BikeType.EBike, RoomType.DoubleRoom, "Early bird");
var booking = result.Value;

// 2. Customer confirms and pays deposit
tour.ConfirmBooking(booking.Id);
tour.UpdateBookingPaymentStatus(booking.Id, PaymentStatus.PartiallyPaid);

// 3. Customer pays balance
tour.UpdateBookingPaymentStatus(booking.Id, PaymentStatus.Paid);

// 4. Tour completes
tour.CompleteBooking(booking.Id);
```

### API Layer Considerations

The API layer may use **coarse-grained DTOs** for client convenience (fewer HTTP requests, simpler forms), but should delegate to **granular domain methods** internally:

```csharp
// API accepts UpdateTourDto with all fields
private static async Task<Results<NoContent, NotFound>> UpdateTour(
    int id,
    UpdateTourDto dto,
    ITourStore tourStore,
    IUnitOfWork unitOfWork,
    CancellationToken ct)
{
    var tour = await tourStore.GetById(id, ct);
    if (tour is null) return TypedResults.NotFound();

    // Delegate to granular domain methods
    tour.UpdateBasicInfo(dto.Identifier, dto.Name);
    tour.UpdateSchedule(dto.StartDate, dto.EndDate);
    tour.UpdatePricing(dto.Price, dto.DoubleRoomSupplementPrice, 
        dto.RegularBikePrice, dto.EBikePrice, (Currency)dto.Currency);
    tour.UpdateIncludedServices([.. dto.IncludedServices]);

    await unitOfWork.SaveEntities(ct);
    return TypedResults.NoContent();
}
```

This provides:
- **Domain expressiveness**: Clear business operations in domain layer
- **Client convenience**: Simple DTOs in API layer
- **Best of both worlds**: Rich domain model + practical API

### Benefits Summary

- **Ubiquitous Language**: Code uses business terms
- **Encapsulation**: Business rules live in domain entities
- **Testability**: Each method is independently testable
- **Maintainability**: Changes to business rules are localized
- **Domain Events**: Fine-grained events for each business operation
- **Type Safety**: Invalid operations prevented at compile time

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

### Test Method Naming Convention

**Use underscores to separate all words in test method names for improved readability.**

✅ **Do this:**

```csharp
public void Map_To_Currency_Dto_Should_Map_All_Valid_Values()
public void Invalid_Amount_With_Negative_Should_Return_Invalid_Result()
public void Sanitize_Collection_Removes_Duplicate_Entries_Case_Insensitive()
```

❌ **Don't do this:**

```csharp
public void MapToCurrencyDto_ShouldMapAllValidValues()  // Hard to read
public void InvalidAmount_WithNegative_ShouldReturnInvalidResult()  // Inconsistent
public void SanitizeCollection_RemovesDuplicateEntries_CaseInsensitive()  // Mixed style
```

**Pattern:** `Method_Name_Context_Description_Expected_Behavior`

- Insert underscores between all word boundaries (camelCase → Snake_Case)
- Makes test names easier to read in test explorers and failure reports
- Provides visual separation between method name, context, and expected behavior
- Works well with long, descriptive test names

**Behavior Tests Exception:**
Behavior test step definitions should follow Gherkin/SpecFlow conventions or use the analyzer/code fix to name them
correctly. They use regex patterns like:

```csharp
[Given(@"I have valid tour details")]
[When(@"I update the tour price to (.*)")]
[Then(@"the booking total should be (.*)")]
```

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

## Use Mappers Instead of Casts

**Always use mapper methods to convert between domain enums and DTOs instead of direct casting.**

❌ **Don't do this:**

```csharp
Currency = (CurrencyDto)tour.Currency,
Status = (BookingStatusDto)booking.Status,
BikeType = (BikeTypeDto)customer.PhysicalInfo.BikeType
```

✅ **Do this instead:**

```csharp
Currency = TourMapper.MapToCurrencyDto(tour.Currency),
Status = BookingMapper.MapToBookingStatusDto(booking.Status),
BikeType = BookingMapper.MapToBikeTypeDto(customer.PhysicalInfo.BikeType)
```

**Why use mappers?**

1. **Type safety**: Mappers catch enum mismatches at compile time if enum values don't align
2. **Explicit intent**: Code clearly shows a conversion is happening
3. **Maintainability**: When enum values change, you only update the mapper
4. **Consistency**: All conversions go through the same path
5. **Testability**: Mappers can be unit tested independently

**Pattern:**

- Create static mapper classes in `Application/Mappings/` folder
- Name methods `MapTo{TargetType}` for clarity
- Always throw `ArgumentOutOfRangeException` for unknown enum values
- Provide both directions (domain ↔ DTO) when needed

**Exception:** Direct casts are acceptable in test code where the simplicity outweighs the benefits of mappers.

## Method Complexity

Prefer extracting code into well-named methods rather than using multiple levels of abstraction within a single method.

Each method should do **one thing** at an appropriate level of abstraction. If a method is doing too much or requires
comments to explain its sections, extract those sections into separate methods.

### Enum Guidelines

**Always assign 0 to Unknown, None, or Other values in enums.**

This ensures that:

1. Default values are meaningful (uninitialized enums default to 0)
2. Database storage is consistent
3. Validation logic is simplified (valid values are non-zero)

✅ **Do this:**

```csharp
public enum PaymentMethod
{
    Other = 0,        // Unknown/fallback value is 0
    CreditCard = 1,
    BankTransfer = 2,
    Cash = 3
}

public enum BookingStatus
{
    None = 0,         // Default/uninitialized state
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3
}
```

❌ **Don't do this:**

```csharp
public enum PaymentMethod
{
    CreditCard = 0,   // Don't start with specific values
    BankTransfer = 1,
    Cash = 2,
    Other = 3         // Other/Unknown should be 0
}
```

### TimeProvider for Testable Time-Dependent Code

**Use `TimeProvider` instead of `DateTime.UtcNow` or `DateTimeOffset.UtcNow` in domain logic.**

This makes code testable by allowing time to be controlled in tests.

✅ **Do this:**

```csharp
public static Result<Payment> Create(
    long bookingId,
    decimal amount,
    DateTime paymentDate,
    PaymentMethod method,
    TimeProvider timeProvider,
    string? referenceNumber = null,
    string? notes = null)
{
    ArgumentNullException.ThrowIfNull(timeProvider);

    var now = timeProvider.GetUtcNow().UtcDateTime;
    if (paymentDate > now)
    {
        return PaymentErrors.FuturePaymentDate(paymentDate);
    }

    return new Payment(bookingId, amount, paymentDate, method, referenceNumber, notes, now);
}
```

❌ **Don't do this:**

```csharp
public static Result<Payment> Create(
    long bookingId,
    decimal amount,
    DateTime paymentDate,
    PaymentMethod method,
    string? referenceNumber = null,
    string? notes = null)
{
    // Hard to test - time is fixed to system clock
    if (paymentDate > DateTime.UtcNow)
    {
        return PaymentErrors.FuturePaymentDate(paymentDate);
    }

    return new Payment(bookingId, amount, paymentDate, method, referenceNumber, notes, DateTime.UtcNow);
}
```

**Benefits:**

- **Testability**: Tests can control time using `FakeTimeProvider`
- **Deterministic**: Same inputs always produce same outputs in tests
- **Time Travel**: Can test future/past scenarios easily
- **Production**: Use `TimeProvider.System` for real applications

**Pattern:**

- Factory methods take `TimeProvider` as parameter
- Get time once at start: `var now = timeProvider.GetUtcNow().UtcDateTime;`
- Pass time to constructor for property assignment
- Tests use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` package

### Do Not Use Regions

**Avoid using `#region` directives in code.**

Regions hide code structure and make navigation harder. If code needs regions to be "organized," it's usually a sign
that:

1. The class is doing too much (violates Single Responsibility Principle)
2. Methods should be extracted to separate classes
3. Code needs better logical grouping through proper class design

❌ **Don't do this:**

```csharp
public class OrderService
{
    #region Validation
    private void ValidateOrder() { }
    private void ValidateCustomer() { }
    #endregion

    #region Processing
    private void ProcessPayment() { }
    private void SendConfirmation() { }
    #endregion
}
```

✅ **Do this instead:**

```csharp
public class OrderService
{
    private readonly OrderValidator _validator;
    private readonly OrderProcessor _processor;
    
    public OrderService(OrderValidator validator, OrderProcessor processor)
    {
        _validator = validator;
        _processor = processor;
    }
}

public class OrderValidator
{
    public void ValidateOrder() { }
    public void ValidateCustomer() { }
}

public class OrderProcessor  
{
    public void ProcessPayment() { }
    public void SendConfirmation() { }
}
```

**Benefits of avoiding regions:**

- Forces better class design and separation of concerns
- Code structure is visible without expanding/collapsing
- Easier to navigate with IDE features (Go to Definition, etc.)
- Simpler code reviews
- Encourages proper refactoring

### Collection types by intent
Choose collection types to clearly express intent (mutability, uniqueness, ordering, and expected operations). Prefer exposing the least-powerful interface that conveys how the collection should be used.

**Prefer arrays for immutable collections and simple iteration scenarios where the collection won't be modified after
initialization.**

Guidelines:

- Arrays for simple immutable collections:
    - Use arrays (`T[]`) when the collection size is known at initialization and won't change
    - Arrays are more efficient for iteration and have lower memory overhead
    - Expose as arrays or `IReadOnlyList<T>` depending on whether callers need to know it's an array
    - Example:
  ```csharp
  public IReadOnlyList<string> IncludedServices { get; private set; } = [];
  
  public void UpdateIncludedServices(string[] services)
  {
      IncludedServices = services;
  }
  ```

- Mutability intent (aggregates / domain models):
  - Internally use mutable collections when you need to add/remove items frequently (e.g. `List<T>`).
  - Expose a read-only view from public APIs and entity properties (e.g. `IReadOnlyCollection<T>` or `IReadOnlyList<T>`).
  - Example:
  ```csharp
  private readonly List<Passenger> _passengers = new();
  public IReadOnlyList<Passenger> Passengers => _passengers;

  public void AddPassenger(Passenger p)
  {
      if (_passengers.Any(x => x.Id == p.Id)) throw new InvalidOperationException("Duplicate passenger");
      _passengers.Add(p);
  }
  ```

- Uniqueness / membership checks:
  - Use `HashSet<T>` when you require uniqueness or fast membership checks. Expose it as `IReadOnlyCollection<T>` or `IEnumerable<T>`.
  ```csharp
  private readonly HashSet<string> _emails = new(StringComparer.OrdinalIgnoreCase);
  public IReadOnlyCollection<string> Emails => _emails;
  ```

- Lookups by key:
  - Use `Dictionary<TKey, TValue>` for keyed lookups; keep it private and provide explicit accessor methods.

- Read-only snapshots and concurrency:
  - For immutable semantics or when sharing between threads, prefer the `System.Collections.Immutable` types (e.g. `ImmutableList<T>`).

- Query projections / API responses:
    - For read/query surfaces prefer returning arrays or `IEnumerable<T>`. If consumers need count or index access,
      prefer arrays or `IReadOnlyCollection<T>`/`IReadOnlyList<T>`.

Practical decision matrix (short):

- Collection size known and won't change: use arrays.
- You will add/remove items in the aggregate: use `List<T>` internally + expose `IReadOnlyCollection<T>`.
- You need uniqueness: `HashSet<T>` (internal) + expose read-only.
- You need keyed lookup: `Dictionary<TKey, TValue>` (internal).

## Asynchronous Programming

### Do Not Use Default Values for CancellationToken

**Never provide default values for `CancellationToken` parameters in public APIs.**

❌ **Don't do this:**

```csharp
public async Task<GetTourDto?> GetTourById(int id, CancellationToken cancellationToken = default)
{
    return await httpClient.GetFromJsonAsync<GetTourDto>($"/tours/{id}", cancellationToken);
}
```

✅ **Do this instead:**

```csharp
public async Task<GetTourDto?> GetTourById(int id, CancellationToken cancellationToken)
{
    return await httpClient.GetFromJsonAsync<GetTourDto>($"/tours/{id}", cancellationToken);
}
```

**Why avoid default values?**

1. **Explicit intent**: Callers must consciously decide whether to support cancellation
2. **Prevents silent bugs**: Missing cancellation tokens become compile errors, not runtime issues
3. **Better discoverability**: Developers see that the operation supports cancellation
4. **Consistent patterns**: All async methods clearly show cancellation support

**For callers who don't need cancellation:**

```csharp
// Explicitly pass CancellationToken.None
var tour = await toursApi.GetTourById(tourId, CancellationToken.None);
```

**Exception:** Private helper methods within a class may use `= default` for convenience, but all public APIs should
require explicit cancellation tokens.
