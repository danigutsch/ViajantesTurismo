Feature: Personal Information Validation
As a tourism system
I want to validate customer personal information
So that only valid customer data is accepted

    Scenario: Creating personal info with valid data
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with missing first name
        Given I have personal information with first name ""
        When I create the personal info
        Then the creation should fail
        And the error should be "First name is required."
