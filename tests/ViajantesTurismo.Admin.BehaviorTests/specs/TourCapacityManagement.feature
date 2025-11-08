Feature: Tour Capacity Management
As a tour administrator
I want to manage minimum and maximum customer capacity for tours
So that I can ensure tours have enough participants and don't exceed their limits

    Background:
        Given I have valid tour details

    Scenario: Create tour with valid capacity
        When I create a tour with minimum 4 and maximum 12 customers
        Then the tour should be created successfully
        And the minimum capacity should be 4
        And the maximum capacity should be 12

    Scenario: Reject tour creation when max is less than min
        When I try to create a tour with minimum 10 and maximum 5 customers
        Then the tour creation should fail
        And the error should indicate max must be at least min

    Scenario: Reject tour with minimum below 1
        When I try to create a tour with minimum 0 and maximum 10 customers
        Then the tour creation should fail
        And the error should indicate minimum must be at least 1

    Scenario: Reject tour with maximum above 20
        When I try to create a tour with minimum 5 and maximum 25 customers
        Then the tour creation should fail
        And the error should indicate maximum cannot exceed 20

    Scenario: Update tour capacity successfully
        Given a tour exists with minimum 4 and maximum 12 customers
        When I update the capacity to minimum 6 and maximum 15
        Then the capacity update should succeed
        And the minimum capacity should be 6
        And the maximum capacity should be 15

    Scenario: Prevent booking when tour is fully booked
        Given a tour exists with minimum 2 and maximum 3 customers
        And the tour has 2 confirmed bookings with 1 customer each
        And a third customer exists
        When I try to add a booking for the third customer
        Then the booking should be created successfully
        When I try to add a booking for a fourth customer
        Then the booking creation should fail
        And the error should indicate the tour is fully booked

    Scenario: Calculate available spots correctly
        Given a tour exists with minimum 4 and maximum 10 customers
        And the tour has 2 confirmed bookings with 1 customer each
        Then the current customer count should be 2
        And the available spots should be 8
        And the tour should not be at minimum capacity
        And the tour should not be fully booked

    Scenario: Calculate available spots with companion bookings
        Given a tour exists with minimum 4 and maximum 10 customers
        And the tour has 1 confirmed booking with 2 customers
        And the tour has 1 confirmed booking with 1 customer
        Then the current customer count should be 3
        And the available spots should be 7

    Scenario: Count only confirmed bookings toward capacity
        Given a tour exists with minimum 4 and maximum 10 customers
        And the tour has 2 confirmed bookings with 1 customer each
        And the tour has 1 pending booking with 1 customer
        And the tour has 1 cancelled booking with 1 customer
        Then the current customer count should be 2
        And the available spots should be 8
