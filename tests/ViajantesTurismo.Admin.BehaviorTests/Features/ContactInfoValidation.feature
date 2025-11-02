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
