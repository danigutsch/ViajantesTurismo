Feature: Booking Entity Sanitization
As a domain model
I want booking data to be automatically sanitized at the entity level
So that data consistency is maintained

    Scenario: Create booking sanitizes whitespace in notes
        When I create a booking with notes "   "
        Then the booking should be created successfully
        And the booking notes should be null

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