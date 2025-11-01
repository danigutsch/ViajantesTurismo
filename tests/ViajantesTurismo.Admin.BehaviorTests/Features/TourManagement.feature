Feature: Tour Management
As a tour operator
I want to manage tour details
So that tours have valid and accurate information

    Scenario: Creating a tour with valid dates
        Given I have tour dates from "2025-06-01" to "2025-06-10"
        When I create the tour
        Then the tour should be created successfully

    Scenario: Cannot create tour with end date before start date
        Given I have tour dates from "2025-06-10" to "2025-06-01"
        When I try to create the tour
        Then the tour creation should fail with argument exception "End date must be after start date."

    Scenario: Cannot create tour with end date same as start date
        Given I have tour dates from "2025-06-01" to "2025-06-01"
        When I try to create the tour
        Then the tour creation should fail with argument exception "End date must be after start date."

    Scenario: Updating tour schedule with valid dates
        Given an existing tour with dates from "2025-06-01" to "2025-06-10"
        When I update the schedule to "2025-07-01" to "2025-07-15"
        Then the tour dates should be "2025-07-01" to "2025-07-15"

    Scenario: Cannot update tour schedule with invalid date range
        Given an existing tour with dates from "2025-06-01" to "2025-06-10"
        When I try to update the schedule to "2025-07-15" to "2025-07-01"
        Then the tour creation should fail with argument exception "End date must be after start date."

    Scenario: Updating tour basic information
        Given an existing tour with identifier "CUBA2024" and name "Cuba Adventure 2024"
        When I update the identifier to "CUBA2025" and name to "Cuba Deluxe 2025"
        Then the tour identifier should be "CUBA2025"
        And the tour name should be "Cuba Deluxe 2025"

    Scenario: Updating tour pricing
        Given an existing tour with base price 2000.00
        When I update the base price to 2500.00
        Then the tour base price should be 2500.00

    Scenario: Updating tour included services
        Given an existing tour with services "Hotel, Breakfast"
        When I update the services to "Hotel, Breakfast, Lunch, City Tour"
        Then the tour should include services "Hotel, Breakfast, Lunch, City Tour"

    Scenario: Updating tour currency
        Given an existing tour with currency "USD"
        When I update the currency to "EUR"
        Then the tour currency should be "EUR"

    Scenario: Updating all tour pricing components
        Given an existing tour
        When I update pricing with single room 600.00, regular bike 150.00, e-bike 250.00
        Then the tour pricing should reflect all updates
