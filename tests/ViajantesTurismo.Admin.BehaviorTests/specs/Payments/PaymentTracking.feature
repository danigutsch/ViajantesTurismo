Feature: Payment Tracking
As a system administrator
I want to track all payments for a booking
So that I can manage payment history and reconcile accounts

    Background:
        Given a tour exists
        And a tour exists with a pending booking for payment tests

    Scenario: View payment history for booking with no payments
        Then the booking should have 0 payments
        And the payment history should be empty

    Scenario: View payment history after recording payments
        Given I record a payment of 300.00 on 2025-01-15 using CreditCard
        And I record a payment of 200.00 on 2025-02-01 using Cash
        Then the booking should have 2 payments
        And the first payment amount should be 300.00
        And the second payment amount should be 200.00
        And the payment history should be ordered by payment date

    Scenario: Payment includes recorded timestamp
        When I record a payment of 100.00 on 2025-01-15 using Cash
        Then the payment should have a recorded timestamp
        And the recorded timestamp should be recent

    Scenario: Retrieve payment by ID
        Given I record a payment of 500.00 on 2025-01-15 using CreditCard
        When I retrieve the payment by its ID
        Then the payment details should match the recorded payment

    Scenario: Multiple payments with different methods are tracked separately
        When I record a payment of 100.00 on 2025-01-15 using CreditCard
        And I record a payment of 150.00 on 2025-01-16 using BankTransfer
        And I record a payment of 200.00 on 2025-01-17 using Cash
        And I record a payment of 250.00 on 2025-01-18 using Check
        And I record a payment of 300.00 on 2025-01-19 using PayPal
        Then the booking should have 5 payments
        And each payment should have its distinct method

    Scenario: Payment with maximum length reference number
        When I record a payment with the following details:
          | Field           | Value                                                                                                                                                                                                                                                         |
          | Amount          | 100.00                                                                                                                                                                                                                                                        |
          | PaymentDate     | 2025-01-15                                                                                                                                                                                                                                                    |
          | Method          | CreditCard                                                                                                                                                                                                                                                    |
          | ReferenceNumber | REF1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890 |
        Then the payment should be recorded successfully

    Scenario: Payment with maximum length notes
        When I record a payment with the following details:
          | Field       | Value                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
          | Amount      | 100.00                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
          | PaymentDate | 2025-01-15                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
          | Method      | Cash                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
          | Notes       | This is a very long note that describes the payment in great detail including the customer's request for a receipt, the specific circumstances of the payment, any special instructions, and additional context that might be important for accounting purposes. The note can contain multiple sentences and should handle whitespace properly. It should also support various characters and formatting. This text is intentionally long to test the maximum length constraint and ensure that the system properly handles lengthy payment notes without truncation or data loss. Additional padding text to reach maximum length: Lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor incididunt ut labore et dolore magna aliqua Ut enim ad minim veniam quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur Excepteur sint occaecat cupidatat non proident sunt in culpa qui officia deserunt mollit anim id est laborum |
        Then the payment should be recorded successfully