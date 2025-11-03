Feature: Tour Included Services Validation
As a system administrator
I want tour included services to be validated
So that we maintain valid tour service information

    Background:
        Given a valid tour exists with the following details:
          | Field                     | Value      |
          | Identifier                | CUBA2025   |
          | Name                      | Cuba Tour  |
          | StartDate                 | 2025-01-15 |
          | EndDate                   | 2025-01-22 |
          | Price                     | 2500.00    |
          | DoubleRoomSupplementPrice | 500.00     |
          | RegularBikePrice          | 50.00      |
          | EBikePrice                | 75.00      |
          | Currency                  | USD        |

    Scenario: Update included services with valid list
        When I update the tour's included services with:
          | Service          |
          | Hotel            |
          | Breakfast        |
          | City Tour        |
          | Airport Transfer |
        Then the tour update should succeed
        And the tour should have 4 included services

    Scenario: Update included services with empty list
        When I update the tour's included services with an empty list
        Then the tour update should succeed
        And the tour should have 0 included services

    Scenario: Update included services with single item
        When I update the tour's included services with:
          | Service |
          | Hotel   |
        Then the tour update should succeed
        And the tour should have 1 included services

    Scenario: Update included services with whitespace is sanitized
        When I update the tour's included services with services containing extra whitespace
        Then the tour update should succeed
        And the services should be properly sanitized

    Scenario: Update included services with duplicate entries
        When I update the tour's included services with:
          | Service   |
          | Hotel     |
          | Breakfast |
          | Hotel     |
        Then the tour update should succeed
        And the tour should have 2 included services
        And the included services should contain "Hotel"
        And the included services should contain "Breakfast"

    Scenario: Update included services multiple times
        When I update the tour's included services with:
          | Service   |
          | Hotel     |
          | Breakfast |
        And I update the tour's included services with:
          | Service   |
          | Dinner    |
          | Transport |
        Then the tour update should succeed
        And the tour should have 2 included services
        And the included services should contain "Dinner"
        And the included services should contain "Transport"