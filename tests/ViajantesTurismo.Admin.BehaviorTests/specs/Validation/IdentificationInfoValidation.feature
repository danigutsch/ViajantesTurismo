@BC:Admin
@Agg:Customer
@Entity:IdentificationInfo
@regression
Feature: Identification Info Validation

As a system administrator, I need to validate customer identification information to ensure
we can verify customer identity and comply with regulatory requirements.

**Business Rules:**
- National ID is required (government-issued identification number)
- ID issuing nationality is required (country that issued the ID)
- National ID: maximum 64 characters
- ID nationality: maximum 64 characters
- All fields automatically trimmed and whitespace normalized

    Rule: National ID is required and must not exceed maximum length

        @Invariant:INV-CUST-019
        @error_case
        @smoke
        Scenario: I cannot create identification info without a national ID
            When I attempt to create identification info without a national ID
            Then I should be informed that national ID is required

        @Invariant:INV-CUST-020
        @error_case
        @critical
        Scenario: I cannot create identification info with an excessively long national ID
            When I attempt to create identification info with a national ID of 65 characters
            Then I should be informed that national ID cannot exceed 64 characters

        @Invariant:INV-CUST-019
        @Invariant:INV-CUST-020
        @happy_path
        Scenario: I can create identification info with national ID at maximum allowed length
            When I create identification info with a national ID of 64 characters
            Then the identification info should be successfully created

    Rule: ID nationality is required and must not exceed maximum length

        @Invariant:INV-CUST-021
        @error_case
        @smoke
        Scenario: I cannot create identification info without an ID nationality
            When I attempt to create identification info without an ID nationality
            Then I should be informed that ID nationality is required

        @Invariant:INV-CUST-021
        @error_case
        @critical
        Scenario: I cannot create identification info with an excessively long ID nationality
            When I attempt to create identification info with an ID nationality of 65 characters
            Then I should be informed that ID nationality cannot exceed 64 characters

        @Invariant:INV-CUST-021
        @happy_path
        Scenario: I can create identification info with ID nationality at maximum allowed length
            When I create identification info with an ID nationality of 64 characters
            Then the identification info should be successfully created

    Rule: Identification fields are automatically sanitized

        @happy_path
        @smoke
        Scenario: Identification info with complete valid information is accepted
            When I create identification info with national ID "12345678" and ID nationality "Brazilian"
            Then the identification info should be successfully created

        @edge_case
        Scenario: Whitespace in identification fields is automatically normalized
            When I create identification info with fields containing extra whitespace
            Then the identification info should be successfully created
            And all identification fields should have normalized whitespace

    Rule: Multiple validation errors are reported together

        @Invariant:INV-CUST-019
        @Invariant:INV-CUST-021
        @error_case
        Scenario: I am informed of all missing required fields
            When I attempt to create identification info without any fields
            Then I should be informed that national ID is required
            And I should be informed that ID nationality is required

        @Invariant:INV-CUST-020
        @Invariant:INV-CUST-021
        @error_case
        Scenario: I am informed when multiple fields exceed maximum length
            When I attempt to create identification info with both fields exceeding maximum length
            Then I should be informed that national ID cannot exceed 64 characters
            And I should be informed that ID nationality cannot exceed 64 characters