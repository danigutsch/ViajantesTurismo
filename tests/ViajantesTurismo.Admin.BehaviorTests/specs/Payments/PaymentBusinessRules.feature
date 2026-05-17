Feature: Payment Business Rules
As a system administrator
I want payments to follow business rules
So that payment integrity is maintained

    Background:
        Given a tour exists
        And a tour exists with a pending booking for payment tests

    Scenario: Can record payment for confirmed booking
        Given the booking is confirmed
        When I record a payment of 100.00 on 2025-01-15 using Cash
        Then the payment should be recorded successfully

    Scenario: Can record payment for pending booking
        Given the booking is pending
        When I record a payment of 100.00 on 2025-01-15 using Cash
        Then the payment should be recorded successfully

    Scenario: Payment dates can be out of chronological order
        When I record a payment of 100.00 on 2025-03-15 using Cash
        And I record a payment of 200.00 on 2025-01-15 using CreditCard
        And I record a payment of 150.00 on 2025-02-15 using BankTransfer
        Then the booking should have 3 payments
        And the payments should maintain their recording order

    Scenario: Multiple payments on same date with different methods
        When I record a payment of 100.00 on 2025-01-15 using Cash
        And I record a payment of 200.00 on 2025-01-15 using CreditCard
        And I record a payment of 300.00 on 2025-01-15 using BankTransfer
        Then the booking should have 3 payments
        And all payments should have the same payment date

    Scenario: Payment notes can contain line breaks
        When I record a payment with the following details:
          | Field       | Value                               |
          | Amount      | 100.00                              |
          | PaymentDate | 2025-01-15                          |
          | Method      | Cash                                |
          | Notes       | First line\nSecond line\nThird line |
        Then the payment should be recorded successfully

    Scenario: Payment for booking with discount applied
        Given the booking has a 10% discount applied
        And the booking total price is 900.00
        When I record a payment of 900.00 on 2025-01-15 using CreditCard
        Then the booking payment status should be "Paid"
        And the remaining balance should be 0.00

    Scenario: Multiple partial payments with different dates
        When I record a payment of 100.00 on 2025-01-01 using Cash
        And I record a payment of 150.00 on 2025-02-01 using CreditCard
        And I record a payment of 200.00 on 2025-03-01 using BankTransfer
        And I record a payment of 250.00 on 2025-04-01 using Check
        And I record a payment of 300.00 on 2025-05-01 using PayPal
        Then the booking should have 5 payments
        And the amount paid should be 1000.00
        And the booking payment status should be "Paid"
