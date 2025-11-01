Feature: Gender Validation
As a tourism system
I want to validate customer gender
So that only valid genders are accepted

    Scenario: Creating personal info with valid gender
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with empty gender
        Given I have personal information with gender ""
        When I create the personal info
        Then the creation should fail
        And the error should be "Gender is required."

    Scenario: Creating personal info with whitespace-only gender
        Given I have personal information with gender "   "
        When I create the personal info
        Then the creation should fail
        And the error should be "Gender is required."

    Scenario: Creating personal info with null gender
        Given I have personal information with null gender
        When I create the personal info
        Then the creation should fail
        And the error should be "Gender is required."

    Scenario: Creating personal info with Male gender
        Given I have personal information with gender "Male"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with Female gender
        Given I have personal information with gender "Female"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with Other gender
        Given I have personal information with gender "Other"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with non-binary gender
        Given I have personal information with gender "Non-binary"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with custom gender value
        Given I have personal information with gender "Prefer not to say"
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with gender at maximum length
        Given I have personal information with gender of 64 characters
        When I create the personal info
        Then the creation should succeed
        And the personal info should contain the provided data

    Scenario: Creating personal info with gender exceeding maximum length
        Given I have personal information with gender of 65 characters
        When I create the personal info
        Then the creation should fail
        And the error should be "Gender cannot exceed 64 characters."