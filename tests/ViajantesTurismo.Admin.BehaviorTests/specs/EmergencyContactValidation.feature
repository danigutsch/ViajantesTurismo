@BC:Admin @Agg:Customer @VO:EmergencyContact @regression
Feature: Emergency Contact Validation

As a system administrator, I need to validate emergency contact information to ensure
we can reach the appropriate person in case of an emergency involving the customer.

**Business Rules:**
- Emergency contact name is required (maximum 128 characters)
- Emergency contact mobile phone is required (maximum 64 characters)
- All fields automatically trimmed and whitespace normalized

Rule: Emergency contact name is required and must not exceed maximum length

    @Invariant:INV-CUST-028 @error_case @smoke
    Scenario: I cannot create an emergency contact without a name
        When I attempt to create an emergency contact without a name
        Then I should be informed that emergency contact name is required

    @Invariant:INV-CUST-028 @error_case @critical
    Scenario: I cannot create an emergency contact with an excessively long name
        When I attempt to create an emergency contact with a name of 129 characters
        Then I should be informed that emergency contact name cannot exceed 128 characters

    @Invariant:INV-CUST-028 @happy_path
    Scenario: I can create an emergency contact with name at maximum allowed length
        When I create an emergency contact with a name of 128 characters
        Then the emergency contact should be successfully created

Rule: Emergency contact mobile is required and must not exceed maximum length

    @Invariant:INV-CUST-029 @error_case @smoke
    Scenario: I cannot create an emergency contact without a mobile number
        When I attempt to create an emergency contact without a mobile
        Then I should be informed that emergency contact mobile is required

    @Invariant:INV-CUST-029 @error_case @critical
    Scenario: I cannot create an emergency contact with an excessively long mobile number
        When I attempt to create an emergency contact with a mobile of 65 characters
        Then I should be informed that emergency contact mobile cannot exceed 64 characters

    @Invariant:INV-CUST-029 @happy_path
    Scenario: I can create an emergency contact with mobile at maximum allowed length
        When I create an emergency contact with a mobile of 64 characters
        Then the emergency contact should be successfully created

Rule: Emergency contact fields are automatically sanitized

    @happy_path @smoke
    Scenario: Emergency contact with complete valid information is accepted
        When I create an emergency contact with name "Jane Doe" and mobile "+1234567890"
        Then the emergency contact should be successfully created

    @edge_case
    Scenario: Whitespace in emergency contact fields is automatically normalized
        When I create an emergency contact with fields containing extra whitespace
        Then the emergency contact should be successfully created
        And all emergency contact fields should have normalized whitespace

Rule: Multiple validation errors are reported together

    @Invariant:INV-CUST-028 @Invariant:INV-CUST-029 @error_case
    Scenario: I am informed of all missing required fields
        When I attempt to create an emergency contact without any fields
        Then I should be informed that emergency contact name is required
        And I should be informed that emergency contact mobile is required
