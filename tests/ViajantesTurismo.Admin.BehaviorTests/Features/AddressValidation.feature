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
