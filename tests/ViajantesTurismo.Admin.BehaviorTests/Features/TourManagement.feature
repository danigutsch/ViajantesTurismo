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

    Scenario: Cannot create tour with duration too short
        Given I have tour dates from "2025-06-01" to "2025-06-03"
        When I try to create the tour
        Then the tour creation should fail with validation error for "schedule"

    Scenario: Cannot update schedule with duration too short
        Given an existing tour with dates from "2025-06-01" to "2025-06-10"
        When I try to update the schedule to "2025-07-01" to "2025-07-03"
        Then the schedule update should fail with validation error for "schedule"

    Scenario: Cannot update base price with negative value
        Given an existing tour with base price 2000.00
        When I try to update the base price to -100.00
        Then the price update should fail

    Scenario: Cannot update base price exceeding maximum
        Given an existing tour with base price 2000.00
        When I try to update the base price to 100001.00
        Then the price update should fail

    Scenario: Update base price to zero succeeds
        Given an existing tour with base price 2000.00
        When I update the base price to 0.00
        Then the tour base price should be 0.00

    Scenario: Cannot update basic info with empty identifier
        Given an existing tour with identifier "CUBA2024" and name "Cuba Adventure"
        When I try to update the identifier to "" and name to "New Name"
        Then the basic info update should fail with validation error for "identifier"

    Scenario: Cannot update basic info with empty name
        Given an existing tour with identifier "CUBA2024" and name "Cuba Adventure"
        When I try to update the identifier to "CUBA2025" and name to ""
        Then the basic info update should fail with validation error for "name"

    Scenario: Cannot update basic info with identifier too long
        Given an existing tour with identifier "CUBA2024" and name "Cuba Adventure"
        When I try to update the identifier to a string longer than 128 characters
        Then the basic info update should fail with validation error for "identifier"

    Scenario: Cannot update basic info with name too long
        Given an existing tour with identifier "CUBA2024" and name "Cuba Adventure"
        When I try to update the name to a string longer than 128 characters
        Then the basic info update should fail with validation error for "name"

    Scenario: Cannot update pricing with negative single room supplement
        Given an existing tour
        When I try to update pricing with single room -100.00
        Then the pricing update should fail with validation error for "price"

    Scenario: Cannot update pricing with negative regular bike price
        Given an existing tour
        When I try to update pricing with regular bike -50.00
        Then the pricing update should fail with validation error for "price"

    Scenario: Cannot update pricing with negative e-bike price
        Given an existing tour
        When I try to update pricing with e-bike -75.00
        Then the pricing update should fail with validation error for "price"

    Scenario: Cannot update pricing with single room supplement exceeding maximum
        Given an existing tour
        When I try to update pricing with single room 100001.00
        Then the pricing update should fail with validation error for "price"

    Scenario: Cannot update pricing with regular bike price exceeding maximum
        Given an existing tour
        When I try to update pricing with regular bike 100001.00
        Then the pricing update should fail with validation error for "price"

    Scenario: Cannot update pricing with e-bike price exceeding maximum
        Given an existing tour
        When I try to update pricing with e-bike 100001.00
        Then the pricing update should fail with validation error for "price"
