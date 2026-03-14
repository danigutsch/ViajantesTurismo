Feature: Tour Update Base Price
As a tour operator
I want to update the tour base price independently
So that I can adjust the main tour cost without changing other pricing

    Scenario: Successfully update tour base price
        Given a tour exists with base price 1000
        When I update the base price to 1200
        Then the tour base price update should succeed
        And the tour should have base price 1200

    Scenario: Cannot update tour with zero base price
        Given a tour exists with base price 1000
        When I update the base price to 0
        Then the tour base price update should fail
        And the error should contain "base price"

    Scenario: Cannot update tour with negative base price
        Given a tour exists with base price 1000
        When I update the base price to -500
        Then the tour base price update should fail
        And the error should contain "base price"

    Scenario: Cannot update tour with base price exceeding maximum
        Given a tour exists with base price 1000
        When I update the base price to 100001
        Then the tour base price update should fail
        And the error should contain "base price"

    Scenario: Successfully update tour base price to maximum allowed
        Given a tour exists with base price 1000
        When I update the base price to 100000
        Then the tour base price update should succeed
        And the tour should have base price 100000

    Scenario: Tour base price sanitizes decimal places
        Given a tour exists with base price 1000
        When I update the base price to 1499.999
        Then the tour base price update should succeed
        And the tour should have base price 1500.00

    Scenario: Successfully increase tour base price
        Given a tour exists with base price 1000
        When I update the base price to 1500
        Then the tour base price update should succeed
        And the tour should have base price 1500

    Scenario: Successfully decrease tour base price
        Given a tour exists with base price 1500
        When I update the base price to 1000
        Then the tour base price update should succeed
        And the tour should have base price 1000