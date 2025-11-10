@BC:Admin @Agg:Customer @VO:PersonalInfo @regression
Feature: Profession Validation

Profession information helps understand customer backgrounds and is required for customer profiles.
The system accommodates various profession formats including simple titles, compound roles, and specialized positions.

    Rule: Profession is required for personal information
        @Invariant:INV-CUST-013
        Scenario: I attempt to create personal info without specifying profession
            When I attempt to create personal info without profession
            Then I should be informed that profession is required

        @Invariant:INV-CUST-013
        Scenario: I attempt to create personal info with only whitespace as profession
            When I attempt to create personal info with whitespace-only profession
            Then I should be informed that profession is required

    Rule: Profession must not exceed maximum length
        @Invariant:INV-CUST-014
        Scenario: I create personal info with profession at maximum allowed length
            When I create personal info with profession of 128 characters
            Then the personal info should be successfully created

        @Invariant:INV-CUST-014
        Scenario: I attempt to create personal info with profession exceeding maximum length
            When I attempt to create personal info with profession of 129 characters
            Then I should be informed that profession cannot exceed 128 characters

    Rule: System supports various profession formats
        Scenario: I create personal info with single-word profession title
            When I create personal info with profession "Engineer"
            Then the personal info should be successfully created
            And the profession should be "Engineer"

        Scenario: I create personal info with multi-word profession title
            When I create personal info with profession "Software Engineer"
            Then the personal info should be successfully created
            And the profession should be "Software Engineer"

        Scenario: I create personal info with profession containing special characters
            When I create personal info with profession "IT/Tech Specialist"
            Then the personal info should be successfully created
            And the profession should be "IT/Tech Specialist"

        Scenario: I create personal info with profession containing numbers
            When I create personal info with profession "Level 5 Manager"
            Then the personal info should be successfully created
            And the profession should be "Level 5 Manager"
