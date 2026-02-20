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

    Scenario: Updating booking notes through the tour
        Given a tour exists with a booking
        When I update the booking notes to "Dietary restrictions noted" through the tour
        Then the booking notes should be "Dietary restrictions noted"

    Scenario: Removing a booking from a tour
        Given a tour exists with a booking
        When I remove the booking from the tour
        Then the tour should not have the booking

    Scenario: Cannot remove booking that doesn't exist in tour
        Given a tour exists
        When I try to remove a non-existent booking
        Then the result should fail with message "not found in this tour"

    Scenario: Cannot confirm booking that doesn't exist in tour
        Given a tour exists
        When I try to confirm a non-existent booking
        Then the result should fail with message "not found in this tour"

    Scenario: Cannot cancel booking that doesn't exist in tour
        Given a tour exists
        When I try to cancel a non-existent booking
        Then the result should fail with message "not found in this tour"

    Scenario: Cannot complete a booking that doesn't exist in tour
        Given a tour exists
        When I try to complete a non-existent booking
        Then the result should fail with message "not found in this tour"

    Scenario: Cannot update notes for booking that doesn't exist in tour
        Given a tour exists
        When I try to update notes for a non-existent booking
        Then the result should fail with message "not found in this tour"

    Scenario: Tour AddBooking handles BikeType Regular correctly
        Given a tour exists
        When I add a booking to tour with bike type "Regular" and no companion
        Then the booking should be created successfully
        And the booking principal customer bike price should be the tour regular bike price

    Scenario: Tour AddBooking handles BikeType EBike correctly
        Given a tour exists
        When I add a booking to tour with bike type "EBike" and no companion
        Then the booking should be created successfully
        And the booking principal customer bike price should be the tour ebike price

    Scenario: Tour AddBooking handles Single room type correctly
        Given a tour exists
        When I add a booking to tour with room type "SingleOccupancy"
        Then the booking should be created successfully
        And the booking room additional cost should be the tour single room supplement

    Scenario: Tour AddBooking handles Double room type correctly
        Given a tour exists
        When I add a booking to tour with room type "DoubleOccupancy"
        Then the booking should be created successfully
        And the booking room additional cost should be 0

    Scenario: Tour status transitions handle all BookingStatus values - Confirm
        Given a tour exists with a pending booking
        When I confirm the booking through the tour
        Then the booking status should be "Confirmed"

    Scenario: Tour status transitions handle all BookingStatus values - Cancel
        Given a tour exists with a pending booking
        When I cancel the booking through the tour
        Then the booking status should be "Cancelled"

    Scenario: Tour status transitions handle all BookingStatus values - Complete
        Given a tour exists with a confirmed booking
        When I complete the booking through the tour
        Then the booking status should be "Completed"

    Scenario: Tour Confirm handles Confirmed status (idempotent)
        Given a tour exists with a confirmed booking
        When I confirm the booking through the tour
        Then the booking status should be "Confirmed"

    Scenario: Tour Confirm handles Cancelled status (invalid transition)
        Given a tour exists with a cancelled booking
        When I try to confirm the booking through the tour
        Then the booking update should fail with conflict error
        And the error message should contain "Cannot transition from Cancelled to Confirmed"

    Scenario: Tour Confirm handles Completed status (invalid transition)
        Given a tour exists with a completed booking
        When I try to confirm the booking through the tour
        Then the booking update should fail with conflict error
        And the error message should contain "Cannot transition from Completed to Confirmed"