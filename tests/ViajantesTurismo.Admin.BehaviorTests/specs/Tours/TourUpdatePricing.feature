Feature: Tour Update Pricing
As a tour operator
I want to update tour pricing components
So that I can adjust supplement and bike rental prices

    Scenario: Successfully update tour pricing with valid values
        Given a tour exists with pricing setup
        When I update the pricing to single room supplement 600, regular bike 120, e-bike 220, and currency "USD"
        Then the tour pricing update should succeed
        And the tour should have single room supplement 600
        And the tour should have regular bike price 120
        And the tour should have e-bike price 220

    Scenario: Cannot update tour with zero single room supplement
        Given a tour exists with pricing setup
        When I update the pricing to single room supplement 0, regular bike 100, e-bike 200, and currency "USD"
        Then the tour pricing update should fail
        And the error should contain "single room"

    Scenario: Cannot update tour with negative single room supplement
        Given a tour exists with pricing setup
        When I update the pricing to single room supplement -100, regular bike 100, e-bike 200, and currency "USD"
        Then the tour pricing update should fail
        And the error should contain "single room"

    Scenario: Cannot update tour with zero regular bike price
        Given a tour exists with pricing setup
        When I update the pricing to single room supplement 500, regular bike 0, e-bike 200, and currency "USD"
        Then the tour pricing update should fail
        And the error should contain "regular bike"

    Scenario: Cannot update tour with negative regular bike price
        Given a tour exists with pricing setup
        When I update the pricing to single room supplement 500, regular bike -50, e-bike 200, and currency "USD"
        Then the tour pricing update should fail
        And the error should contain "regular bike"

    Scenario: Cannot update tour with zero e-bike price
        Given a tour exists with pricing setup
        When I update the pricing to single room supplement 500, regular bike 100, e-bike 0, and currency "USD"
        Then the tour pricing update should fail
        And the error should contain "e-bike"

    Scenario: Cannot update tour with negative e-bike price
        Given a tour exists with pricing setup
        When I update the pricing to single room supplement 500, regular bike 100, e-bike -75, and currency "USD"
        Then the tour pricing update should fail
        And the error should contain "e-bike"

    Scenario: Cannot update tour with single room supplement exceeding maximum
        Given a tour exists with pricing setup
        When I update the pricing to single room supplement 100001, regular bike 100, e-bike 200, and currency "USD"
        Then the tour pricing update should fail
        And the error should contain "single room"

    Scenario: Cannot update tour with regular bike price exceeding maximum
        Given a tour exists with pricing setup
        When I update the pricing to single room supplement 500, regular bike 100001, e-bike 200, and currency "USD"
        Then the tour pricing update should fail
        And the error should contain "regular bike"

    Scenario: Cannot update tour with e-bike price exceeding maximum
        Given a tour exists with pricing setup
        When I update the pricing to single room supplement 500, regular bike 100, e-bike 100001, and currency "USD"
        Then the tour pricing update should fail
        And the error should contain "e-bike"

    Scenario: Tour pricing sanitizes decimal places
        Given a tour exists with pricing setup
        When I update the pricing to single room supplement 599.999, regular bike 119.999, e-bike 219.999, and currency "USD"
        Then the tour pricing update should succeed
        And the tour should have single room supplement 600.00
        And the tour should have regular bike price 120.00
        And the tour should have e-bike price 220.00