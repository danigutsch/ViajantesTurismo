Feature: Customer Update Methods
As a customer management system
I want to update customer information sections independently
So that customer data can be maintained accurately over time

    Scenario: Successfully update customer personal info
        Given a customer exists with personal info "John" "Doe"
        When I update the personal info to "Jane" "Smith"
        Then the customer personal info update should succeed
        And the customer should have first name "Jane"
        And the customer should have last name "Smith"

    Scenario: Successfully update customer identification info
        Given a customer exists with passport "AB123456"
        When I update the identification info to passport "CD789012"
        Then the customer identification info update should succeed
        And the customer should have passport "CD789012"

    Scenario: Successfully update customer contact info
        Given a customer exists with email "john@example.com"
        When I update the contact info to email "jane@example.com"
        Then the customer contact info update should succeed
        And the customer should have email "jane@example.com"

    Scenario: Successfully update customer address
        Given a customer exists with city "New York"
        When I update the address to city "Los Angeles"
        Then the customer address update should succeed
        And the customer should have city "Los Angeles"

    Scenario: Successfully update customer physical info
        Given a customer exists with height 170
        When I update the physical info to height 180
        Then the customer physical info update should succeed
        And the customer should have height 180

    Scenario: Successfully update customer accommodation preferences
        Given a customer exists with bed type "Single"
        When I update the accommodation preferences to bed type "Double"
        Then the customer accommodation preferences update should succeed
        And the customer should have bed type "Double"

    Scenario: Successfully update customer emergency contact
        Given a customer exists with emergency contact "Bob Doe"
        When I update the emergency contact to "Alice Smith"
        Then the customer emergency contact update should succeed
        And the customer should have emergency contact "Alice Smith"

    Scenario: Successfully update customer medical info
        Given a customer exists with allergies "Peanuts"
        When I update the medical info to allergies "Shellfish"
        Then the customer medical info update should succeed
        And the customer should have allergies "Shellfish"

    Scenario: Multiple updates to same customer
        Given a customer exists with personal info "John" "Doe"
        When I update the personal info to "Jane" "Smith"
        And I update the contact info to email "jane.smith@example.com"
        And I update the address to city "San Francisco"
        Then all customer updates should succeed
        And the customer should have first name "Jane"
        And the customer should have last name "Smith"
        And the customer should have email "jane.smith@example.com"
        And the customer should have city "San Francisco"