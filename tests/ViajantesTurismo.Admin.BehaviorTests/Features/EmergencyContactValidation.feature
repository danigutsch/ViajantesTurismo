Feature: Emergency Contact Validation
As a system administrator
I want emergency contact information to be validated
So that we maintain valid emergency contact details

    Scenario: Create emergency contact with valid data
        When I create emergency contact with name "Jane Doe" and mobile "+1234567890"
        Then the emergency contact should be created successfully

    Scenario: Create emergency contact with sanitized name
        When I create emergency contact with name "  Jane Doe  "
        Then the name should be "Jane Doe"

    Scenario: Create emergency contact with sanitized mobile
        When I create emergency contact with mobile "  +1234567890  "
        Then the mobile should be "+1234567890"

    Scenario: Name with multiple consecutive spaces is normalized
        When I create emergency contact with name "Jane    Doe"
        Then the name should be "Jane Doe"

    Scenario: Mobile with multiple consecutive spaces is normalized
        When I create emergency contact with mobile "+1234    567890"
        Then the mobile should be "+1234 567890"

    Scenario: Cannot create emergency contact with empty name
        When I create emergency contact with name "" and mobile "+1234567890"
        Then the emergency contact creation should fail
        And the error should be "Emergency contact name is required."

    Scenario: Cannot create emergency contact with null name
        When I create emergency contact with null name and mobile "+1234567890"
        Then the emergency contact creation should fail
        And the error should be "Emergency contact name is required."

    Scenario: Cannot create emergency contact with whitespace only name
        When I create emergency contact with name "   " and mobile "+1234567890"
        Then the emergency contact creation should fail
        And the error should be "Emergency contact name is required."

    Scenario: Cannot create emergency contact with name too long
        When I create emergency contact with name of 129 characters
        Then the emergency contact creation should fail
        And the error should be "Emergency contact name cannot exceed 128 characters."

    Scenario: Create emergency contact with name at maximum length
        When I create emergency contact with name of 128 characters
        Then the emergency contact should be created successfully

    Scenario: Cannot create emergency contact with empty mobile
        When I create emergency contact with name "Jane Doe" and mobile ""
        Then the emergency contact creation should fail
        And the error should be "Emergency contact mobile is required."

    Scenario: Cannot create emergency contact with null mobile
        When I create emergency contact with name "Jane Doe" and null mobile
        Then the emergency contact creation should fail
        And the error should be "Emergency contact mobile is required."

    Scenario: Cannot create emergency contact with whitespace only mobile
        When I create emergency contact with name "Jane Doe" and mobile "   "
        Then the emergency contact creation should fail
        And the error should be "Emergency contact mobile is required."

    Scenario: Cannot create emergency contact with mobile too long
        When I create emergency contact with mobile of 65 characters
        Then the emergency contact creation should fail
        And the error should be "Emergency contact mobile cannot exceed 64 characters."

    Scenario: Create emergency contact with mobile at maximum length
        When I create emergency contact with mobile of 64 characters
        Then the emergency contact should be created successfully

    Scenario: Cannot create emergency contact with multiple validation errors
        When I create emergency contact with name "" and mobile ""
        Then the emergency contact creation should fail
        And the error should be "Emergency contact name is required."
        And the error should be "Emergency contact mobile is required."