Feature: Last Name Validation
As a tourism system
I want to validate customer last name
So that only valid last names are accepted

    Scenario: Creating personal info with valid last name
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed

    Scenario: Creating personal info with missing last name
        Given I have personal information with last name ""
        When I create the personal info
        Then the creation should fail
        And the error should be "Last name is required."
