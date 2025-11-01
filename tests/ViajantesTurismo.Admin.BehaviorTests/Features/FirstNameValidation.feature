Feature: First Name Validation
As a tourism system
I want to validate customer first name
So that only valid first names are accepted

    Scenario: Creating personal info with valid first name
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with missing first name
        Given I have personal information with first name ""
        When I create the personal info
        Then the creation should fail
        And the error should be "First name is required."

    Scenario: Creating personal info with whitespace-only first name
        Given I have personal information with first name "   "
        When I create the personal info
        Then the creation should fail
        And the error should be "First name is required."

    Scenario: Creating personal info with null first name
        Given I have personal information with null first name
        When I create the personal info
        Then the creation should fail
        And the error should be "First name is required."

    Scenario: Creating personal info with single-word first name
        Given I have personal information with first name "Maria"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with hyphenated first name
        Given I have personal information with first name "Mary-Ann"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with compound first name
        Given I have personal information with first name "Jean Pierre"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with first name containing apostrophe
        Given I have personal information with first name "D'Angelo"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with first name containing accents
        Given I have personal information with first name "José"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data
