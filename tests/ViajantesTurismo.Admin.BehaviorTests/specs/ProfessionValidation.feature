Feature: Profession Validation
As a tourism system
I want to validate customer profession
So that only valid professions are accepted

    Scenario: Creating personal info with valid profession
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with empty profession
        Given I have personal information with profession ""
        When I create the personal info
        Then the creation should fail
        And the error should be "Profession is required."

    Scenario: Creating personal info with whitespace-only profession
        Given I have personal information with profession "   "
        When I create the personal info
        Then the creation should fail
        And the error should be "Profession is required."

    Scenario: Creating personal info with null profession
        Given I have personal information with null profession
        When I create the personal info
        Then the creation should fail
        And the error should be "Profession is required."

    Scenario: Creating personal info with valid single-word profession
        Given I have personal information with profession "Engineer"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with valid multi-word profession
        Given I have personal information with profession "Software Engineer"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with profession containing special characters
        Given I have personal information with profession "IT/Tech Specialist"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with profession containing numbers
        Given I have personal information with profession "Level 5 Manager"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with profession at maximum length
        Given I have personal information with profession of 128 characters
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with profession exceeding maximum length
        Given I have personal information with profession of 129 characters
        When I create the personal info
        Then the creation should fail
        And the error should be "Profession cannot exceed 128 characters."