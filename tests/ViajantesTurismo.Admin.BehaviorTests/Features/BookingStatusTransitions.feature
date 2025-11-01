Feature: Booking Status Transitions with Payment
As a tour operator
I want to ensure booking status and payment status can be managed independently
So that I can handle various real-world scenarios

    Scenario: Confirm booking then mark as paid
        Given a pending booking exists
        When the operator confirms the booking
        And the operator updates the payment status to "Paid"
        Then the booking status should be "Confirmed"
        And the booking payment status should be "Paid"

    Scenario: Mark as paid then confirm booking
        Given a pending booking exists
        When the operator updates the payment status to "Paid"
        And the operator confirms the booking
        Then the booking status should be "Confirmed"
        And the booking payment status should be "Paid"

    Scenario: Partial payment on confirmed booking
        Given a confirmed booking exists
        When the operator updates the payment status to "PartiallyPaid"
        Then the booking status should be "Confirmed"
        And the booking payment status should be "PartiallyPaid"

    Scenario: Complete booking with unpaid status
        Given a confirmed booking exists
        When the operator completes the booking
        Then the booking status should be "Completed"
        And the booking payment status should be "Unpaid"

    Scenario: Cancel booking and refund
        Given a confirmed booking exists
        When the operator cancels the booking
        And the operator updates the payment status to "Refunded"
        Then the booking status should be "Cancelled"
        And the booking payment status should be "Refunded"

    Scenario: Update price and payment status independently
        Given a pending booking exists with price 1500.00
        When the operator updates the price to 1800.00
        And the operator updates the payment status to "PartiallyPaid"
        Then the booking price should be 1800.00
        And the booking payment status should be "PartiallyPaid"
        And the booking status should be "Pending"
