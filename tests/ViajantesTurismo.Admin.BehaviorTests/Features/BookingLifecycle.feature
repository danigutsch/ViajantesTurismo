Feature: Booking Lifecycle
As a tour operator
I want to manage booking status transitions
So that bookings follow valid business rules

    Scenario: Confirming a pending booking
        Given a pending booking exists
        When the operator confirms the booking
        Then the booking status should be "Confirmed"

    Scenario: Confirming an already confirmed booking is idempotent
        Given a confirmed booking exists
        When the operator confirms the booking
        Then the booking status should be "Confirmed"

    Scenario: Cannot confirm a cancelled booking
        Given a cancelled booking exists
        When the operator tries to confirm the booking
        Then the result should fail with message "Cannot transition from Cancelled to Confirmed."

    Scenario: Cannot confirm a completed booking
        Given a completed booking exists
        When the operator tries to confirm the booking
        Then the result should fail with message "Cannot transition from Completed to Confirmed."

    Scenario: Cancelling a pending booking
        Given a pending booking exists
        When the operator cancels the booking
        Then the booking status should be "Cancelled"

    Scenario: Cancelling a confirmed booking
        Given a confirmed booking exists
        When the operator cancels the booking
        Then the booking status should be "Cancelled"

    Scenario: Cancelling an already cancelled booking is idempotent
        Given a cancelled booking exists
        When the operator cancels the booking
        Then the booking status should be "Cancelled"

    Scenario: Cannot cancel a completed booking
        Given a completed booking exists
        When the operator tries to cancel the booking
        Then the result should fail with message "Cannot transition from Completed to Cancelled."

    Scenario: Completing a confirmed booking
        Given a confirmed booking exists
        When the operator completes the booking
        Then the booking status should be "Completed"

    Scenario: Completing a pending booking
        Given a pending booking exists
        When the operator completes the booking
        Then the booking status should be "Completed"

    Scenario: Completing an already completed booking is idempotent
        Given a completed booking exists
        When the operator completes the booking
        Then the booking status should be "Completed"

    Scenario: Cannot complete a cancelled booking
        Given a cancelled booking exists
        When the operator tries to complete the booking
        Then the result should fail with message "Cannot transition from Cancelled to Completed."

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

    Scenario: Updating booking payment status
        Given a pending booking exists
        When the operator updates the payment status to "Paid"
        Then the booking payment status should be "Paid"

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