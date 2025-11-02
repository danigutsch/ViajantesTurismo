Feature: Booking Validation
As a tour operator
I want booking data to be validated
So that only valid bookings are created and updated

    Background:
        Given a tour exists

    Scenario: Cannot update booking with zero price
        Given a tour exists with a booking priced at 1500.00
        When I try to update the booking price to 0.00 through the tour
        Then the booking update should fail with validation error for "totalPrice"
        And the error message should contain "Total price must be greater than zero"

    Scenario: Cannot update booking with negative price
        Given a tour exists with a booking
        When I try to update the booking price to -500.00 through the tour
        Then the booking update should fail with validation error for "totalPrice"
        And the error message should contain "Total price must be greater than zero"

    Scenario: Cannot update booking with price exceeding maximum
        Given a tour exists with a booking priced at 1500.00
        When I try to update the booking price to 100001.00 through the tour
        Then the booking update should fail with validation error for "totalPrice"
        And the error message should contain "Total price must be less than 100000"

    Scenario: Update booking with price at maximum allowed value
        Given a tour exists with a booking priced at 1500.00
        When I update the booking price to 100000.00 through the tour
        Then the booking price should be 100000.00

    Scenario: Update booking with valid price
        Given a tour exists with a booking priced at 1500.00
        When I update the booking price to 2000.00 through the tour
        Then the booking price should be 2000.00

    Scenario: Cannot update booking notes exceeding maximum length
        Given a tour exists with a booking
        When I try to update the booking notes with 2001 characters through the tour
        Then the booking update should fail with validation error for "notes"
        And the error message should contain "cannot exceed 2000 characters"

    Scenario: Update booking notes at maximum length
        Given a tour exists with a booking
        When I try to update the booking notes with 2000 characters through the tour
        Then the booking notes should be updated successfully

    Scenario: Update booking notes with valid length
        Given a tour exists with a booking
        When I update the booking notes to "Special dietary requirements" through the tour
        Then the booking notes should be "Special dietary requirements"

    Scenario: Update booking notes with empty string
        Given a tour exists with a booking priced at 1500.00 and notes "Initial notes"
        When I update the booking notes to "" through the tour
        Then the booking notes should be null or empty

    Scenario: Update booking notes with whitespace only
        Given a tour exists with a booking
        When I update the booking notes to "   " through the tour
        Then the booking notes should be null or empty

    Scenario: Cannot confirm cancelled booking
        Given a tour exists with a cancelled booking
        When I try to confirm the booking through the tour
        Then the booking update should fail with conflict error
        And the error message should contain "Cannot transition from Cancelled to Confirmed"

    Scenario: Cannot confirm completed booking
        Given a tour exists with a completed booking
        When I try to confirm the booking through the tour
        Then the booking update should fail with conflict error
        And the error message should contain "Cannot transition from Completed to Confirmed"

    Scenario: Confirm confirmed booking is idempotent
        Given a tour exists with a confirmed booking
        When I confirm the booking through the tour
        Then the booking status should be "Confirmed"

    Scenario: Cannot cancel completed booking
        Given a tour exists with a completed booking
        When I try to cancel the booking through the tour
        Then the booking update should fail with conflict error
        And the error message should contain "Cannot transition from Completed to Cancelled"

    Scenario: Cancel cancelled booking is idempotent
        Given a tour exists with a cancelled booking
        When I cancel the booking through the tour
        Then the booking status should be "Cancelled"

    Scenario: Cannot complete cancelled booking
        Given a tour exists with a cancelled booking
        When I try to complete the booking through the tour
        Then the booking update should fail with conflict error
        And the error message should contain "Cannot transition from Cancelled to Completed"

    Scenario: Complete completed booking is idempotent
        Given a tour exists with a completed booking
        When I complete the booking through the tour
        Then the booking status should be "Completed"

    Scenario: Valid booking status transition from pending to confirmed
        Given a tour exists with a pending booking
        When I confirm the booking through the tour
        Then the booking status should be "Confirmed"

    Scenario: Valid booking status transition from pending to cancelled
        Given a tour exists with a pending booking
        When I cancel the booking through the tour
        Then the booking status should be "Cancelled"

    Scenario: Valid booking status transition from pending to completed
        Given a tour exists with a pending booking
        When I complete the booking through the tour
        Then the booking status should be "Completed"

    Scenario: Valid booking status transition from confirmed to cancelled
        Given a tour exists with a confirmed booking
        When I cancel the booking through the tour
        Then the booking status should be "Cancelled"

    Scenario: Valid booking status transition from confirmed to completed
        Given a tour exists with a confirmed booking
        When I complete the booking through the tour
        Then the booking status should be "Completed"

    Scenario: Update payment status on pending booking
        Given a tour exists with a pending booking
        When I update the payment status to "Paid" through the tour
        Then the booking payment status should be "Paid"

    Scenario: Update payment status on confirmed booking
        Given a tour exists with a confirmed booking
        When I update the payment status to "PartiallyPaid" through the tour
        Then the booking payment status should be "PartiallyPaid"

    Scenario: Update payment status on cancelled booking
        Given a tour exists with a cancelled booking
        When I update the payment status to "Refunded" through the tour
        Then the booking payment status should be "Refunded"

    Scenario: Update payment status on completed booking
        Given a tour exists with a completed booking
        When I update the payment status to "Paid" through the tour
        Then the booking payment status should be "Paid"
