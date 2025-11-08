Feature: Companion Bookings
As a tour operator
I want to create bookings with companion customers
So that couples and friends can book together with appropriate pricing

    Scenario: Create booking with companion using regular bikes
        Given a tour exists
        And a principal customer exists
        And a companion customer exists
        When I add a booking with principal customer 1 on regular bike and companion customer 2 on regular bike in double room
        Then the booking should have a companion customer
        And the booking should include principal bike price
        And the booking should include companion bike price

    Scenario: Create booking with companion using different bike types
        Given a tour exists
        And a principal customer exists
        And a companion customer exists
        When I add a booking with principal customer 1 on regular bike and companion customer 2 on e-bike in double room
        Then the booking should have a companion customer
        And the booking should include principal regular bike price
        And the booking should include companion e-bike price

    Scenario: Create booking with companion both using e-bikes
        Given a tour exists
        And a principal customer exists
        And a companion customer exists
        When I add a booking with principal customer 1 on e-bike and companion customer 2 on e-bike in double room
        Then the booking should have a companion customer
        And both customers should have e-bike pricing

    Scenario: Create booking without companion in single room
        Given a tour exists
        And a principal customer exists
        When I add a booking with principal customer 1 on regular bike without companion in single room
        Then the booking should not have a companion customer
        And the booking should include single room supplement

    Scenario: Cannot create booking with companion when no bike type provided
        Given a tour exists
        And a principal customer exists
        And a companion customer exists
        When I add a booking with principal customer 1 on regular bike and companion customer 2 with no bike type in double room
        Then the booking creation should fail
        And the error should be for field "companionBikeType"
        And the error message should contain "Bike type must be selected"

    Scenario: Cannot create booking when principal has no bike type
        Given a tour exists
        And a principal customer exists
        When I add a booking with principal customer 1 with no bike type without companion in single room
        Then the booking creation should fail
        And the error should be for field "principalBikeType"
        And the error message should contain "Bike type must be selected"

    Scenario: Double room with companion has no single room supplement
        Given a tour exists
        And a principal customer exists
        And a companion customer exists
        When I add a booking with principal customer 1 on regular bike and companion customer 2 on regular bike in double room
        Then the booking should not include single room supplement

    Scenario: Single room without companion includes supplement
        Given a tour exists
        And a principal customer exists
        When I add a booking with principal customer 1 on regular bike without companion in single room
        Then the booking should include single room supplement