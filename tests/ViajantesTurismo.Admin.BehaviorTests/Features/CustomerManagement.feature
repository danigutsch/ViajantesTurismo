Feature: Customer Management
As a tour operator
I want to manage customer information
So that customer records are complete and accurate

    Scenario: Creating a customer with complete valid information
        Given I have valid personal information
        And I have valid identification information
        And I have valid contact information
        And I have valid address information
        And I have valid physical information
        And I have valid accommodation preferences
        And I have valid emergency contact
        And I have valid medical information
        When I create a customer
        Then the customer should be created successfully
        And the customer should contain all the provided information

    Scenario: Updating customer personal information
        Given I have an existing customer
        When I update the customer with new personal information
        Then the customer personal information should be updated

    Scenario: Updating customer identification information
        Given I have an existing customer
        When I update the customer with new identification information
        Then the customer identification information should be updated

    Scenario: Updating customer contact information
        Given I have an existing customer
        When I update the customer with new contact information
        Then the customer contact information should be updated

    Scenario: Updating customer address
        Given I have an existing customer
        When I update the customer with new address
        Then the customer address should be updated

    Scenario: Updating customer physical information
        Given I have an existing customer
        When I update the customer with new physical information
        Then the customer physical information should be updated

    Scenario: Updating customer accommodation preferences
        Given I have an existing customer
        When I update the customer with new accommodation preferences
        Then the customer accommodation preferences should be updated

    Scenario: Updating customer emergency contact
        Given I have an existing customer
        When I update the customer with new emergency contact
        Then the customer emergency contact should be updated

    Scenario: Updating customer medical information
        Given I have an existing customer
        When I update the customer with new medical information
        Then the customer medical information should be updated

    Scenario: Customer name with whitespace is trimmed
        Given I have personal information for sanitization with first name "  John  " and last name "  Doe  "
        When I create personal information from sanitization inputs
        Then the personal information should be created successfully from sanitization
        And the sanitized first name should be "John"
        And the sanitized last name should be "Doe"

    Scenario: Customer address with whitespace is trimmed
        Given I have address for sanitization with city "  New York  " and country "  USA  "
        When I create address information from sanitization inputs
        Then the sanitized address city should be "New York"
        And the sanitized address country should be "USA"

    Scenario: Customer contact info with whitespace is trimmed
        Given I have contact info with email "  john@example.com  " and mobile "  +1234567890  "
        When I create contact information
        Then the sanitized email should be "john@example.com"
        And the sanitized mobile should be "+1234567890"

    Scenario: Customer contact info with Instagram and Facebook whitespace
        Given I have contact info with Instagram "  john_doe  " and Facebook "  johndoe123  "
        When I create contact information with social media
        Then the sanitized Instagram should be "john_doe"
        And the sanitized Facebook should be "johndoe123"

    Scenario: Customer contact info normalizes multiple spaces
        Given I have contact info with email "john  @  example.com" and mobile "+123  456  7890"
        When I create contact information
        Then the sanitized email should be "john @ example.com"
        And the sanitized mobile should be "+123 456 7890"

    Scenario: Customer identification info with whitespace is trimmed
        Given I have identification info with national ID "  ABC123456  " and nationality "  American  "
        When I create identification information
        Then the sanitized national ID should be "ABC123456"
        And the sanitized ID nationality should be "American"

    Scenario: Customer emergency contact with whitespace is trimmed
        Given I have emergency contact with name "  Jane Doe  " and mobile "  +9876543210  "
        When I create emergency contact information
        Then the sanitized emergency contact name should be "Jane Doe"
        And the sanitized emergency contact mobile should be "+9876543210"

    Scenario: Customer medical info with whitespace is trimmed
        Given I have medical info with allergies "  Peanuts, Dairy  " and additional info "  Requires insulin  "
        When I create medical information
        Then the sanitized allergies should be "Peanuts, Dairy"
        And the sanitized additional info should be "Requires insulin"

    Scenario: Customer medical info preserves multi-line formatting
        Given I have medical info with allergies "Peanuts\nDairy\nGluten" and additional info "Line 1\nLine 2"
        When I create medical information
        Then the sanitized allergies should be "Peanuts\nDairy\nGluten"
        And the sanitized additional info should be "Line 1\nLine 2"
