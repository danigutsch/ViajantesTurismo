Feature: Booking Customer Entity
As a domain model
I want to ensure BookingCustomer entity operations work correctly
So that customer bike selections are properly validated

    Scenario: Create booking customer with regular bike
        When I create a booking customer with id 1, bike type "Regular", and bike price 100
        Then the booking customer should be created successfully
        And the booking customer should have bike type "Regular"
        And the booking customer should have bike price 100

    Scenario: Create booking customer with eBike
        When I create a booking customer with id 1, bike type "EBike", and bike price 200
        Then the booking customer should be created successfully
        And the booking customer should have bike type "EBike"
        And the booking customer should have bike price 200

    Scenario: Create booking customer with no bike
        When I create a booking customer with id 1, bike type "None", and bike price 0
        Then the booking customer should be created successfully
        And the booking customer should have bike type "None"
        And the booking customer should have bike price 0

    Scenario: Cannot create booking customer with negative bike price
        When I try to create a booking customer with bike price -50
        Then the booking customer creation should fail
        And the error should be for field "bikePrice"

    Scenario: Cannot create booking customer with bike price exceeding maximum
        When I try to create a booking customer with bike price 100001
        Then the booking customer creation should fail
        And the error should be for field "bikePrice"

    Scenario: Create booking customer with bike price at maximum
        When I create a booking customer with id 1, bike type "EBike", and bike price 100000
        Then the booking customer should be created successfully
        And the booking customer should have bike price 100000

    Scenario: Booking customer sanitizes bike price
        When I create a booking customer with id 1, bike type "Regular", and bike price 99.999
        Then the booking customer should be created successfully
        And the booking customer should have bike price 100