@BC:Admin
@Agg:Tour
@Entity:Payment
@regression
Feature: Payment Recording and Tracking

**Business Need:** Tour operators must accurately record all payments received from customers,
automatically update payment status, and prevent overpayments to maintain accurate financial records.

**Key Business Rules:**
- Payments are immutable financial records (can only be created, never modified)
- Payment status updates automatically based on amount paid vs total price
- Payment amount must be positive and cannot exceed remaining balance
- Payment date cannot be in the future
- Multiple payments can be recorded until booking is fully paid

**Related Invariants:**
- INV-TOUR-020: Payment amount cannot exceed remaining balance
- INV-TOUR-021: Payment date cannot be in the future

    Background:
        Given I am authenticated as a tour operator
        And a tour exists with a pending booking for payment tests

    Rule: Payments are recorded with complete audit trail

        @happy_path
        Scenario: Record payment with full details
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

        @happy_path
        Scenario: Record payment with only required fields
            When I record a payment with the following details:
              | Field       | Value      |
              | Amount      | 250.00     |
              | PaymentDate | 2025-01-20 |
              | Method      | Cash       |
            Then the payment should be recorded successfully
            And the payment reference number should be empty
            And the payment notes should be empty

        @happy_path
        Scenario: Accept all payment methods
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

    Rule: Payment status updates automatically based on amounts

        @happy_path
        Scenario: Auto-update from Unpaid to PartiallyPaid
            Given the booking payment status is "Unpaid"
            When I record a payment of 100.00 on 2025-01-15 using Cash
            Then the booking payment status should be "PartiallyPaid"

        @happy_path
        Scenario: Auto-update from PartiallyPaid to Paid
            Given I record a payment of 500.00 on 2025-01-15 using CreditCard
            And the booking payment status is "PartiallyPaid"
            When I record a payment of 500.00 on 2025-02-01 using BankTransfer
            Then the booking payment status should be "Paid"

        @happy_path
        Scenario: Track multiple payments until fully paid
            When I record a payment of 300.00 on 2025-01-15 using CreditCard
            And I record a payment of 400.00 on 2025-02-01 using BankTransfer
            And I record a payment of 300.00 on 2025-02-15 using Cash
            Then the booking payment status should be "Paid"
            And the amount paid should be 1000.00
            And the remaining balance should be 0.00

    Rule: Payment amount must be positive and within balance

        @Invariant:INV-TOUR-020
        @error_case
        Scenario: Reject payment exceeding remaining balance
            Given I record a payment of 600.00 on 2025-01-15 using CreditCard
            When I attempt to record a payment of 500.00 on 2025-02-01 using Cash
            Then the payment should be rejected with error "Payment amount $500.00 exceeds remaining balance $400.00"
            And the amount paid should be 600.00
            And the remaining balance should be 400.00

        @error_case
        Scenario: Reject zero payment amount
            When I attempt to record a payment with amount 0.00
            Then the payment should be rejected with error containing "greater than zero"

        @error_case
        Scenario: Reject negative payment amount
            When I attempt to record a payment with amount -50.00
            Then the payment should be rejected with error containing "greater than zero"

    Rule: Payment date must not be in the future

        @Invariant:INV-TOUR-021
        @error_case
        Scenario: Reject future payment date
            When I attempt to record a payment with a date in the future
            Then the payment should be rejected with error containing "future"

    Rule: Payment method must be valid

        @error_case
        Scenario: Reject invalid payment method
            When I attempt to record a payment with an invalid payment method
            Then the payment should be rejected with error containing "Invalid payment method"