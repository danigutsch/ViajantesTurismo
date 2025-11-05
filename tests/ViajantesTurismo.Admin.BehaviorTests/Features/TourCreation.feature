Feature: Tour Creation
As a tour operator
I want to create tours with valid data
So that only properly validated tours enter the system

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

    Scenario: Cannot create tour with duration too short
        Given I have tour dates from "2025-06-01" to "2025-06-03"
        When I try to create the tour
        Then the tour creation should fail with validation error for "schedule"

    Scenario: Create tour with minimum valid duration (just over 3 days)
        Given I have tour dates from "2025-06-01" to "2025-06-06"
        When I create the tour
        Then the tour should be created successfully

    Scenario: DateRange preserves UTC time zone information
        Given I have UTC tour dates from "2025-06-01T10:00:00Z" to "2025-06-10T18:00:00Z"
        When I create the tour
        Then the tour should be created successfully
        And the tour should preserve UTC time zone

    Scenario: DateRange calculates duration with partial days
        Given I have tour dates from "2025-06-01T08:00:00" to "2025-06-06T14:00:00"
        When I create the tour
        Then the tour should be created successfully
        And the tour duration should be greater than 5 days

    Scenario: DateRange calculates duration correctly across months
        Given I have tour dates from "2025-05-28" to "2025-06-05"
        When I create the tour
        Then the tour should be created successfully
        And the tour duration should be 8 days

    Scenario: DateRange calculates duration correctly across years
        Given I have tour dates from "2025-12-28" to "2026-01-05"
        When I create the tour
        Then the tour should be created successfully
        And the tour duration should be 8 days

    Scenario: DateRange with leap year date handling
        Given I have tour dates from "2024-02-28" to "2024-03-05"
        When I create the tour
        Then the tour should be created successfully
        And the tour duration should be 6 days

    Scenario: Tour stores DateRange properties correctly
        Given I have UTC tour dates from "2025-06-15T14:30:00Z" to "2025-06-25T16:45:00Z"
        When I create the tour
        Then the tour should be created successfully
        And the tour StartDate should be "2025-06-15T14:30:00Z"
        And the tour EndDate should be "2025-06-25T16:45:00Z"

    Scenario: Create tour with long duration
        Given I have tour dates from "2025-06-01" to "2025-06-30"
        When I create the tour
        Then the tour should be created successfully
        And the tour duration should be 29 days

    Scenario: Cannot create tour with empty identifier
        Given I have tour details with identifier "" and name "Cuba Tour"
        When I try to create the tour
        Then the tour creation should fail with validation error for "identifier"

    Scenario: Cannot create tour with identifier too long
        Given I have tour details with identifier longer than 128 characters
        When I try to create the tour
        Then the tour creation should fail with validation error for "identifier"

    Scenario: Cannot create tour with empty name
        Given I have tour details with identifier "CUBA2024" and name ""
        When I try to create the tour
        Then the tour creation should fail with validation error for "name"

    Scenario: Cannot create tour with name too long
        Given I have tour details with name longer than 128 characters
        When I try to create the tour
        Then the tour creation should fail with validation error for "name"

    Scenario: Cannot create tour with negative base price
        Given I have tour details with base price -100.00
        When I try to create the tour
        Then the tour creation should fail with validation error for "price"

    Scenario: Cannot create tour with base price exceeding maximum
        Given I have tour details with base price 100001.00
        When I try to create the tour
        Then the tour creation should fail with validation error for "price"

    Scenario: Cannot create tour with negative single room supplement
        Given I have tour details with single room supplement -50.00
        When I try to create the tour
        Then the tour creation should fail with validation error for "price"

    Scenario: Cannot create tour with single room supplement exceeding maximum
        Given I have tour details with single room supplement 100001.00
        When I try to create the tour
        Then the tour creation should fail with validation error for "price"

    Scenario: Cannot create tour with negative regular bike price
        Given I have tour details with regular bike price -30.00
        When I try to create the tour
        Then the tour creation should fail with validation error for "price"

    Scenario: Cannot create tour with regular bike price exceeding maximum
        Given I have tour details with regular bike price 100001.00
        When I try to create the tour
        Then the tour creation should fail with validation error for "price"

    Scenario: Cannot create tour with negative e-bike price
        Given I have tour details with e-bike price -40.00
        When I try to create the tour
        Then the tour creation should fail with validation error for "price"

    Scenario: Cannot create tour with e-bike price exceeding maximum
        Given I have tour details with e-bike price 100001.00
        When I try to create the tour
        Then the tour creation should fail with validation error for "price"

    Scenario: Cannot create tour with zero base price
        Given I have tour details with base price 0.00
        When I try to create the tour
        Then the tour creation should fail with validation error for "price"

    Scenario: Cannot create tour with zero single room supplement
        Given I have tour details with single room supplement 0.00
        When I try to create the tour
        Then the tour creation should fail with validation error for "price"

    Scenario: Cannot create tour with zero regular bike price
        Given I have tour details with regular bike price 0.00
        When I try to create the tour
        Then the tour creation should fail with validation error for "price"

    Scenario: Cannot create tour with zero e-bike price
        Given I have tour details with e-bike price 0.00
        When I try to create the tour
        Then the tour creation should fail with validation error for "price"

    Scenario: Create tour with multiple validation errors
        Given I have tour details with multiple invalid values
        When I try to create the tour
        Then the tour creation should fail with multiple validation errors