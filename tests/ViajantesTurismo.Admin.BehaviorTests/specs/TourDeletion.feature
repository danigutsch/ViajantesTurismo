@BC:Admin @Agg:Tour @regression
Feature: Tour Deletion
  As a tour operator
  I want to delete tours under appropriate conditions
  So that I can maintain a clean tour catalog while protecting bookings

  **Business Need:** Tour operators need the ability to remove tours from the system,
  but must be prevented from deleting tours that have confirmed bookings to protect
  customer commitments and maintain data integrity.

  **Key Business Rules:**
  - Tours without bookings can be deleted freely
  - Tours with only pending bookings can be deleted (pending bookings are not confirmed)
  - Tours with confirmed bookings cannot be deleted (INV-TOUR-015)
  - Tours with cancelled or completed bookings may be archived (business decision)

  **Related Invariants:**
  - INV-TOUR-015: Cannot delete tour with confirmed bookings

Background:
  Given I am authenticated as a tour operator

Rule: Tours can only be deleted if they have no confirmed bookings

  @happy_path @ignore
  Scenario: Delete tour with no bookings
    Given a tour exists with no bookings
    When I delete the tour
    Then the tour should be deleted successfully

  @happy_path @ignore
  Scenario: Delete tour with only pending bookings
    Given a tour exists
    And the tour has a pending booking
    When I delete the tour
    Then the tour should be deleted successfully

  @Invariant:INV-TOUR-015 @error_case @critical @ignore
  Scenario: Reject deletion of tour with confirmed booking
    Given a tour exists
    And the tour has a confirmed booking
    When I attempt to delete the tour
    Then the deletion should fail
    And the error message should contain "Cannot delete tour with confirmed bookings"

  @Invariant:INV-TOUR-015 @error_case @ignore
  Scenario: Reject deletion of tour with multiple confirmed bookings
    Given a tour exists
    And the tour has 3 confirmed bookings
    When I attempt to delete the tour
    Then the deletion should fail
    And the error message should contain "Cannot delete tour with confirmed bookings"

  @Invariant:INV-TOUR-015 @error_case @ignore
  Scenario: Reject deletion of tour with mixed booking statuses including confirmed
    Given a tour exists
    And the tour has a pending booking
    And the tour has a confirmed booking
    And the tour has a cancelled booking
    When I attempt to delete the tour
    Then the deletion should fail
    And the error message should contain "Cannot delete tour with confirmed bookings"
