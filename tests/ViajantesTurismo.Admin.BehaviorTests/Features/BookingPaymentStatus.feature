Feature: Booking Payment Status Management
As a tour operator
I want to manage booking payment status independently from booking status
So that I can track payments separately from booking lifecycle

    Scenario: Update payment status to Paid
        Given a pending booking exists
        When the operator updates the payment status to "Paid"
        Then the booking payment status should be "Paid"
        And the booking status should be "Pending"

    Scenario: Update payment status to PartiallyPaid
        Given a confirmed booking exists
        When the operator updates the payment status to "PartiallyPaid"
        Then the booking payment status should be "PartiallyPaid"
        And the booking status should be "Confirmed"

    Scenario: Update payment status to Refunded
        Given a cancelled booking exists
        When the operator updates the payment status to "Refunded"
        Then the booking payment status should be "Refunded"
        And the booking status should be "Cancelled"

    Scenario: Payment status changes are independent of booking status
        Given a pending booking exists
        When the operator updates the payment status to "Paid"
        And the operator confirms the booking
        Then the booking status should be "Confirmed"
        And the booking payment status should be "Paid"
