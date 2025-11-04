Feature: Payment Edge Cases
As a system administrator
I want payment validation to handle edge cases properly
So that data integrity is maintained

    Background:
        Given a tour exists
        And a tour exists with a pending booking for payment tests

    Scenario: Record payment with past date
        When I record a payment of 100.00 on 2020-01-01 using Cash
        Then the payment should be recorded successfully

    Scenario: Very small valid payment amount
        When I record a payment of 0.01 on 2025-01-15 using Cash
        Then the payment should be recorded successfully
        And the amount paid should be 0.01
        And the remaining balance should be 999.99

    Scenario: Payment amount with many decimal places gets rounded
        When I record a payment of 123.456789 on 2025-01-15 using CreditCard
        Then the payment should be recorded successfully
        And the payment amount should be sanitized to valid precision

    Scenario: Exact remaining balance payment
        Given I record a payment of 300.00 on 2025-01-15 using CreditCard
        When I record a payment of 700.00 on 2025-01-16 using BankTransfer
        Then the payment should be recorded successfully
        And the booking payment status should be "Paid"
        And the remaining balance should be 0.00

    Scenario: Payment one cent below total
        When I record a payment of 999.99 on 2025-01-15 using CreditCard
        Then the payment should be recorded successfully
        And the booking payment status should be "PartiallyPaid"

    Scenario: Payment one cent over remaining balance
        Given I record a payment of 500.00 on 2025-01-15 using CreditCard
        When I attempt to record a payment of 500.01 on 2025-01-16 using Cash
        Then the payment should be rejected with error "Payment amount $500.01 exceeds remaining balance $500.00"

    Scenario: Cannot record zero payment even with zero balance
        Given I record a payment of 1000.00 on 2025-01-15 using CreditCard
        When I attempt to record a payment with amount 0.00
        Then the payment should be rejected with error containing "greater than zero"

    Scenario: Payment with special characters in reference number
        When I record a payment with the following details:
          | Field           | Value           |
          | Amount          | 100.00          |
          | PaymentDate     | 2025-01-15      |
          | Method          | CreditCard      |
          | ReferenceNumber | TXN-#123/456_78 |
        Then the payment should be recorded successfully

    Scenario: Payment with special characters in notes
        When I record a payment with the following details:
          | Field       | Value                   |
          | Amount      | 100.00                  |
          | PaymentDate | 2025-01-15              |
          | Method      | Cash                    |
          | Notes       | Payment received! @user |
        Then the payment should be recorded successfully

    Scenario: Payment method enum boundary value
        When I attempt to record a payment with an invalid payment method
        Then the payment should be rejected with error containing "Invalid payment method"