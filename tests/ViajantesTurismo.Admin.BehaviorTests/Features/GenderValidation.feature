Feature: Gender Validation
As a tourism system
I want to validate customer gender
So that only valid genders are accepted

    Scenario: Creating personal info with valid gender
        Given I have valid personal information
        When I create the personal info
        Then the creation should succeed

    Scenario: Creating personal info with empty gender
        Given I have personal information with gender ""
        When I create the personal info
        Then the creation should fail
        And the error should be "Gender is required."
