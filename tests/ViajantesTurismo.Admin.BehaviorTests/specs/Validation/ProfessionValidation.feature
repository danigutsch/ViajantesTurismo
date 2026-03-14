@BC:Admin
@Agg:Customer
@VO:PersonalInfo
@regression
Feature: Occupation Validation

Occupation information helps understand customer backgrounds and is required for customer profiles.
The system accommodates various occupation formats including simple titles, compound roles, and specialized positions.

    Rule: Occupation is required for personal information

        @Invariant:INV-CUST-013
        Scenario: I attempt to create personal info without specifying occupation
            When I attempt to create personal info without occupation
            Then I should be informed that occupation is required

        @Invariant:INV-CUST-013
        Scenario: I attempt to create personal info with only whitespace as occupation
            When I attempt to create personal info with whitespace-only occupation
            Then I should be informed that occupation is required

    Rule: Occupation must not exceed maximum length

        @Invariant:INV-CUST-014
        Scenario: I create personal info with occupation at maximum allowed length
            When I create personal info with occupation of 128 characters
            Then the personal info should be successfully created

        @Invariant:INV-CUST-014
        Scenario: I attempt to create personal info with occupation exceeding maximum length
            When I attempt to create personal info with occupation of 129 characters
            Then I should be informed that occupation cannot exceed 128 characters

    Rule: System supports various occupation formats

        Scenario: I create personal info with single-word occupation title
            When I create personal info with occupation "Engineer"
            Then the personal info should be successfully created
            And the occupation should be "Engineer"

        Scenario: I create personal info with multi-word occupation title
            When I create personal info with occupation "Software Engineer"
            Then the personal info should be successfully created
            And the occupation should be "Software Engineer"

        Scenario: I create personal info with occupation containing special characters
            When I create personal info with occupation "IT/Tech Specialist"
            Then the personal info should be successfully created
            And the occupation should be "IT/Tech Specialist"

        Scenario: I create personal info with occupation containing numbers
            When I create personal info with occupation "Level 5 Manager"
            Then the personal info should be successfully created
            And the occupation should be "Level 5 Manager"