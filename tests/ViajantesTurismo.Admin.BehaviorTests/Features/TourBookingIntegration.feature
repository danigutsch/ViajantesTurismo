Feature: Tour-Booking Integration
As a tour operator
I want to manage bookings within tours
So that tour bookings are properly tracked

    Scenario: Adding a booking to a tour
        Given a tour exists
        And a customer exists
        When I add a booking for the customer to the tour with price 1500.00
        Then the tour should have the booking
        And the booking should be in pending status

    Scenario: Confirming a booking through the tour
        Given a tour exists with a pending booking
        When I confirm the booking through the tour
        Then the booking status should be "Confirmed"

    Scenario: Cancelling a booking through the tour
        Given a tour exists with a pending booking
        When I cancel the booking through the tour
        Then the booking status should be "Cancelled"

    Scenario: Completing a booking through the tour
        Given a tour exists with a confirmed booking
        When I complete the booking through the tour
        Then the booking status should be "Completed"

    Scenario: Updating booking price through the tour
        Given a tour exists with a booking priced at 1500.00
        When I update the booking price to 1800.00 through the tour
        Then the booking price should be 1800.00

    Scenario: Updating booking notes through the tour
        Given a tour exists with a booking
        When I update the booking notes to "Dietary restrictions noted" through the tour
        Then the booking notes should be "Dietary restrictions noted"

    Scenario: Updating booking details through the tour
        Given a tour exists with a booking priced at 1500.00 and notes "Original notes"
        When I update both price to 1800.00 and notes to "Updated notes" through the tour
        Then the booking price should be 1800.00
        And the booking notes should be "Updated notes"

    Scenario: Removing a booking from a tour
        Given a tour exists with a booking
        When I remove the booking from the tour
        Then the tour should not have the booking

    Scenario: Cannot confirm booking that doesn't exist in tour
        Given a tour exists
        When I try to confirm a non-existent booking
        Then the result should fail with message "not found in this tour"

    Scenario: Cannot cancel booking that doesn't exist in tour
        Given a tour exists
        When I try to cancel a non-existent booking
        Then the result should fail with message "not found in this tour"

    Scenario: Cannot complete booking that doesn't exist in tour
        Given a tour exists
        When I try to complete a non-existent booking
        Then the result should fail with message "not found in this tour"

    Scenario: Cannot update price for booking that doesn't exist in tour
        Given a tour exists
        When I try to update price for a non-existent booking
        Then the result should fail with message "not found in this tour"

    Scenario: Cannot update notes for booking that doesn't exist in tour
        Given a tour exists
        When I try to update notes for a non-existent booking
        Then the result should fail with message "not found in this tour"

    Scenario: Cannot update payment status for booking that doesn't exist in tour
        Given a tour exists
        When I try to update payment status for a non-existent booking
        Then the result should fail with message "not found in this tour"

    Scenario: Full booking update through tour
        Given a tour exists with a pending booking priced at 1500.00
        When I update the booking with price 2000.00, notes "Updated", status "Confirmed", and payment "Paid"
        Then the booking price should be 2000.00
        And the booking notes should be "Updated"
        And the booking status should be "Confirmed"
        And the booking payment status should be "Paid"