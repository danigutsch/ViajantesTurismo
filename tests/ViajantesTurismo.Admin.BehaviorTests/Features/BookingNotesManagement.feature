Feature: Booking Notes Management
As a tour operator
I want to manage booking notes
So that important information about bookings can be recorded and updated

    Scenario: Updating booking notes
        Given a pending booking exists
        When the operator updates the notes to "Customer requested vegetarian meals"
        Then the booking notes should be "Customer requested vegetarian meals"

    Scenario: Updating booking notes to null clears notes
        Given a pending booking exists
        When the operator updates the notes to null
        Then the booking notes should be null

    Scenario: Updating booking notes to empty string
        Given a pending booking exists
        When the operator updates the notes to ""
        Then the booking notes should be ""

    Scenario: Cannot update booking notes exceeding max length
        Given a pending booking exists
        When the operator tries to update the notes to a string longer than 2000 characters
        Then the result should fail with message starting with "Notes cannot exceed 2000 characters"

    Scenario: Booking notes with whitespace is trimmed
        Given a pending booking exists
        When the operator updates the notes to "  Customer needs assistance  "
        Then the booking notes should be "Customer needs assistance"

    Scenario: Booking notes preserves multiple lines and formatting
        Given a pending booking exists
        When the operator updates the notes to "Line 1\nLine 2\n\nLine 4"
        Then the booking notes should be "Line 1\nLine 2\n\nLine 4"

    Scenario: Booking notes with excessive whitespace around preserves internal formatting
        Given a pending booking exists
        When the operator updates the notes to "  Important:\n- Item 1\n- Item 2  "
        Then the booking notes should be "Important:\n- Item 1\n- Item 2"