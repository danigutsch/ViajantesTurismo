Feature: Profession Validation
As a tourism system
I want to validate customer profession
So that only valid professions are accepted

    Scenario: Creating personal info with valid profession
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed

    Scenario: Creating personal info with empty profession
        Given I have personal information with profession ""
        When I create the personal info
        Then the creation should fail
        And the error should be "Profession is required."
