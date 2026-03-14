Feature: Payment Status Transitions
As a system administrator
I want payment status to automatically update based on amounts paid
So that I can quickly see booking payment progress

    Background:
        Given a tour exists
        And a tour exists with a pending booking for payment tests

    Scenario: New booking starts with Unpaid status
        Then the booking payment status should be "Unpaid"
        And the amount paid should be 0.00
        And the remaining balance should be 1000.00

    Scenario: Partial payment transitions to PartiallyPaid
        When I record a payment of 1.00 on 2025-01-15 using Cash
        Then the booking payment status should be "PartiallyPaid"

    Scenario: Half payment remains PartiallyPaid
        When I record a payment of 500.00 on 2025-01-15 using CreditCard
        Then the booking payment status should be "PartiallyPaid"

    Scenario: Almost full payment remains PartiallyPaid
        When I record a payment of 999.99 on 2025-01-15 using BankTransfer
        Then the booking payment status should be "PartiallyPaid"

    Scenario: Exact total payment transitions to Paid
        When I record a payment of 1000.00 on 2025-01-15 using CreditCard
        Then the booking payment status should be "Paid"
        And the remaining balance should be 0.00

    Scenario: Multiple payments reaching exactly total transitions to Paid
        When I record a payment of 250.00 on 2025-01-15 using CreditCard
        And I record a payment of 250.00 on 2025-01-16 using BankTransfer
        And I record a payment of 250.00 on 2025-01-17 using Cash
        And I record a payment of 250.00 on 2025-01-18 using Check
        Then the booking payment status should be "Paid"
        And the amount paid should be 1000.00
        And the remaining balance should be 0.00

    Scenario: Status updates on each incremental payment
        Given I record a payment of 100.00 on 2025-01-15 using Cash
        And the booking payment status is "PartiallyPaid"
        When I record a payment of 400.00 on 2025-01-16 using CreditCard
        Then the booking payment status should be "PartiallyPaid"
        When I record a payment of 500.00 on 2025-01-17 using BankTransfer
        Then the booking payment status should be "Paid"

    Scenario: Cannot record payment when already fully paid
        Given I record a payment of 1000.00 on 2025-01-15 using CreditCard
        And the booking payment status is "Paid"
        When I attempt to record a payment of 1.00 on 2025-01-16 using Cash
        Then the payment should be rejected with error "Payment amount $1.00 exceeds remaining balance $0.00"

    Scenario: Payment status never goes backwards
        Given I record a payment of 600.00 on 2025-01-15 using CreditCard
        And the booking payment status is "PartiallyPaid"
        When I record a payment of 400.00 on 2025-01-16 using Cash
        Then the booking payment status should be "Paid"

    Scenario: Single cent remains PartiallyPaid
        When I record a payment of 999.99 on 2025-01-15 using CreditCard
        Then the booking payment status should be "PartiallyPaid"
        And the remaining balance should be 0.01