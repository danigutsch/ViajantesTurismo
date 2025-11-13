@BC:Admin @Agg:Tour @regression
Feature: Tour Capacity Management

  **Business Need:** Tour operators must control participant numbers to ensure tours are
  economically viable (minimum participants) while not exceeding safety or resource limits (maximum capacity).

  **Key Business Rules:**
  - Minimum capacity: 1-20 customers
  - Maximum capacity: 1-20 customers
  - Minimum must be less than or equal to maximum
  - Only confirmed bookings count toward capacity
  - Cannot exceed maximum capacity with confirmed bookings

  **Related Invariants:**
  - INV-TOUR-010: Minimum customers must be between 1 and 20
  - INV-TOUR-011: Maximum customers must be between 1 and 20
  - INV-TOUR-012: Minimum customers must be less than or equal to maximum
  - INV-TOUR-013: Cannot exceed maximum customer capacity (confirmed bookings only)
  - INV-TOUR-014: Cannot reduce maximum capacity below current confirmed bookings

Background:
  Given I am authenticated as a tour operator
  And I have valid tour details

Rule: Tour capacity must be within valid ranges

  @Invariant:INV-TOUR-010 @Invariant:INV-TOUR-011 @Invariant:INV-TOUR-012 @happy_path
  Scenario: Create tour with valid capacity configuration
    When I create a tour with minimum 4 and maximum 12 customers
    Then the tour should be created successfully
    And the minimum capacity should be 4
    And the maximum capacity should be 12

  @Invariant:INV-TOUR-010 @Invariant:INV-TOUR-011 @Invariant:INV-TOUR-012 @happy_path
  Scenario: Update tour capacity within valid ranges
    Given a tour exists with minimum 4 and maximum 12 customers
    When I update the capacity to minimum 6 and maximum 15
    Then the capacity update should succeed
    And the minimum capacity should be 6
    And the maximum capacity should be 15

  @Invariant:INV-TOUR-014 @error_case
  Scenario: Reject capacity reduction below current confirmed bookings
    Given a tour exists with minimum 4 and maximum 12 customers
    And the tour has 2 confirmed bookings with 2 customers each
    When I try to update the capacity to minimum 2 and maximum 3
    Then the capacity update should fail
    And the error should indicate cannot reduce capacity below current bookings

Rule: Minimum capacity must be less than or equal to maximum

  @Invariant:INV-TOUR-012 @error_case
  Scenario: Reject tour when maximum is less than minimum
    When I try to create a tour with minimum 10 and maximum 5 customers
    Then the tour creation should fail
    And the error should indicate max must be at least min

Rule: Minimum capacity must be at least 1

  @Invariant:INV-TOUR-010 @error_case
  Scenario: Reject tour with minimum capacity below 1
    When I try to create a tour with minimum 0 and maximum 10 customers
    Then the tour creation should fail
    And the error should indicate minimum must be at least 1

Rule: Maximum capacity cannot exceed 20

  @Invariant:INV-TOUR-011 @error_case
  Scenario: Reject tour with maximum capacity above 20
    When I try to create a tour with minimum 5 and maximum 25 customers
    Then the tour creation should fail
    And the error should indicate maximum cannot exceed 20

Rule: Only confirmed bookings count toward capacity limits

  @Invariant:INV-TOUR-013 @happy_path
  Scenario: Prevent booking when tour reaches maximum capacity
    Given a tour exists with minimum 2 and maximum 3 customers
    And the tour has 2 confirmed bookings with 1 customer each
    And a third customer exists
    When I try to add a booking for the third customer
    Then the booking should be created successfully
    When I try to add a booking for a fourth customer
    Then the booking creation should fail
    And the error should indicate the tour is fully booked

  @Invariant:INV-TOUR-013 @happy_path
  Scenario: Calculate available capacity correctly
    Given a tour exists with minimum 4 and maximum 10 customers
    And the tour has 2 confirmed bookings with 1 customer each
    Then the current customer count should be 2
    And the available spots should be 8
    And the tour should not be at minimum capacity
    And the tour should not be fully booked

  @Invariant:INV-TOUR-013 @happy_path
  Scenario: Count companion bookings toward capacity
    Given a tour exists with minimum 4 and maximum 10 customers
    And the tour has 1 confirmed booking with 2 customers
    And the tour has 1 confirmed booking with 1 customer
    Then the current customer count should be 3
    And the available spots should be 7

  @Invariant:INV-TOUR-013 @happy_path
  Scenario: Exclude non-confirmed bookings from capacity calculation
    Given a tour exists with minimum 4 and maximum 10 customers
    And the tour has 2 confirmed bookings with 1 customer each
    And the tour has 1 pending booking with 1 customer
    And the tour has 1 cancelled booking with 1 customer
    Then the current customer count should be 2
    And the available spots should be 8
