Feature: Booking Status Lifecycle
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

    Scenario: Completing a pending booking requires confirmation first
    Given a pending booking exists
    When the operator tries to complete the booking
    Then the operation should fail
    And the error message should contain "Booking must be confirmed before it can be completed"

    Scenario: Completing an already completed booking is idempotent
        Given a completed booking exists
        When the operator completes the booking
        Then the booking status should be "Completed"

    Scenario: Cannot complete a cancelled booking
        Given a cancelled booking exists
        When the operator tries to complete the booking
        Then the result should fail with message "Cannot transition from Cancelled to Completed."
