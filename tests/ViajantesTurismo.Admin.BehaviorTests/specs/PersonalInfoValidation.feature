Feature: Personal Information Validation
As a tourism system
I want to validate customer personal information
So that only valid customer data is accepted

    Scenario: Creating personal info with valid data
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with missing first name
        Given I have personal information with first name ""
        When I create the personal info
        Then the creation should fail
        And the error should be "First name is required."

    Scenario: Creating personal info with missing last name
        Given I have personal information with last name ""
        When I create the personal info
        Then the creation should fail
        And the error should be "Last name is required."

    Scenario: Creating personal info with empty gender
        Given I have personal information with gender ""
        When I create the personal info
        Then the creation should fail
        And the error should be "Gender is required."

    Scenario: Creating personal info with empty nationality
        Given I have personal information with nationality ""
        When I create the personal info
        Then the creation should fail
        And the error should be "Nationality is required."

    Scenario: Creating personal info with empty occupation
        Given I have personal information with occupation ""
        When I create the personal info
        Then the creation should fail
        And the error should be "Occupation is required."

    Scenario: Creating personal info with future birth date
        Given I have personal information with birth date in the future
        When I create the personal info
        Then the creation should fail
        And the error should be "Birth date cannot be in the future."