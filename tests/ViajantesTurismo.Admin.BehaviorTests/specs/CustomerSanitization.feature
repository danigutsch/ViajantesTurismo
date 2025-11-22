Feature: Customer Sanitization
As a tour operator
I want customer data to be automatically sanitized
So that data is consistent and normalized across the system

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
        Given I have contact info with email "john@example.com" and mobile "+123  456  7890"
        When I create contact information
        Then the sanitized email should be "john@example.com"
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