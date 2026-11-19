@BC:Admin
@Agg:Tour
@regression
Feature: Tour retirement
As an Admin
I want to retire tours that have no active bookings
So that I can withdraw tours while preserving their booking history

    Background:
        Given I am authenticated as a tour operator
        And a tour exists with no bookings

    Rule: Pending and confirmed bookings require resolution before retirement

        @happy_path
        Scenario: Retire a tour without bookings
            When I retire the tour
            Then the stored tour should be retired

        @Invariant:INV-TOUR-015
        @error_case
        Scenario: Reject retirement with a pending booking
            Given the tour has a pending booking
            When I retire the tour
            Then retirement should conflict with the active booking

        @Invariant:INV-TOUR-015
        @error_case
        Scenario: Reject retirement with a confirmed booking
            Given the tour has a confirmed booking
            When I retire the tour
            Then retirement should conflict with the active booking

        @happy_path
        Scenario: Retire a tour with a cancelled booking
            Given the tour has a cancelled booking
            When I retire the tour
            Then the stored tour should be retired
            And the historical booking should be retained

        @happy_path
        Scenario: Retire a tour with a completed booking
            Given the tour has a completed booking
            When I retire the tour
            Then the stored tour should be retired
            And the historical booking should be retained
