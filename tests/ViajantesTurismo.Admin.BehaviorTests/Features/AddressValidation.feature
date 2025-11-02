Feature: Address Validation
As a system administrator
I want address information to be validated
So that we maintain valid customer addresses

    Scenario: Create address with valid data
        When I create address with street "123 Main St", city "New York", state "NY", country "USA", postal code "10001"
        Then the address should be created successfully

    Scenario: Create address with sanitized street
        When I create address with street "  123 Main St  "
        Then the street should be "123 Main St"

    Scenario: Create address with sanitized city
        When I create address with city "  New York  "
        Then the city should be "New York"

    Scenario: Create address with sanitized state
        When I create address with state "  NY  "
        Then the state should be "NY"

    Scenario: Create address with sanitized country
        When I create address with country "  USA  "
        Then the country should be "USA"

    Scenario: Create address with sanitized postal code
        When I create address with postal code "  10001  "
        Then the postal code should be "10001"

    Scenario: Create address with null complement
        When I create address with complement null
        Then the complement should be null

    Scenario: Create address with whitespace-only complement becomes null
        When I create address with complement "   "
        Then the complement should be null

    Scenario: Create address with valid complement
        When I create address with complement "Apt 5B"
        Then the complement should be "Apt 5B"

    Scenario: Street with multiple spaces is normalized
        When I create address with street "123    Main    St"
        Then the street should be "123 Main St"

    Scenario: Neighborhood with multiple spaces is normalized
        When I create address with neighborhood "  Downtown    Area  "
        Then the neighborhood should be "Downtown Area"

    Scenario: All fields trimmed and normalized
        When I create address with all fields having extra whitespace
        Then all address fields should be properly trimmed and normalized

    Scenario: Cannot create address with empty street
        When I create address with street "" and city "New York" and state "NY" and country "USA" and postal code "10001"
        Then the address creation should fail
        And the error should be "Street is required."

    Scenario: Cannot create address with null street
        When I create address with null street and city "New York" and state "NY" and country "USA" and postal code "10001"
        Then the address creation should fail
        And the error should be "Street is required."

    Scenario: Cannot create address with whitespace only street
        When I create address with street "   " and city "New York" and state "NY" and country "USA" and postal code "10001"
        Then the address creation should fail
        And the error should be "Street is required."

    Scenario: Cannot create address with street too long
        When I create address with street of 129 characters
        Then the address creation should fail
        And the error should be "Street cannot exceed 128 characters."

    Scenario: Create address with street at maximum length
        When I create address with street of 128 characters
        Then the address should be created successfully

    Scenario: Cannot create address with empty neighborhood
        When I create address with street "123 Main St" and neighborhood "" and postal code "10001" and city "New York" and state "NY" and country "USA"
        Then the address creation should fail
        And the error should be "Neighborhood is required."

    Scenario: Cannot create address with empty postal code
        When I create address with street "123 Main St" and neighborhood "Downtown" and postal code "" and city "New York" and state "NY" and country "USA"
        Then the address creation should fail
        And the error should be "Postal code is required."

    Scenario: Cannot create address with empty city
        When I create address with street "123 Main St" and neighborhood "Downtown" and postal code "10001" and city "" and state "NY" and country "USA"
        Then the address creation should fail
        And the error should be "City is required."

    Scenario: Cannot create address with empty state
        When I create address with street "123 Main St" and neighborhood "Downtown" and postal code "10001" and city "New York" and state "" and country "USA"
        Then the address creation should fail
        And the error should be "State is required."

    Scenario: Cannot create address with empty country
        When I create address with street "123 Main St" and neighborhood "Downtown" and postal code "10001" and city "New York" and state "NY" and country ""
        Then the address creation should fail
        And the error should be "Country is required."

    Scenario: Cannot create address with complement too long
        When I create address with complement of 129 characters
        Then the address creation should fail
        And the error should be "Complement cannot exceed 128 characters."

    Scenario: Create address with complement at maximum length
        When I create address with complement of 128 characters
        Then the address should be created successfully

    Scenario: Cannot create address with neighborhood too long
        When I create address with neighborhood of 129 characters
        Then the address creation should fail
        And the error should be "Neighborhood cannot exceed 128 characters."

    Scenario: Create address with neighborhood at maximum length
        When I create address with neighborhood of 128 characters
        Then the address should be created successfully

    Scenario: Cannot create address with postal code too long
        When I create address with postal code of 65 characters
        Then the address creation should fail
        And the error should be "Postal code cannot exceed 64 characters."

    Scenario: Create address with postal code at maximum length
        When I create address with postal code of 64 characters
        Then the address should be created successfully

    Scenario: Cannot create address with city too long
        When I create address with city of 129 characters
        Then the address creation should fail
        And the error should be "City cannot exceed 128 characters."

    Scenario: Create address with city at maximum length
        When I create address with city of 128 characters
        Then the address should be created successfully

    Scenario: Cannot create address with state too long
        When I create address with state of 129 characters
        Then the address creation should fail
        And the error should be "State cannot exceed 128 characters."

    Scenario: Create address with state at maximum length
        When I create address with state of 128 characters
        Then the address should be created successfully

    Scenario: Cannot create address with country too long
        When I create address with country of 129 characters
        Then the address creation should fail
        And the error should be "Country cannot exceed 128 characters."

    Scenario: Create address with country at maximum length
        When I create address with country of 128 characters
        Then the address should be created successfully

    Scenario: Cannot create address with multiple validation errors
        When I create address with street "" and city "" and state "" and country "" and postal code "" and neighborhood ""
        Then the address creation should fail
        And the error should be "Street is required."
        And the error should be "Neighborhood is required."
        And the error should be "Postal code is required."
        And the error should be "City is required."
        And the error should be "State is required."
        And the error should be "Country is required."