Feature: Customer Management
As a tour operator
I want to manage customer information
So that customer records are complete and accurate

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

    Scenario: Updating customer personal information
        Given I have an existing customer
        When I update the customer with new personal information
        Then the customer personal information should be updated

    Scenario: Updating customer identification information
        Given I have an existing customer
        When I update the customer with new identification information
        Then the customer identification information should be updated

    Scenario: Updating customer contact information
        Given I have an existing customer
        When I update the customer with new contact information
        Then the customer contact information should be updated

    Scenario: Updating customer address
        Given I have an existing customer
        When I update the customer with new address
        Then the customer address should be updated

    Scenario: Updating customer physical information
        Given I have an existing customer
        When I update the customer with new physical information
        Then the customer physical information should be updated

    Scenario: Updating customer accommodation preferences
        Given I have an existing customer
        When I update the customer with new accommodation preferences
        Then the customer accommodation preferences should be updated

    Scenario: Updating customer emergency contact
        Given I have an existing customer
        When I update the customer with new emergency contact
        Then the customer emergency contact should be updated

    Scenario: Updating customer medical information
        Given I have an existing customer
        When I update the customer with new medical information
        Then the customer medical information should be updated

    Scenario: Customer name with whitespace is trimmed
        Given I have personal information for sanitization with first name "  John  " and last name "  Doe  "
        When I create personal information from sanitization inputs
        Then the personal information should be created successfully from sanitization
        And the sanitized first name should be "John"
        And the sanitized last name should be "Doe"

    Scenario: Customer address with whitespace is trimmed
        Given I have address for sanitization with city "  New York  " and country "  USA  "
        When I create address information from sanitization inputs
        Then the sanitized address city should be "New York"
        And the sanitized address country should be "USA"
