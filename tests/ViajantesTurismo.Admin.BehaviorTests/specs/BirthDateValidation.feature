Feature: Birth Date Validation
As a tourism system
I want to validate customer birth date
So that only valid birth dates are accepted

    Scenario: Creating personal info with valid birth date
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with future birth date
        Given I have personal information with birth date in the future
        When I create the personal info
        Then the creation should fail
        And the error should be "Birth date cannot be in the future."

    Scenario: Creating personal info with birth date today
        Given I have personal information with birth date today
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with birth date one day in the future
        Given I have personal information with birth date one day in the future
        When I create the personal info
        Then the creation should fail
        And the error should be "Birth date cannot be in the future."

    Scenario: Creating personal info with birth date one day in the past
        Given I have personal information with birth date one day in the past
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with very old birth date
        Given I have personal information with birth date 100 years ago
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with birth date for an adult
        Given I have personal information with birth date 25 years ago
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with birth date for a minor
        Given I have personal information with birth date 10 years ago
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data
