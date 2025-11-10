@BC:Admin @Agg:Customer @regression
Feature: Customer Creation
  As a tour operator
  I want to create customers with valid data
  So that customer records are properly validated and unique

  **Business Need:** Tour operators must register customers with complete and valid information
  to ensure effective communication, proper tour management, and compliance with data requirements.
  Each customer must have a unique email address to prevent duplicate registrations.

  **Key Business Rules:**
  - Email addresses must be unique across all customers (INV-CUST-001)
  - All required personal, contact, and identification information must be valid
  - Customer data is validated according to entity-level constraints

  **Related Invariants:**
  - INV-CUST-001: Email addresses must be unique across all customers

Background:
  Given I am authenticated as a tour operator

Rule: Customer email addresses must be unique

  @Invariant:INV-CUST-001 @error_case @critical @ignore
  Scenario: Reject duplicate customer email
    Given a customer exists with email "john@example.com"
    When I attempt to create another customer with email "john@example.com"
    Then the customer creation should fail
    And the error message should contain "Email address already exists"

  @Invariant:INV-CUST-001 @happy_path @ignore
  Scenario: Create customer with unique email
    Given a customer exists with email "john@example.com"
    When I create a customer with email "jane@example.com"
    Then the customer should be created successfully

Rule: Complete customer information must be valid

  @happy_path @smoke
  Scenario: Creating a customer with complete valid information
    Given I have valid personal information
    And I have valid identification information
    And I have valid contact information
    And I have valid address information
    And I have valid physical information
    And I have valid accommodation preferences
    And I have valid emergency contact
    And I have valid medical information
    When I create a customer
    Then the customer should be created successfully
    And the customer should contain all the provided information
