@BC:Admin
@Agg:Customer
@VO:PersonalInfo
@regression
Feature: Nationality Validation

Nationality identifies the customer's citizenship and is required for travel documentation and legal compliance.
The system accommodates various nationality formats including single nationalities, dual citizenship, and complex forms.

    Rule: Nationality is required for personal information

        @Invariant:INV-CUST-011
        Scenario: I attempt to create personal info without specifying nationality
            When I attempt to create personal info without nationality
            Then I should be informed that nationality is required

        @Invariant:INV-CUST-011
        Scenario: I attempt to create personal info with only whitespace as nationality
            When I attempt to create personal info with whitespace-only nationality
            Then I should be informed that nationality is required

    Rule: Nationality must not exceed maximum length

        @Invariant:INV-CUST-012
        Scenario: I create personal info with nationality at maximum allowed length
            When I create personal info with nationality of 128 characters
            Then the personal info should be successfully created

        @Invariant:INV-CUST-012
        Scenario: I attempt to create personal info with nationality exceeding maximum length
            When I attempt to create personal info with nationality of 129 characters
            Then I should be informed that nationality cannot exceed 128 characters

    Rule: System supports various nationality formats

        Scenario: I create personal info with single-word nationality
            When I create personal info with nationality "Brazilian"
            Then the personal info should be successfully created
            And the nationality should be "Brazilian"

        Scenario: I create personal info with hyphenated nationality
            When I create personal info with nationality "British-American"
            Then the personal info should be successfully created
            And the nationality should be "British-American"

        Scenario: I create personal info with multi-word nationality
            When I create personal info with nationality "South African"
            Then the personal info should be successfully created
            And the nationality should be "South African"

        Scenario: I create personal info with nationality containing accents
            When I create personal info with nationality "Française"
            Then the personal info should be successfully created
            And the nationality should be "Française"

        Scenario: I create personal info representing dual citizenship
            When I create personal info with nationality "Canadian/American"
            Then the personal info should be successfully created
            And the nationality should be "Canadian/American"