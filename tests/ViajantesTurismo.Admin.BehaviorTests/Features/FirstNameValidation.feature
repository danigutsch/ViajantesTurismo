Feature: First Name Validation
As a tourism system
I want to validate customer first name
So that only valid first names are accepted

    Scenario: Creating personal info with valid first name
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed

    Scenario: Creating personal info with missing first name
        Given I have personal information with first name ""
        When I create the personal info
        Then the creation should fail
        And the error should be "First name is required."
