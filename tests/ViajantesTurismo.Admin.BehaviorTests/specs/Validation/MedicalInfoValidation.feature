@BC:Admin
@Agg:Customer
@VO:MedicalInfo
@regression
Feature: Medical Info Validation

As a system administrator, I need to validate customer medical information to ensure
we have accurate health-related data for safety and accommodation purposes.

**Business Rules:**
- Medical information is optional (allergies and additional info can be null or empty)
- When provided, allergies cannot exceed 500 characters
- When provided, additional information cannot exceed 500 characters
- Whitespace-only values are treated as null
- All fields automatically trimmed and whitespace normalized

    Rule: Medical information fields are optional

        @happy_path
        @smoke
        Scenario: Medical info can be created with complete information
            When I create medical info with allergies "Peanuts" and additional info "Requires EpiPen"
            Then the medical info should be successfully created

        @edge_case
        Scenario: Medical info can be created with only allergies
            When I create medical info with only allergies "Shellfish"
            Then the medical info should be successfully created
            And the additional info should be empty

        @edge_case
        Scenario: Medical info can be created with only additional information
            When I create medical info with only additional info "No known allergies"
            Then the medical info should be successfully created
            And the allergies should be empty

        @happy_path
        Scenario: Medical info can be created with no information
            When I create medical info without any information
            Then the medical info should be successfully created
            And the allergies should be empty
            And the additional info should be empty

    Rule: Allergies field must not exceed maximum length when provided

        @Invariant:INV-CUST-030
        @error_case
        @critical
        Scenario: I cannot create medical info with excessively long allergies
            When I attempt to create medical info with allergies of 501 characters
            Then I should be informed that allergies cannot exceed 500 characters

        @Invariant:INV-CUST-030
        @happy_path
        Scenario: I can create medical info with allergies at maximum allowed length
            When I create medical info with allergies of 500 characters
            Then the medical info should be successfully created

    Rule: Additional information field must not exceed maximum length when provided

        @Invariant:INV-CUST-030
        @error_case
        @critical
        Scenario: I cannot create medical info with excessively long additional information
            When I attempt to create medical info with additional info of 501 characters
            Then I should be informed that additional information cannot exceed 500 characters

        @Invariant:INV-CUST-030
        @happy_path
        Scenario: I can create medical info with additional info at maximum allowed length
            When I create medical info with additional info of 500 characters
            Then the medical info should be successfully created

    Rule: Medical info fields are automatically sanitized

        @edge_case
        Scenario: Whitespace in medical info fields is automatically normalized
            When I create medical info with fields containing extra whitespace
            Then the medical info should be successfully created
            And all medical info fields should have normalized whitespace

    Rule: Multiple validation errors are reported together

        @Invariant:INV-CUST-030
        @error_case
        Scenario: I am informed when multiple fields exceed maximum length
            When I attempt to create medical info with both fields exceeding maximum length
            Then I should be informed that allergies cannot exceed 500 characters
            And I should be informed that additional information cannot exceed 500 characters