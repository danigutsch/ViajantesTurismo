Feature: Booking Discounts
As a booking administrator
I want to apply discounts to bookings
So that I can offer promotions, early bird pricing, or negotiate custom rates

    Background:
        Given a tour exists with base price 2000, double room supplement 500, regular bike price 100, and e-bike price 200

    Scenario: Create booking without discount
        When I create a booking with principal customer 1, regular bike, single room, and no discount
        Then the booking should be created successfully
        And the booking total price should be 2100

    Scenario: Create booking with percentage discount
        When I create a booking with principal customer 1, regular bike, single room, and 10% discount
        Then the booking should be created successfully
        And the booking total price should be 1890

    Scenario: Create booking with absolute discount
        When I create a booking with principal customer 1, regular bike, double room, and 150 absolute discount
        Then the booking should be created successfully
        And the booking total price should be 2450

    Scenario: Create booking with discount reason
        When I create a booking with principal customer 1, regular bike, single room, 15% discount, and reason "Early bird special"
        Then the booking should be created successfully
        And the booking should have discount reason "Early bird special"

    Scenario: Create double room booking with companion and percentage discount
        When I create a booking with principal customer 1 regular bike, companion customer 2 e-bike, double room, and 20% discount
        Then the booking should be created successfully
        And the booking total price should be 2240

    Scenario: Reject booking with percentage discount exceeding 100%
        When I create a booking with principal customer 1, regular bike, single room, and 101% discount
        Then the booking should fail with error containing "cannot exceed 100%"

    Scenario: Reject booking with negative discount amount
        When I create a booking with principal customer 1, regular bike, single room, and -10% discount
        Then the booking should fail with error containing "cannot be negative"

    Scenario: Reject booking with absolute discount exceeding subtotal
        When I create a booking with principal customer 1, regular bike, single room, and 3000 absolute discount
        Then the booking should fail with error containing "cannot exceed subtotal"

    Scenario: Reject booking with discount resulting in zero price
        When I create a booking with principal customer 1, regular bike, single room, and 100% discount
        Then the booking should fail with error containing "Final price after discount must be greater than zero"

    Scenario: Reject booking with absolute discount equal to subtotal
        When I create a booking with principal customer 1, regular bike, single room, and 2100 absolute discount
        Then the booking should fail with error containing "Final price after discount must be greater than zero"

    Scenario: Accept booking with maximum valid percentage discount
        When I create a booking with principal customer 1, regular bike, single room, and 99.9% discount
        Then the booking should be created successfully
        And the booking total price should be approximately 2.1

    Scenario: Accept booking with zero discount
        When I create a booking with principal customer 1, regular bike, single room, and 0% discount
        Then the booking should be created successfully
        And the booking total price should be 2100

    Scenario Outline: Various discount scenarios
        When I create a booking with base price <base>, room cost <room>, principal bike <bike1>, companion bike <bike2>, and <discount>% discount
        Then the booking should be created successfully
        And the booking total price should be <total>

        Examples:
          | base | room | bike1 | bike2 | discount | total |
          | 2000 | 0    | 100   | 0     | 10       | 1890  |
          | 2000 | 0    | 100   | 0     | 50       | 1050  |
          | 2000 | 500  | 100   | 200   | 15       | 2380  |
          | 2000 | 500  | 100   | 200   | 25       | 2100  |
          | 2000 | 0    | 100   | 0     | 5        | 1995  |