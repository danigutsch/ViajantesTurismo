Feature: Tour Update Included Services
As a tour operator
I want to update tour included services
So that tour service offerings can be modified

    Scenario: Updating tour included services
        Given an existing tour with services "Hotel, Breakfast"
        When I update the services to "Hotel, Breakfast, Lunch, City Tour"
        Then the tour should include services "Hotel, Breakfast, Lunch, City Tour"