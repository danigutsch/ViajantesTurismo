Feature: Booking Sanitization
As a tour operator
I want booking data to be automatically sanitized
So that data is clean and consistent in the system

    Background:
        Given a tour exists

    Scenario: Booking notes with leading and trailing whitespace is trimmed
        When I add a booking with notes "  Special requirements  "
        Then the booking notes should be "Special requirements"

    Scenario: Update booking notes with whitespace is trimmed
        Given a tour exists with a booking
        When I update the booking notes to "  Updated notes  " through the tour
        Then the booking notes should be "Updated notes"

    Scenario: Booking notes with only whitespace becomes null
        When I add a booking with notes "     "
        Then the booking notes should be null or empty

    Scenario: Update booking notes with only whitespace becomes null
        Given a tour exists with a booking priced at 1500.00 and notes "Initial"
        When I update the booking notes to "   " through the tour
        Then the booking notes should be null or empty

    Scenario: Null booking notes remains null
        When I add a booking with null notes
        Then the booking notes should be null or empty

    Scenario: Update booking notes to null
        Given a tour exists with a booking priced at 1500.00 and notes "Initial"
        When I update the booking notes to null through the tour
        Then the booking notes should be null or empty

    Scenario: Very long booking notes is trimmed to max length during validation
        When I add a booking with notes exceeding 2000 characters
        Then the booking creation should fail with notes validation error