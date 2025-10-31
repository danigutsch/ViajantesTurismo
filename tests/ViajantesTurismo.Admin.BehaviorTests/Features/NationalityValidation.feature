Feature: Nationality Validation
As a tourism system
I want to validate customer nationality
So that only valid nationalities are accepted

    Scenario: Creating personal info with valid nationality
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with empty nationality
        Given I have personal information with nationality ""
        When I create the personal info
        Then the creation should fail
        And the error should be "Nationality is required."

    Scenario: Creating personal info with whitespace-only nationality
        Given I have personal information with nationality "   "
        When I create the personal info
        Then the creation should fail
        And the error should be "Nationality is required."

    Scenario: Creating personal info with null nationality
        Given I have personal information with null nationality
        When I create the personal info
        Then the creation should fail
        And the error should be "Nationality is required."

    Scenario: Creating personal info with single-word nationality
        Given I have personal information with nationality "Brazilian"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with hyphenated nationality
        Given I have personal information with nationality "British-American"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with nationality containing spaces
        Given I have personal information with nationality "South African"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with nationality containing accents
        Given I have personal information with nationality "Française"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with dual nationality
        Given I have personal information with nationality "Canadian/American"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data
