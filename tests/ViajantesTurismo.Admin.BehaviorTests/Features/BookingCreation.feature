Feature: Booking Creation
As a domain model
I want to ensure Booking entity creation is properly validated
So that only valid bookings can be created

    Scenario: Create booking with valid data
        When I create a booking with base price 1000, room type "DoubleRoom", room cost 0, and regular bike 100 for principal
        Then the booking should be created successfully
        And the booking total price should be 1100

    Scenario: Create booking with principal and companion customers
        When I create a booking with base price 1000, room type "DoubleRoom", room cost 200, regular bike 100 for principal, and eBike 200 for companion
        Then the booking should be created successfully
        And the booking total price should be 1500

    Scenario: Create booking with single room supplement
        When I create a booking with base price 1000, room type "SingleRoom", room cost 0, and regular bike 100 for principal
        Then the booking should be created successfully
        And the booking total price should be 1100

    Scenario: Cannot create booking with zero base price
        When I try to create a booking with base price 0
        Then the booking creation should fail
        And the error should be for field "basePrice"

    Scenario: Cannot create booking with negative base price
        When I try to create a booking with base price -100
        Then the booking creation should fail
        And the error should be for field "basePrice"

    Scenario: Cannot create booking with base price exceeding maximum
        When I try to create a booking with base price 100001
        Then the booking creation should fail
        And the error should be for field "basePrice"

    Scenario: Cannot create booking with negative room cost
        When I try to create a booking with base price 1000 and room cost -100
        Then the booking creation should fail
        And the error should be for field "roomAdditionalCost"

    Scenario: Cannot create booking with room cost exceeding maximum
        When I try to create a booking with base price 1000 and room cost 100001
        Then the booking creation should fail
        And the error should be for field "roomAdditionalCost"

    Scenario: Cannot create booking with notes exceeding maximum length
        When I try to create a booking with notes of 2001 characters
        Then the booking creation should fail
        And the error should be for field "notes"

    Scenario: Create booking with notes at maximum length
        When I create a booking with notes of 2000 characters
        Then the booking should be created successfully

    Scenario: Create booking with SingleRoom room type
        When I create a booking with base price 1000, room type "SingleRoom", room cost 0, and regular bike 100 for principal
        Then the booking should be created successfully
        And the booking should have room type "SingleRoom"

    Scenario Outline: Cannot create booking with invalid RoomType values
        When I try to create a booking with invalid room type <invalidValue>
        Then the booking creation should fail
        And the error should be for field "roomType"
        And the error message should contain "Invalid room type"

        Examples:
          | invalidValue |
          | -1           |
          | 2            |
          | 99           |
          | 999          |