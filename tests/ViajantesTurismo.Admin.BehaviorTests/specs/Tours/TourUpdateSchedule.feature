Feature: Tour Update Schedule
As a tour operator
I want to update tour start and end dates
So that I can adjust tour schedules when needed

    Scenario: Successfully update tour schedule with valid dates
        Given a tour exists with dates from "2024-06-01" to "2024-06-15"
        When I update the tour schedule to start "2024-07-01" and end "2024-07-20"
        Then the tour schedule update should succeed
        And the tour start date should be "2024-07-01"
        And the tour end date should be "2024-07-20"

    Scenario: Cannot update tour schedule with end date before start date
        Given a tour exists with dates from "2024-06-01" to "2024-06-15"
        When I update the tour schedule to start "2024-07-20" and end "2024-07-10"
        Then the tour schedule update should fail
        And the error should contain "date"

    Scenario: Cannot update tour schedule with end date equal to start date
        Given a tour exists with dates from "2024-06-01" to "2024-06-15"
        When I update the tour schedule to start "2024-07-01" and end "2024-07-01"
        Then the tour schedule update should fail
        And the error should contain "date"

    Scenario: Cannot update tour schedule with duration too short
        Given a tour exists with dates from "2024-06-01" to "2024-06-15"
        When I update the tour schedule to start "2024-07-01" and end "2024-07-05"
        Then the tour schedule update should fail
        And the error should contain "days long"

    Scenario: Successfully update tour schedule with minimum duration
        Given a tour exists with dates from "2024-06-01" to "2024-06-15"
        When I update the tour schedule to start "2024-07-01" and end "2024-07-07"
        Then the tour schedule update should succeed
        And the tour start date should be "2024-07-01"
        And the tour end date should be "2024-07-07"

    Scenario: Successfully extend tour duration
        Given a tour exists with dates from "2024-06-01" to "2024-06-10"
        When I update the tour schedule to start "2024-06-01" and end "2024-06-30"
        Then the tour schedule update should succeed
        And the tour end date should be "2024-06-30"

    Scenario: Successfully shorten tour duration
        Given a tour exists with dates from "2024-06-01" to "2024-06-30"
        When I update the tour schedule to start "2024-06-01" and end "2024-06-15"
        Then the tour schedule update should succeed
        And the tour end date should be "2024-06-15"