Feature: Birth Date Validation
As a tourism system
I want to validate customer birth date
So that only valid birth dates are accepted

    Scenario: Creating personal info with valid birth date
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed

    Scenario: Creating personal info with future birth date
        Given I have personal information with birth date in the future
        When I create the personal info
        Then the creation should fail
        And the error should be "Birth date cannot be in the future."
