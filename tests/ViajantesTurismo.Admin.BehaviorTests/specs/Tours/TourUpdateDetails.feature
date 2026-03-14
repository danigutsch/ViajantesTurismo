Feature: Tour Update Details
As a tour operator
I want to update tour identifier and name
So that I can keep tour information current and accurate

    Scenario: Successfully update tour details with valid values
        Given a tour exists with identifier "CUBA2024" and name "Cuba Bike Adventure"
        When I update the tour details to identifier "CUBA2025" and name "Cuba Cycling Experience"
        Then the tour details update should succeed
        And the tour should have identifier "CUBA2025"
        And the tour should have name "Cuba Cycling Experience"

    @Invariant:INV-TOUR-001
    Scenario: Cannot update tour to use another tour's identifier
        Given a tour exists with identifier "CUBA2024" and name "Cuba Bike Adventure"
        And another tour exists with identifier "CUBA2025"
        When I update the tour details to identifier "CUBA2025" and name "Cuba Bike Adventure"
        Then the tour details update should fail
        And the error should contain "identifier"
        And the error should contain "already exists"

    Scenario: Cannot update tour with empty identifier
        Given a tour exists with identifier "CUBA2024" and name "Cuba Bike Adventure"
        When I update the tour details to identifier "" and name "Valid Name"
        Then the tour details update should fail
        And the error should contain "identifier"

    Scenario: Cannot update tour with empty name
        Given a tour exists with identifier "CUBA2024" and name "Cuba Bike Adventure"
        When I update the tour details to identifier "VALID" and name ""
        Then the tour details update should fail
        And the error should contain "name"

    Scenario: Cannot update tour with identifier exceeding maximum length
        Given a tour exists with identifier "CUBA2024" and name "Cuba Bike Adventure"
        When I update the tour details to identifier with 129 characters and name "Valid Name"
        Then the tour details update should fail
        And the error should contain "identifier"

    Scenario: Cannot update tour with name exceeding maximum length
        Given a tour exists with identifier "CUBA2024" and name "Cuba Bike Adventure"
        When I update the tour details to identifier "VALID" and name with 129 characters
        Then the tour details update should fail
        And the error should contain "name"

    Scenario: Update tour details with whitespace trimming
        Given a tour exists with identifier "CUBA2024" and name "Cuba Bike Adventure"
        When I update the tour details to identifier "  CUBA2025  " and name "  Cuba Tour  "
        Then the tour details update should succeed
        And the tour should have identifier "CUBA2025"
        And the tour should have name "Cuba Tour"

    Scenario: Cannot update tour identifier if bookings exist
        Given a tour exists with identifier "CUBA2024" and has 1 booking
        When I try to update the tour details to identifier "CUBA2025" and name "New Name"
        Then the tour details update should fail
        And the error message should contain "cannot be changed if bookings exist"