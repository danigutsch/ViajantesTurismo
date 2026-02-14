Feature: Tour Update Currency
As a tour operator
I want to update tour currency independently
So that I can change the pricing currency without modifying price values

    Scenario: Successfully update tour currency to USD
        Given a tour exists with currency "EUR"
        When I update the currency to "USD"
        Then the tour should have currency "USD"

    Scenario: Successfully update tour currency to EUR
        Given a tour exists with currency "USD"
        When I update the currency to "EUR"
        Then the tour should have currency "EUR"

    Scenario: Successfully update tour currency to BRL
        Given a tour exists with currency "USD"
        When I update the currency to "BRL"
        Then the tour should have currency "BRL"

    Scenario: Cannot update tour currency if bookings exist
        Given a tour exists with currency "EUR" and has 1 booking
        When I try to update the currency to "USD"
        Then the currency update should fail
        And the error message should contain "cannot be changed if bookings exist"
