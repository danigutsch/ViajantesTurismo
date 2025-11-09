@BC:Admin @Agg:Booking @regression
Feature: Booking Creation
  As a booking administrator
  I want to create bookings with valid configurations
  So that customers can reserve spots on tours

  Bookings represent customer reservations on tours. Each booking must meet
  validation requirements for pricing, room configuration, and bike selection
  to ensure accurate billing and proper tour logistics.

  Business Rules:
  - Base price must be positive and within business limits (> 0 and ≤ 100,000)
  - Room costs must be non-negative and within limits (≥ 0 and ≤ 100,000)
  - Notes cannot exceed 2,000 characters
  - Room types must be valid (SingleRoom or DoubleRoom)
  - Bike types must be specified for all travelers

  Background:
    Given I am authenticated as a booking administrator
    And a tour exists with standard pricing

  Rule: Booking pricing must be positive and within business limits
    All booking prices must be positive amounts within our business operating
    range. Base price must be greater than zero, and room costs must be
    non-negative, with all amounts up to 100,000.

    @Invariant:INV-TOUR-008 @smoke @happy_path
    Scenario: Create booking with valid pricing
      When I create a booking with base price 1000, room type "DoubleRoom", room cost 0, and regular bike 100 for principal
      Then the booking should be created successfully
      And the booking total price should be 1100

    @Invariant:INV-TOUR-008 @happy_path
    Scenario: Create booking with principal and companion
      When I create a booking with base price 1000, room type "DoubleRoom", room cost 200, regular bike 100 for principal, and eBike 200 for companion
      Then the booking should be created successfully
      And the booking total price should be 1500

    @Invariant:INV-TOUR-008 @error_case @critical
    Scenario: Base price must be positive
      When I attempt to create a booking with base price 0
      Then I should not be able to create the booking
      And I should be informed that the base price must be positive

    @Invariant:INV-TOUR-008 @error_case
    Scenario: Base price cannot be negative
      When I attempt to create a booking with base price -100
      Then I should not be able to create the booking
      And I should be informed that the base price must be positive

    @Invariant:INV-TOUR-009 @error_case
    Scenario: Base price cannot exceed business maximum
      When I attempt to create a booking with base price 100001
      Then I should not be able to create the booking
      And I should be informed that the price exceeds our maximum rate

    @Invariant:INV-TOUR-008 @error_case
    Scenario: Room cost cannot be negative
      When I attempt to create a booking with base price 1000 and room cost -100
      Then I should not be able to create the booking
      And I should be informed that room costs must be non-negative

    @Invariant:INV-TOUR-009 @error_case
    Scenario: Room cost cannot exceed business maximum
      When I attempt to create a booking with base price 1000 and room cost 100001
      Then I should not be able to create the booking
      And I should be informed that the cost exceeds our maximum rate

  Rule: Booking notes have length constraints
    Notes provide important booking details and must be kept within
    reasonable length limits for database storage and usability.

    @happy_path
    Scenario: Create booking with notes at maximum length
      When I create a booking with notes of 2000 characters
      Then the booking should be created successfully

    @error_case
    Scenario: Notes cannot exceed maximum length
      When I attempt to create a booking with notes of 2001 characters
      Then I should not be able to create the booking
      And I should be informed that notes cannot exceed 2000 characters

  Rule: Room types must be valid
    Bookings must specify a valid room configuration for proper
    accommodation planning and billing.

    @happy_path
    Scenario: Create booking with single room
      When I create a booking with base price 1000, room type "SingleRoom", room cost 0, and regular bike 100 for principal
      Then the booking should be created successfully
      And the booking should have room type "SingleRoom"

    @error_case
    Scenario Outline: Room type must be valid enum value
      When I attempt to create a booking with invalid room type <invalidValue>
      Then I should not be able to create the booking
      And I should be informed that the room type is invalid

      Examples:
        | invalidValue |
        | -1           |
        | 2            |
        | 99           |
