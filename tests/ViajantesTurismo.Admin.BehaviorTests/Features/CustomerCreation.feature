Feature: Customer Creation
As a tour operator
I want to create customers with valid data
So that customer records are properly validated

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