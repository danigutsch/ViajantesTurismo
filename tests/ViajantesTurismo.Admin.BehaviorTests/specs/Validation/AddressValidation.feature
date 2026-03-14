@BC:Admin
@Agg:Customer
@VO:Address
@regression
Feature: Address Validation

As a system administrator, I need to validate customer addresses to ensure we can reliably
communicate with and locate customers when needed.

**Business Rules:**
- All address fields (street, neighborhood, postal code, city, state, country) are required
- Optional complement field for additional location details (apartment number, floor, etc.)
- Street, neighborhood, city, state, country: maximum 128 characters
- Postal code: maximum 64 characters
- Complement: maximum 128 characters
- All fields automatically trimmed and whitespace normalized

    Rule: Street address is required and must not exceed maximum length

        @Invariant:INV-CUST-022
        @error_case
        @smoke
        Scenario: I cannot create an address without a street
            When I attempt to create an address without a street
            Then I should be informed that street is required

        @Invariant:INV-CUST-022
        @error_case
        @critical
        Scenario: I cannot create an address with an excessively long street
            When I attempt to create an address with a street of 129 characters
            Then I should be informed that street cannot exceed 128 characters

        @Invariant:INV-CUST-022
        @happy_path
        Scenario: I can create an address with street at maximum allowed length
            When I create an address with a street of 128 characters
            Then the address should be successfully created

    Rule: Neighborhood is required and must not exceed maximum length

        @Invariant:INV-CUST-023
        @error_case
        @smoke
        Scenario: I cannot create an address without a neighborhood
            When I attempt to create an address without a neighborhood
            Then I should be informed that neighborhood is required

        @Invariant:INV-CUST-023
        @error_case
        @critical
        Scenario: I cannot create an address with an excessively long neighborhood
            When I attempt to create an address with a neighborhood of 129 characters
            Then I should be informed that neighborhood cannot exceed 128 characters

        @Invariant:INV-CUST-023
        @happy_path
        Scenario: I can create an address with neighborhood at maximum allowed length
            When I create an address with a neighborhood of 128 characters
            Then the address should be successfully created

    Rule: Postal code is required and must not exceed maximum length

        @Invariant:INV-CUST-024
        @error_case
        @smoke
        Scenario: I cannot create an address without a postal code
            When I attempt to create an address without a postal code
            Then I should be informed that postal code is required

        @Invariant:INV-CUST-024
        @error_case
        @critical
        Scenario: I cannot create an address with an excessively long postal code
            When I attempt to create an address with a postal code of 65 characters
            Then I should be informed that postal code cannot exceed 64 characters

        @Invariant:INV-CUST-024
        @happy_path
        Scenario: I can create an address with postal code at maximum allowed length
            When I create an address with a postal code of 64 characters
            Then the address should be successfully created

    Rule: City is required and must not exceed maximum length

        @Invariant:INV-CUST-025
        @error_case
        @smoke
        Scenario: I cannot create an address without a city
            When I attempt to create an address without a city
            Then I should be informed that city is required

        @Invariant:INV-CUST-025
        @error_case
        @critical
        Scenario: I cannot create an address with an excessively long city name
            When I attempt to create an address with a city of 129 characters
            Then I should be informed that city cannot exceed 128 characters

        @Invariant:INV-CUST-025
        @happy_path
        Scenario: I can create an address with city at maximum allowed length
            When I create an address with a city of 128 characters
            Then the address should be successfully created

    Rule: State is required and must not exceed maximum length

        @Invariant:INV-CUST-026
        @error_case
        @smoke
        Scenario: I cannot create an address without a state
            When I attempt to create an address without a state
            Then I should be informed that state is required

        @Invariant:INV-CUST-026
        @error_case
        @critical
        Scenario: I cannot create an address with an excessively long state name
            When I attempt to create an address with a state of 129 characters
            Then I should be informed that state cannot exceed 128 characters

        @Invariant:INV-CUST-026
        @happy_path
        Scenario: I can create an address with state at maximum allowed length
            When I create an address with a state of 128 characters
            Then the address should be successfully created

    Rule: Country is required and must not exceed maximum length

        @Invariant:INV-CUST-027
        @error_case
        @smoke
        Scenario: I cannot create an address without a country
            When I attempt to create an address without a country
            Then I should be informed that country is required

        @Invariant:INV-CUST-027
        @error_case
        @critical
        Scenario: I cannot create an address with an excessively long country name
            When I attempt to create an address with a country of 129 characters
            Then I should be informed that country cannot exceed 128 characters

        @Invariant:INV-CUST-027
        @happy_path
        Scenario: I can create an address with country at maximum allowed length
            When I create an address with a country of 128 characters
            Then the address should be successfully created

    Rule: Address complement is optional with maximum length constraint

        @happy_path
        Scenario: I can create an address with a valid complement
            When I create an address with complement "Apartment 5B"
            Then the address should be successfully created
            And the complement should be "Apartment 5B"

        @edge_case
        Scenario: Address complement can be omitted
            When I create an address without a complement
            Then the address should be successfully created
            And the complement should be empty

        @error_case
        Scenario: I cannot create an address with an excessively long complement
            When I attempt to create an address with a complement of 129 characters
            Then I should be informed that complement cannot exceed 128 characters

        @happy_path
        Scenario: I can create an address with complement at maximum allowed length
            When I create an address with a complement of 128 characters
            Then the address should be successfully created

    Rule: Address fields are automatically sanitized

        @happy_path
        Scenario: Address with complete valid information is accepted
            When I create an address with street "123 Main St", city "New York", state "NY", country "USA", postal code "10001", and neighborhood "Downtown"
            Then the address should be successfully created

        @edge_case
        Scenario: Whitespace in address fields is automatically normalized
            When I create an address with fields containing extra whitespace
            Then the address should be successfully created
            And all address fields should have normalized whitespace