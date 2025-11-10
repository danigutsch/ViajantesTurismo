@BC:Admin @Agg:Tour @regression
Feature: Booking Status Transitions and Payment Management

  **Business Need:** Tour operators must manage booking lifecycle (Pending → Confirmed → Completed/Cancelled)
  independently from payment status to handle real-world scenarios where payments arrive before/after confirmation.

  **Key Business Rules:**
  - Booking status follows state machine: Pending → Confirmed → Completed OR Cancelled
  - Payment status is independent and can be updated at any stage
  - Cancelled and Completed bookings cannot be modified (INV-TOUR-018)
  - Only Pending bookings can be removed (INV-TOUR-019)

  **Related Invariants:**
  - INV-TOUR-018: Bookings cannot be modified if status is Cancelled or Completed
  - INV-TOUR-019: Bookings can only be removed if status is Pending

Background:
  Given I am authenticated as a tour operator

Rule: Booking status and payment status are independent

  @Invariant:INV-TOUR-018 @happy_path
  Scenario: Confirm booking before payment is received
    Given a pending booking exists
    When the operator confirms the booking
    And the operator updates the payment status to "Paid"
    Then the booking status should be "Confirmed"
    And the booking payment status should be "Paid"

  @Invariant:INV-TOUR-018 @happy_path
  Scenario: Receive payment before confirming booking
    Given a pending booking exists
    When the operator updates the payment status to "Paid"
    And the operator confirms the booking
    Then the booking status should be "Confirmed"
    And the booking payment status should be "Paid"

  @happy_path
  Scenario: Track partial payment on confirmed booking
    Given a confirmed booking exists
    When the operator updates the payment status to "PartiallyPaid"
    Then the booking status should be "Confirmed"
    And the booking payment status should be "PartiallyPaid"

Rule: Completed tours may have unpaid bookings

  @happy_path
  Scenario: Complete tour despite unpaid booking
    Given a confirmed booking exists
    When the operator completes the booking
    Then the booking status should be "Completed"
    And the booking payment status should be "Unpaid"

Rule: Cancelled bookings track refund status

  @happy_path
  Scenario: Cancel booking and process refund
    Given a confirmed booking exists
    When the operator cancels the booking
    And the operator updates the payment status to "Refunded"
    Then the booking status should be "Cancelled"
    And the booking payment status should be "Refunded"
