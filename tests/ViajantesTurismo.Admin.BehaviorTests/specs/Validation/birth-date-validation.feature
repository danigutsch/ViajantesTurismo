@BC:Admin
@Agg:Customer
@Entity:PersonalInfo
@regression
Feature: Birth Date Validation
As a customer service representative
I want to validate customer birth dates
So that we maintain accurate customer records and prevent data entry errors

Birth dates are essential for customer identification and age verification.
The system must ensure that birth dates are logically valid and not set in the future.

Business Rules:
- Birth date cannot be in the future
- Birth date must be a valid date
- System accepts customers of any age (no minimum age requirement)

    Rule: Birth date cannot be in the future
    Birth dates must be in the past or today to prevent
    data entry errors and ensure logical consistency.

        @Invariant:INV-CUST-004
        @smoke
        @happy_path
        Scenario: Register customer with valid birth date
            Given I have valid personal information
            When I create the personal info
            Then the creation should succeed
            And the personal info should contain the provided data

        @Invariant:INV-CUST-004
        @error_case
        @critical
        Scenario: Birth date cannot be in the future
            When I attempt to create personal info with birth date in the future
            Then I should not be able to create the personal info
            And I should be informed that birth date cannot be in the future

        @Invariant:INV-CUST-004
        @error_case
        Scenario: Birth date one day in the future is rejected
            When I attempt to create personal info with birth date one day in the future
            Then I should not be able to create the personal info
            And I should be informed that birth date cannot be in the future

        @Invariant:INV-CUST-004
        @happy_path
        @edge_case
        Scenario: Birth date exactly 10 years ago is accepted
            When I create personal info with birth date 10 years ago
            Then the creation should succeed
            And the personal info should contain the provided data

        @Invariant:INV-CUST-004
        @happy_path
        @edge_case
        Scenario: Birth date 11 years ago is accepted
            When I create personal info with birth date 11 years ago
            Then the creation should succeed
            And the personal info should contain the provided data

    Rule: Birth dates must represent customers at least 10 years old
    The system requires customers to be at least 10 years old.
    Customers younger than 10 are not accepted.

        @Invariant:INV-CUST-004
        @happy_path
        Scenario: Young adult customer is accepted
            When I create personal info with birth date 25 years ago
            Then the creation should succeed
            And the personal info should contain the provided data

        @Invariant:INV-CUST-004
        @happy_path
        Scenario: Minor customer is accepted
            When I create personal info with birth date 10 years ago
            Then the creation should succeed
            And the personal info should contain the provided data

        @Invariant:INV-CUST-004
        @happy_path
        Scenario: Elderly customer is accepted
            When I create personal info with birth date 100 years ago
            Then the creation should succeed
            And the personal info should contain the provided data
