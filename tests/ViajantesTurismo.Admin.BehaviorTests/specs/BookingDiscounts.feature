@BC:Admin @Agg:Booking @Entity:Discount @regression
Feature: Booking Discounts
  As a booking administrator
  I want to apply discounts to bookings
  So that I can offer promotional pricing, early bird specials, and custom rates

  Discounts allow us to provide flexible pricing for customers while maintaining
  proper business controls. Discounts can be percentage-based or absolute amounts,
  with validation to ensure final prices remain positive and reasonable.

  Business Rules:
  - Percentage discounts must be between 0 and 100%
  - Absolute discounts cannot exceed the booking subtotal
  - Final price after discount must be greater than zero
  - Discount reasons can be recorded for tracking and reporting
  - Negative discounts are not allowed

  Background:
    Given a tour exists with base price 2000, double room supplement 500, regular bike price 100, and e-bike price 200

  Rule: Percentage discounts must be valid and within limits
    Percentage discounts provide a proportional reduction in price.
    They must be between 0 and 100% to ensure valid pricing.

    @happy_path
    Scenario: Create booking without discount
      When I create a booking with principal customer 1, regular bike, single room, and no discount
      Then the booking should be created successfully
      And the booking total price should be 2100

    @happy_path
    Scenario: Apply percentage discount to booking
      When I create a booking with principal customer 1, regular bike, single room, and 10% discount
      Then the booking should be created successfully
      And the booking total price should be 1890

    @happy_path
    Scenario: Apply percentage discount with companion
      When I create a booking with principal customer 1 regular bike, companion customer 2 e-bike, double room, and 20% discount
      Then the booking should be created successfully
      And the booking total price should be 2240

    @Invariant:INV-TOUR-024 @error_case @critical
    Scenario: Percentage discount cannot exceed 100%
      When I attempt to apply a 101% discount to a booking
      Then I should not be able to create the booking
      And I should be informed that percentage discounts cannot exceed 100%

    @Invariant:INV-TOUR-024 @error_case
    Scenario: Discount cannot be negative
      When I attempt to apply a -10% discount to a booking
      Then I should not be able to create the booking
      And I should be informed that discounts cannot be negative

    @happy_path @edge_case
    Scenario: Apply maximum valid percentage discount
      When I create a booking with principal customer 1, regular bike, single room, and 99.9% discount
      Then the booking should be created successfully
      And the booking total price should be approximately 2.1

    @happy_path
    Scenario: Apply zero discount
      When I create a booking with principal customer 1, regular bike, single room, and 0% discount
      Then the booking should be created successfully
      And the booking total price should be 2100

  Rule: Absolute discounts must not exceed booking subtotal
    Absolute discounts provide a fixed amount reduction. They cannot
    exceed the booking subtotal as this would result in negative pricing.

    @happy_path
    Scenario: Apply absolute discount to booking
      When I create a booking with principal customer 1, regular bike, double room, and 150 absolute discount
      Then the booking should be created successfully
      And the booking total price should be 2450

    @Invariant:INV-TOUR-022 @error_case @critical
    Scenario: Absolute discount cannot exceed subtotal
      When I attempt to apply a 3000 absolute discount to a 2100 booking
      Then I should not be able to create the booking
      And I should be informed that the discount cannot exceed the subtotal

  Rule: Final price after discount must be positive
    After applying any discount, the final booking price must remain
    positive to ensure valid transactions and prevent billing errors.

    @Invariant:INV-TOUR-023 @error_case @critical
    Scenario: Discount resulting in zero price is rejected
      When I attempt to apply a 100% discount to a booking
      Then I should not be able to create the booking
      And I should be informed that the final price must be greater than zero

    @Invariant:INV-TOUR-023 @error_case
    Scenario: Absolute discount equal to subtotal is rejected
      When I attempt to apply a 2100 absolute discount to a 2100 booking
      Then I should not be able to create the booking
      And I should be informed that the final price must be greater than zero

  Rule: Discount reasons can be recorded
    Discounts can include a reason for tracking promotional campaigns,
    early bird specials, or negotiated rates.

    @happy_path
    Scenario: Record discount reason
      When I create a booking with principal customer 1, regular bike, single room, 15% discount, and reason "Early bird special"
      Then the booking should be created successfully
      And the booking should have discount reason "Early bird special"

    @happy_path
    Scenario Outline: Various discount scenarios
      When I create a booking with base price <base>, room cost <room>, principal bike <bike1>, companion bike <bike2>, and <discount>% discount
      Then the booking should be created successfully
      And the booking total price should be <total>

      Examples:
        | base | room | bike1 | bike2 | discount | total |
        | 2000 | 0    | 100   | 0     | 10       | 1890  |
        | 2000 | 0    | 100   | 0     | 50       | 1050  |
        | 2000 | 500  | 100   | 200   | 15       | 2380  |
        | 2000 | 500  | 100   | 200   | 25       | 2100  |
        | 2000 | 0    | 100   | 0     | 5        | 1995  |
