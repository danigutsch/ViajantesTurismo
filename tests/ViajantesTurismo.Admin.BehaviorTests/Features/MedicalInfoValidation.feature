Feature: Medical Info Validation
As a system administrator
I want medical information to be validated
So that we maintain valid customer medical records

    Scenario: Create medical info with valid data
        When I create medical info with allergies "Peanuts" and additional info "Requires EpiPen"
        Then the medical info should be created successfully

    Scenario: Create medical info with null allergies
        When I create medical info with allergies null and additional info "No known allergies"
        Then the medical info should be created successfully
        And the allergies should be null

    Scenario: Create medical info with null additional info
        When I create medical info with allergies "Shellfish" and additional info null
        Then the medical info should be created successfully
        And the additional info should be null

    Scenario: Create medical info with both fields null
        When I create medical info with allergies null and additional info null
        Then the medical info should be created successfully
        And the allergies should be null
        And the additional info should be null

    Scenario: Create medical info with whitespace-only allergies becomes null
        When I create medical info with allergies "   " and additional info "None"
        Then the allergies should be null

    Scenario: Create medical info with whitespace-only additional info becomes null
        When I create medical info with allergies "None" and additional info "   "
        Then the additional info should be null

    Scenario: Allergies with multiple spaces is normalized
        When I create medical info with allergies "Peanuts,    Shellfish,    Dairy"
        Then the allergies should be "Peanuts, Shellfish, Dairy"

    Scenario: Additional info with multiple spaces is normalized
        When I create medical info with additional info "Requires    medication    daily"
        Then the additional info should be "Requires medication daily"

    Scenario: Cannot create medical info with allergies too long
        When I create medical info with allergies of 501 characters
        Then the medical info creation should fail
        And the error should be "Allergies cannot exceed 500 characters."

    Scenario: Create medical info with allergies at maximum length
        When I create medical info with allergies of 500 characters
        Then the medical info should be created successfully

    Scenario: Cannot create medical info with additional info too long
        When I create medical info with additional info of 501 characters
        Then the medical info creation should fail
        And the error should be "Additional information cannot exceed 500 characters."

    Scenario: Create medical info with additional info at maximum length
        When I create medical info with additional info of 500 characters
        Then the medical info should be created successfully

    Scenario: Cannot create medical info with both fields too long
        When I create medical info with allergies of 501 characters and additional info of 501 characters
        Then the medical info creation should fail
        And the error should be "Allergies cannot exceed 500 characters."
        And the error should be "Additional information cannot exceed 500 characters."