Feature: Nationality Validation
As a tourism system
I want to validate customer nationality
So that only valid nationalities are accepted

    Scenario: Creating personal info with valid nationality
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed

    Scenario: Creating personal info with empty nationality
        Given I have personal information with nationality ""
        When I create the personal info
        Then the creation should fail
        And the error should be "Nationality is required."
