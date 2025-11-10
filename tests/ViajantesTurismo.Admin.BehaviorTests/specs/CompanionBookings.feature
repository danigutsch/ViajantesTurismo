@BC:Admin @Agg:Tour @Entity:Booking @regression
Feature: Companion Bookings and Bike Selection

  **Business Need:** Tour operators must handle bookings for couples and friends traveling together,
  with independent bike selection for each traveler and appropriate room pricing based on occupancy.

  **Key Business Rules:**
  - Each booking has a principal customer (required) and optional companion customer
  - Principal and companion must be different people (INV-TOUR-016)
  - Both principal and companion must select a bike type (Regular or EBike) (INV-TOUR-017)
  - BikeType.None is not allowed (INV-TOUR-017)
  - Double rooms with companion avoid single room supplement
  - Single rooms without companion incur single room supplement

  **Related Invariants:**
  - INV-TOUR-016: Principal and companion customers cannot be the same person
  - INV-TOUR-017: BikeType.None cannot be used for bookings (must select Regular or EBike)

Background:
  Given I am authenticated as a tour operator
  And a tour exists

Rule: Companion bookings allow different bike types per traveler

  @happy_path
  Scenario: Book with companion both using regular bikes
    Given a principal customer exists
    And a companion customer exists
    When I add a booking with principal customer 1 on regular bike and companion customer 2 on regular bike in double room
    Then the booking should have a companion customer
    And the booking should include principal bike price
    And the booking should include companion bike price

  @happy_path
  Scenario: Book with companion using different bike types
    Given a principal customer exists
    And a companion customer exists
    When I add a booking with principal customer 1 on regular bike and companion customer 2 on e-bike in double room
    Then the booking should have a companion customer
    And the booking should include principal regular bike price
    And the booking should include companion e-bike price

  @happy_path
  Scenario: Book with companion both using e-bikes
    Given a principal customer exists
    And a companion customer exists
    When I add a booking with principal customer 1 on e-bike and companion customer 2 on e-bike in double room
    Then the booking should have a companion customer
    And both customers should have e-bike pricing

Rule: Room supplements depend on occupancy

  @happy_path
  Scenario: Double room with companion avoids single supplement
    Given a principal customer exists
    And a companion customer exists
    When I add a booking with principal customer 1 on regular bike and companion customer 2 on regular bike in double room
    Then the booking should not include single room supplement

  @happy_path
  Scenario: Single room without companion includes supplement
    Given a principal customer exists
    When I add a booking with principal customer 1 on regular bike without companion in single room
    Then the booking should not have a companion customer
    And the booking should include single room supplement

Rule: Principal and companion must be different people

  @Invariant:INV-TOUR-016 @error_case
  Scenario: Reject companion booking when principal and companion are the same customer
    Given a principal customer exists
    When I add a booking with principal customer 1 on regular bike and companion customer 1 on regular bike in double room
    Then the booking creation should fail
    And the error should be for field "companionCustomerId"
    And the error message should contain "Principal and companion customers cannot be the same person"

Rule: Both principal and companion must select valid bike type

  @Invariant:INV-TOUR-017 @error_case
  Scenario: Reject companion booking when companion has no bike type
    Given a principal customer exists
    And a companion customer exists
    When I add a booking with principal customer 1 on regular bike and companion customer 2 with no bike type in double room
    Then the booking creation should fail
    And the error should be for field "companionBikeType"
    And the error message should contain "Bike type must be selected"

  @Invariant:INV-TOUR-017 @error_case
  Scenario: Reject booking when principal has no bike type
    Given a principal customer exists
    When I add a booking with principal customer 1 with no bike type without companion in single room
    Then the booking creation should fail
    And the error should be for field "principalBikeType"
    And the error message should contain "Bike type must be selected"
