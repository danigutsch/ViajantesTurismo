Feature: Payment Recording
As a system administrator
I want to record payments for bookings
So that I can track payment status and balance

    Background:
        Given a tour exists
        And a tour exists with a pending booking for payment tests

    Scenario: Record a valid payment
        When I record a payment with the following details:
          | Field           | Value           |
          | Amount          | 300.00          |
          | PaymentDate     | 2025-01-15      |
          | Method          | CreditCard      |
          | ReferenceNumber | REF-123         |
          | Notes           | Initial deposit |
        Then the payment should be recorded successfully
        And the booking payment status should be "PartiallyPaid"
        And the amount paid should be 300.00
        And the remaining balance should be 700.00

    Scenario: Record multiple payments until fully paid
        When I record a payment of 300.00 on 2025-01-15 using CreditCard
        And I record a payment of 400.00 on 2025-02-01 using BankTransfer
        And I record a payment of 300.00 on 2025-02-15 using Cash
        Then the booking payment status should be "Paid"
        And the amount paid should be 1000.00
        And the remaining balance should be 0.00

    Scenario: Reject payment exceeding remaining balance
        Given I record a payment of 600.00 on 2025-01-15 using CreditCard
        When I attempt to record a payment of 500.00 on 2025-02-01 using Cash
        Then the payment should be rejected with error "Payment amount $500.00 exceeds remaining balance $400.00"
        And the amount paid should be 600.00
        And the remaining balance should be 400.00

    Scenario: Reject payment with invalid amount
        When I attempt to record a payment with amount 0.00
        Then the payment should be rejected with error containing "greater than zero"

    Scenario: Reject payment with negative amount
        When I attempt to record a payment with amount -50.00
        Then the payment should be rejected with error containing "greater than zero"

    Scenario: Reject payment with future date
        When I attempt to record a payment with a date in the future
        Then the payment should be rejected with error containing "future"

    Scenario: Reject payment with invalid payment method
        When I attempt to record a payment with an invalid payment method
        Then the payment should be rejected with error containing "Invalid payment method"

    Scenario: Payment auto-updates status from Unpaid to PartiallyPaid
        Given the booking payment status is "Unpaid"
        When I record a payment of 100.00 on 2025-01-15 using Cash
        Then the booking payment status should be "PartiallyPaid"

    Scenario: Payment auto-updates status from PartiallyPaid to Paid
        Given I record a payment of 500.00 on 2025-01-15 using CreditCard
        And the booking payment status is "PartiallyPaid"
        When I record a payment of 500.00 on 2025-02-01 using BankTransfer
        Then the booking payment status should be "Paid"

    Scenario: Record payment with optional fields omitted
        When I record a payment with the following details:
          | Field       | Value      |
          | Amount      | 250.00     |
          | PaymentDate | 2025-01-20 |
          | Method      | Cash       |
        Then the payment should be recorded successfully
        And the payment reference number should be empty
        And the payment notes should be empty

    Scenario: Record payment with all payment methods
        When I record payments using each payment method:
          | Method       | Amount |
          | Other        | 100.00 |
          | CreditCard   | 100.00 |
          | BankTransfer | 100.00 |
          | Cash         | 100.00 |
          | Check        | 100.00 |
          | PayPal       | 100.00 |
        Then all payments should be recorded successfully
        And the amount paid should be 600.00