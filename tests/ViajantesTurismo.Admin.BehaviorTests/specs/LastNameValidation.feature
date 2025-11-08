Feature: Last Name Validation
As a tourism system
I want to validate customer last name
So that only valid last names are accepted

    Scenario: Creating personal info with valid last name
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with missing last name
        Given I have personal information with last name ""
        When I create the personal info
        Then the creation should fail
        And the error should be "Last name is required."

    Scenario: Creating personal info with whitespace-only last name
        Given I have personal information with last name "   "
        When I create the personal info
        Then the creation should fail
        And the error should be "Last name is required."

    Scenario: Creating personal info with null last name
        Given I have personal information with null last name
        When I create the personal info
        Then the creation should fail
        And the error should be "Last name is required."

    Scenario: Creating personal info with single-word last name
        Given I have personal information with last name "Johnson"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with hyphenated last name
        Given I have personal information with last name "Smith-Jones"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with compound last name
        Given I have personal information with last name "Van der Berg"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with last name containing apostrophe
        Given I have personal information with last name "O'Connor"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with last name containing accents
        Given I have personal information with last name "García"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with last name at maximum length
        Given I have personal information with last name of 128 characters
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with last name exceeding maximum length
        Given I have personal information with last name of 129 characters
        When I create the personal info
        Then the creation should fail
        And the error should be "Last name cannot exceed 128 characters."