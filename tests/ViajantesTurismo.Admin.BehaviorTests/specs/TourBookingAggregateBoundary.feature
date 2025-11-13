@BC:Admin @Agg:Tour @regression
Feature: Tour Booking Aggregate Boundary

  **Business Need:** Enforce Domain-Driven Design aggregate boundaries to maintain data consistency
  and ensure all booking modifications flow through the Tour aggregate root.

  **Key Business Rules:**
  - Bookings can only be created, modified, and removed through Tour methods
  - Booking internal methods are not accessible outside the aggregate
  - Tour maintains the authoritative collection of bookings
  - All booking lifecycle operations require the Tour aggregate

  **Related Invariants:**
  - INV-TOUR-014: Bookings can only be created/modified through Tour methods (aggregate boundary)

Background:
  Given I am authenticated as a tour operator
  And a tour exists

Rule: Bookings can only be modified through Tour aggregate

  @Invariant:INV-TOUR-014 @happy_path
  Scenario: Create booking through Tour aggregate
    When I add a booking for the customer to the tour with price 1500.00
    Then the tour should have the booking
    And the booking should be in pending status

  @Invariant:INV-TOUR-014 @happy_path
  Scenario: Remove booking through Tour aggregate
    Given a tour exists with a booking
    When I remove the booking from the tour
    Then the tour should not have the booking

  @Invariant:INV-TOUR-014 @happy_path
  Scenario: Manage complete booking lifecycle through Tour
    When I add a booking for the customer to the tour with price 1500.00
    And I confirm the booking through the tour
    And I update the booking notes to "VIP customer" through the tour
    Then the booking status should be "Confirmed"
    And the booking notes should be "VIP customer"

Rule: Tour maintains authoritative booking collection

  @Invariant:INV-TOUR-014 @happy_path
  Scenario: Tour tracks multiple bookings
    When I add a booking for the customer to the tour with price 1500.00
    And I add a booking for the customer to the tour with price 2000.00
    Then the tour should have 2 bookings

  @Invariant:INV-TOUR-014 @happy_path
  Scenario: Removing booking updates Tour collection
    Given a tour exists with a booking
    When I remove the booking from the tour
    Then the tour should have 0 bookings

Rule: Booking methods are not accessible outside aggregate

  @Invariant:INV-TOUR-014 @architectural
  Scenario: Booking internal methods are not publicly accessible
    When I try to access booking methods directly
    Then the methods should not be accessible
    And only tour methods should be available

Rule: Operations on non-existent bookings fail gracefully

  @error_case
  Scenario: Cannot confirm non-existent booking
    When I try to confirm a non-existent booking
    Then the operation should fail with not found error
    And the error message should contain "not found"

  @error_case
  Scenario: Cannot update notes for non-existent booking
    When I try to update notes for a non-existent booking
    Then the operation should fail with not found error
    And the error message should contain "not found"

  @error_case
  Scenario: Cannot cancel non-existent booking
    When I try to cancel a non-existent booking
    Then the operation should fail with not found error
    And the error message should contain "not found"

  @error_case
  Scenario: Cannot complete non-existent booking
    When I try to complete a non-existent booking
    Then the operation should fail with not found error
    And the error message should contain "not found"

  @error_case
  Scenario: Cannot remove non-existent booking
    When I try to remove a non-existent booking
    Then the operation should fail with not found error
    And the error message should contain "not found"
