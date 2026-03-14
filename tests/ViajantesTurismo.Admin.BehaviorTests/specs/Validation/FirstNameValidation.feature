@BC:Admin
@Agg:Customer
@Entity:PersonalInfo
@regression
Feature: First Name Validation
As a customer service representative
I want to validate customer first names
So that we maintain accurate and complete customer records

First names are essential for customer identification and communication.
Names must accommodate diverse cultural naming conventions while ensuring
data completeness and quality.

Business Rules:
- First name is required (cannot be empty or whitespace)
- Maximum length is 128 characters
- Names can contain letters, hyphens, apostrophes, spaces, and accented characters
- Names are automatically trimmed and normalized

    Rule: First name is required and cannot be empty
    Every customer must have a first name for identification.
    Empty or whitespace-only values are not accepted.

        @Invariant:INV-CUST-005
        @smoke
        @happy_path
        Scenario: Register customer with valid first name
            Given I have valid personal information
            When I create the personal info
            Then the creation should succeed
            And the personal info should contain the provided data

        @Invariant:INV-CUST-005
        @error_case
        @critical
        Scenario: First name is required
            When I attempt to create personal info with null first name
            Then I should not be able to create the personal info
            And I should be informed that first name is required

        @Invariant:INV-CUST-005
        @error_case
        Scenario: First name cannot be empty
            When I attempt to create personal info with first name ""
            Then I should not be able to create the personal info
            And I should be informed that first name is required

        @Invariant:INV-CUST-005
        @error_case
        Scenario: First name cannot be whitespace only
            When I attempt to create personal info with first name "   "
            Then I should not be able to create the personal info
            And I should be informed that first name is required

    Rule: First name must be within length limits
    Names must be reasonable length to ensure compatibility with
    systems and prevent data quality issues.

        @Invariant:INV-CUST-006
        @error_case
        Scenario: First name cannot exceed maximum length
            When I attempt to create personal info with first name of 129 characters
            Then I should not be able to create the personal info
            And I should be informed that first name cannot exceed 128 characters

        @Invariant:INV-CUST-006
        @happy_path
        @edge_case
        Scenario: First name at maximum length is accepted
            When I create personal info with first name of 128 characters
            Then the creation should succeed
            And the personal info should contain the provided data

    Rule: First name supports diverse naming conventions
    Names must accommodate various cultural and linguistic patterns
    including compound names, hyphens, apostrophes, and accented characters.

        @Invariant:INV-CUST-005
        @happy_path
        Scenario: Single-word first name is accepted
            When I create personal info with first name "Maria"
            Then the creation should succeed
            And the personal info should contain the provided data

        @Invariant:INV-CUST-005
        @happy_path
        Scenario: Hyphenated first name is accepted
            When I create personal info with first name "Mary-Ann"
            Then the creation should succeed
            And the personal info should contain the provided data

        @Invariant:INV-CUST-005
        @happy_path
        Scenario: Compound first name is accepted
            When I create personal info with first name "Jean Pierre"
            Then the creation should succeed
            And the personal info should contain the provided data

        @Invariant:INV-CUST-005
        @happy_path
        Scenario: First name with apostrophe is accepted
            When I create personal info with first name "D'Angelo"
            Then the creation should succeed
            And the personal info should contain the provided data

        @Invariant:INV-CUST-005
        @happy_path
        Scenario: First name with accents is accepted
            When I create personal info with first name "José"
            Then the creation should succeed
            And the personal info should contain the provided data