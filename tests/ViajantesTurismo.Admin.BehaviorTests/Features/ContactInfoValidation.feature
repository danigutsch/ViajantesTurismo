Feature: Contact Info Validation
As a system administrator
I want contact information to be validated
So that we maintain valid customer contact details

    Scenario: Create contact info with valid data
        When I create contact info with email "customer@example.com", mobile "+1234567890", instagram "@user", facebook "user.name"
        Then the contact info should be created successfully

    Scenario: Create contact info with sanitized email
        When I create contact info with email "  customer@example.com  "
        Then the email should be "customer@example.com"

    Scenario: Create contact info with sanitized mobile
        When I create contact info with mobile "  +1234567890  "
        Then the mobile should be "+1234567890"

    Scenario: Create contact info with multiple spaces in instagram handle
        When I create contact info with instagram "  @user    handle  "
        Then the instagram should be "@user handle"

    Scenario: Create contact info with null instagram
        When I create contact info with instagram null
        Then the instagram should be null

    Scenario: Create contact info with null facebook
        When I create contact info with facebook null
        Then the facebook should be null

    Scenario: Create contact info with whitespace-only instagram becomes null
        When I create contact info with instagram "   "
        Then the instagram should be null

    Scenario: Create contact info with whitespace-only facebook becomes null
        When I create contact info with facebook "   "
        Then the facebook should be null

    Scenario: Email with multiple consecutive spaces is normalized
        When I create contact info with email "customer    @example.com"
        Then the email should be "customer @example.com"

    Scenario: Cannot create contact info with empty email
        When I create contact info with email ""
        Then the contact info creation should fail
        And the error should be "Email is required."

    Scenario: Cannot create contact info with null email
        When I create contact info with null email
        Then the contact info creation should fail
        And the error should be "Email is required."

    Scenario: Cannot create contact info with whitespace only email
        When I create contact info with email "   "
        Then the contact info creation should fail
        And the error should be "Email is required."

    Scenario: Cannot create contact info with email too long
        When I create contact info with email of 129 characters
        Then the contact info creation should fail
        And the error should be "Email cannot exceed 128 characters."

    Scenario: Create contact info with email at maximum length
        When I create contact info with email of 128 characters
        Then the contact info should be created successfully

    Scenario: Cannot create contact info with empty mobile
        When I create contact info with mobile ""
        Then the contact info creation should fail
        And the error should be "Mobile is required."

    Scenario: Cannot create contact info with null mobile
        When I create contact info with null mobile
        Then the contact info creation should fail
        And the error should be "Mobile is required."

    Scenario: Cannot create contact info with whitespace only mobile
        When I create contact info with mobile "   "
        Then the contact info creation should fail
        And the error should be "Mobile is required."

    Scenario: Cannot create contact info with mobile too long
        When I create contact info with mobile of 65 characters
        Then the contact info creation should fail
        And the error should be "Mobile cannot exceed 64 characters."

    Scenario: Create contact info with mobile at maximum length
        When I create contact info with mobile of 64 characters
        Then the contact info should be created successfully

    Scenario: Cannot create contact info with Instagram too long
        When I create contact info with Instagram of 65 characters
        Then the contact info creation should fail
        And the error should be "Instagram cannot exceed 64 characters."

    Scenario: Create contact info with Instagram at maximum length
        When I create contact info with Instagram of 64 characters
        Then the contact info should be created successfully

    Scenario: Cannot create contact info with Facebook too long
        When I create contact info with Facebook of 65 characters
        Then the contact info creation should fail
        And the error should be "Facebook cannot exceed 64 characters."

    Scenario: Create contact info with Facebook at maximum length
        When I create contact info with Facebook of 64 characters
        Then the contact info should be created successfully

    Scenario: Cannot create contact info with multiple validation errors
        When I create contact info with email "" and mobile ""
        Then the contact info creation should fail
        And the error should be "Email is required."
        And the error should be "Mobile is required."
