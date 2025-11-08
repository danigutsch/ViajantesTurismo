Feature: Tour Sanitization
As a tour operator
I want tour data to be automatically sanitized
So that data is consistent and normalized across the system

    Scenario: Tour identifier with whitespace is trimmed
        Given I have tour details with identifier "  CUBA2024  " and name "Cuba Tour"
        When I try to create the tour
        Then the tour should be created successfully
        And the tour identifier should be "CUBA2024"

    Scenario: Tour name with multiple spaces is normalized
        Given I have tour details with identifier "TEST2024" and name "Cuba    Adventure    Tour"
        When I try to create the tour
        Then the tour should be created successfully
        And the tour name should be "Cuba Adventure Tour"

    Scenario: Tour price is rounded to two decimals
        Given I have tour details with base price 1999.999
        When I try to create the tour
        Then the tour should be created successfully
        And the tour base price should be 2000.00

    Scenario: Tour included services removes duplicates
        Given I have tour details with services "Hotel, Breakfast, Hotel, Lunch, breakfast"
        When I try to create the tour
        Then the tour should be created successfully
        And the tour should include services "Hotel, Breakfast, Lunch"

    Scenario: Tour included services removes empty strings and null values
        Given I have tour details with services "Hotel, , Breakfast,  , Lunch"
        When I try to create the tour
        Then the tour should be created successfully
        And the tour should include services "Hotel, Breakfast, Lunch"

    Scenario: Tour included services trims whitespace from each item
        Given I have tour details with services "  Hotel  , Breakfast , Lunch  "
        When I try to create the tour
        Then the tour should be created successfully
        And the tour should include services "Hotel, Breakfast, Lunch"

    Scenario: All tour prices rounded to two decimals
        Given I have tour details with base price 1999.996, single room 599.995, regular bike 149.994, e-bike 249.997
        When I try to create the tour
        Then the tour should be created successfully
        And the tour base price should be 2000.00
        And the tour single room supplement should be 600.00
        And the tour regular bike price should be 149.99
        And the tour e-bike price should be 250.00

    Scenario: Tour prices with edge case rounding (midpoint)
        Given I have tour details with base price 1999.995
        When I try to create the tour
        Then the tour should be created successfully
        And the tour base price should be 2000.00

    Scenario: Updating tour services removes duplicates and sanitizes
        Given an existing tour with services "Hotel, Breakfast"
        When I update the services to "  Hotel  , hotel, Breakfast, lunch, LUNCH, "
        Then the tour should include services "Hotel, Breakfast, lunch"