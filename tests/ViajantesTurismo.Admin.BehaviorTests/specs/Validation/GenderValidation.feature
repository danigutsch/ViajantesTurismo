@BC:Admin
@Agg:Customer
@VO:PersonalInfo
@regression
Feature: Gender Validation

Gender is a required field that allows customers to specify their gender identity.
The system supports various gender options and custom values to respect diversity and inclusion.
Gender must be provided but can accommodate different expressions of gender identity.

    Rule: Gender is required for personal information

        @Invariant:INV-CUST-009
        Scenario: I attempt to create personal info without specifying gender
            When I attempt to create personal info without gender
            Then I should be informed that gender is required

        @Invariant:INV-CUST-009
        Scenario: I attempt to create personal info with only whitespace as gender
            When I attempt to create personal info with whitespace-only gender
            Then I should be informed that gender is required

    Rule: Gender must not exceed maximum length

        @Invariant:INV-CUST-010
        Scenario: I create personal info with gender at maximum allowed length
            When I create personal info with gender of 64 characters
            Then the personal info should be successfully created

        @Invariant:INV-CUST-010
        Scenario: I attempt to create personal info with gender exceeding maximum length
            When I attempt to create personal info with gender of 65 characters
            Then I should be informed that gender cannot exceed 64 characters

    Rule: System supports diverse gender identities

        Scenario: I create personal info with standard gender identity - Male
            When I create personal info with gender "Male"
            Then the personal info should be successfully created
            And the gender should be "Male"

        Scenario: I create personal info with standard gender identity - Female
            When I create personal info with gender "Female"
            Then the personal info should be successfully created
            And the gender should be "Female"

        Scenario: I create personal info with gender identity - Other
            When I create personal info with gender "Other"
            Then the personal info should be successfully created
            And the gender should be "Other"

        Scenario: I create personal info with gender identity - Non-binary
            When I create personal info with gender "Non-binary"
            Then the personal info should be successfully created
            And the gender should be "Non-binary"

        Scenario: I create personal info with custom gender expression
            When I create personal info with gender "Prefer not to say"
            Then the personal info should be successfully created
            And the gender should be "Prefer not to say"