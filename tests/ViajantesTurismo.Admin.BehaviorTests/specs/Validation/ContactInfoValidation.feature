@BC:Admin
@Agg:Customer
@Entity:ContactInfo
@regression
Feature: Contact Info Validation
As a customer service representative
I want to validate customer contact information
So that we can reliably communicate with customers

Contact information is critical for customer communication, booking confirmations,
and emergency notifications. All contact details must be validated to ensure we
can reach customers when needed.

Business Rules:
- Email is required and must be valid format (max 128 characters)
- Mobile phone is required and properly formatted (max 64 characters)
- Social media handles (Instagram, Facebook) are optional (max 64 characters each)
- Whitespace is trimmed from all fields
- Empty or whitespace-only optional fields become null

    Rule: Email address is required and must be valid
    Every customer must have a valid email address for booking confirmations
    and important notifications. Email must be in valid format and within
    reasonable length limits.

        @Invariant:INV-CUST-002
        @smoke
        @happy_path
        Scenario: Register customer with valid email
            When I create contact info with email "customer@example.com", mobile "+1234567890", instagram "@user", facebook "user.name"
            Then the contact info should be created successfully

        @Invariant:INV-CUST-002
        @error_case
        @critical
        Scenario: Email is required
            When I attempt to create contact info with null email
            Then I should not be able to create the contact info
            And I should be informed that email is required

        @Invariant:INV-CUST-002
        @error_case
        Scenario: Email cannot be empty
            When I attempt to create contact info with email ""
            Then I should not be able to create the contact info
            And I should be informed that email is required

        @Invariant:INV-CUST-002
        @error_case
        Scenario: Email cannot be whitespace only
            When I attempt to create contact info with email "   "
            Then I should not be able to create the contact info
            And I should be informed that email is required

        @Invariant:INV-CUST-003
        @error_case
        Scenario: Email cannot exceed maximum length
            When I attempt to create contact info with email of 129 characters
            Then I should not be able to create the contact info
            And I should be informed that email cannot exceed 128 characters

        @Invariant:INV-CUST-003
        @happy_path
        @edge_case
        Scenario: Email at maximum length is accepted
            When I create contact info with email of 128 characters
            Then the contact info should be created successfully

    Rule: Mobile phone is required and properly formatted
    Mobile phone is required for urgent communications and booking updates.
    Must be within reasonable length limits.

        @Invariant:INV-CUST-015
        @error_case
        @critical
        Scenario: Mobile is required
            When I attempt to create contact info with null mobile
            Then I should not be able to create the contact info
            And I should be informed that mobile is required

        @Invariant:INV-CUST-015
        @error_case
        Scenario: Mobile cannot be empty
            When I attempt to create contact info with mobile ""
            Then I should not be able to create the contact info
            And I should be informed that mobile is required

        @Invariant:INV-CUST-015
        @error_case
        Scenario: Mobile cannot be whitespace only
            When I attempt to create contact info with mobile "   "
            Then I should not be able to create the contact info
            And I should be informed that mobile is required

        @Invariant:INV-CUST-016
        @error_case
        Scenario: Mobile cannot exceed maximum length
            When I attempt to create contact info with mobile of 65 characters
            Then I should not be able to create the contact info
            And I should be informed that mobile cannot exceed 64 characters

        @Invariant:INV-CUST-016
        @happy_path
        @edge_case
        Scenario: Mobile at maximum length is accepted
            When I create contact info with mobile of 64 characters
            Then the contact info should be created successfully

    Rule: Social media handles are optional with length limits
    Instagram and Facebook handles are optional but if provided must be
    within length limits. Empty values are converted to null.

        @happy_path
        Scenario: Social media handles can be null
            When I create contact info with instagram null
            Then the contact info should be created successfully
            And the instagram should be null

        @happy_path
        Scenario: Facebook handle can be null
            When I create contact info with facebook null
            Then the contact info should be created successfully
            And the facebook should be null

        @happy_path
        Scenario: Whitespace-only Instagram becomes null
            When I create contact info with instagram "   "
            Then the contact info should be created successfully
            And the instagram should be null

        @happy_path
        Scenario: Whitespace-only Facebook becomes null
            When I create contact info with facebook "   "
            Then the contact info should be created successfully
            And the facebook should be null

        @error_case
        Scenario: Instagram cannot exceed maximum length
            When I attempt to create contact info with Instagram of 65 characters
            Then I should not be able to create the contact info
            And I should be informed that Instagram cannot exceed 64 characters

        @happy_path
        @edge_case
        Scenario: Instagram at maximum length is accepted
            When I create contact info with Instagram of 64 characters
            Then the contact info should be created successfully

        @error_case
        Scenario: Facebook cannot exceed maximum length
            When I attempt to create contact info with Facebook of 65 characters
            Then I should not be able to create the contact info
            And I should be informed that Facebook cannot exceed 64 characters

        @happy_path
        @edge_case
        Scenario: Facebook at maximum length is accepted
            When I create contact info with Facebook of 64 characters
            Then the contact info should be created successfully

    Rule: Contact information is sanitized and normalized
    All contact fields have whitespace trimmed and multiple consecutive
    spaces normalized to maintain data quality.

        @happy_path
        Scenario: Email with surrounding whitespace is trimmed
            When I create contact info with email "  customer@example.com  "
            Then the contact info should be created successfully
            And the email should be "customer@example.com"

        @happy_path
        Scenario: Mobile with surrounding whitespace is trimmed
            When I create contact info with mobile "  +1234567890  "
            Then the contact info should be created successfully
            And the mobile should be "+1234567890"

        @happy_path
        Scenario: Instagram with multiple spaces is normalized
            When I create contact info with instagram "  @user    handle  "
            Then the contact info should be created successfully
            And the instagram should be "@user handle"

        @error_case
        Scenario: Email with spaces after normalization is rejected
            When I create contact info with email "customer    @example.com"
            Then I should not be able to create the contact info
            And I should be informed that email must be in a valid format

    Rule: Multiple validation errors are reported
    If contact information has multiple issues, all validation
    errors should be reported to help fix all problems at once.

        @Invariant:INV-CUST-002
        @Invariant:INV-CUST-015
        @error_case
        Scenario: Multiple required fields missing
            When I attempt to create contact info with email "" and mobile ""
            Then I should not be able to create the contact info
            And I should be informed that email is required
            And I should be informed that mobile is required