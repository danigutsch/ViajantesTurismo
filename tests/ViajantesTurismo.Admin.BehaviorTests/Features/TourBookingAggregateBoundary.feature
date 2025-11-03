Feature: Tour Booking Aggregate Boundary
As a tour operator
I want to ensure bookings can only be modified through tour methods
So that aggregate boundaries are respected and data consistency is maintained

    Background:
        Given a tour exists

    Scenario: Cannot access booking methods directly - they are internal
        When I try to access booking methods directly
        Then the methods should not be accessible
        And only tour methods should be available

    Scenario: Adding booking through tour succeeds
        When I add a booking for the customer to the tour with price 1500.00
        Then the tour should have the booking
        And the booking should be in pending status

    Scenario: Removing booking through tour succeeds
        Given a tour exists with a booking
        When I remove the booking from the tour
        Then the tour should not have the booking

    Scenario: Cannot confirm non-existent booking
        When I try to confirm a non-existent booking
        Then the operation should fail with not found error
        And the error message should contain "Booking with ID 99999 not found"

    Scenario: Cannot update notes for non-existent booking
        When I try to update notes for a non-existent booking
        Then the operation should fail with not found error
        And the error message should contain "Booking with ID 99999 not found"

    Scenario: Cannot update payment status for non-existent booking
        When I try to update payment status for a non-existent booking
        Then the operation should fail with not found error
        And the error message should contain "Booking with ID 99999 not found"

    Scenario: Cannot cancel non-existent booking
        When I try to cancel a non-existent booking
        Then the operation should fail with not found error
        And the error message should contain "Booking with ID 99999 not found"

    Scenario: Cannot complete non-existent booking
        When I try to complete a non-existent booking
        Then the operation should fail with not found error
        And the error message should contain "Booking with ID 99999 not found"

    Scenario: Cannot remove non-existent booking
        When I try to remove a non-existent booking
        Then the operation should fail with not found error
        And the error message should contain "Booking with ID 99999 not found"

    Scenario: Tour maintains collection of bookings
        Given a tour exists
        When I add a booking for the customer to the tour with price 1500.00
        And I add a booking for the customer to the tour with price 2000.00
        Then the tour should have 2 bookings

    Scenario: Removing booking updates tour's booking collection
        Given a tour exists with a booking
        When I remove the booking from the tour
        Then the tour should have 0 bookings

    Scenario: All booking lifecycle through tour
        Given a tour exists
        When I add a booking for the customer to the tour with price 1500.00
        And I confirm the booking through the tour
        And I update the booking notes to "VIP customer" through the tour
        Then the booking status should be "Confirmed"
        And the booking notes should be "VIP customer"