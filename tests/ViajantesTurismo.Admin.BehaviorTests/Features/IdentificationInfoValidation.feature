Feature: Identification Info Validation
As a system administrator
I want identification information to be validated
So that we maintain valid customer identification records

    Scenario: Create identification info with valid data
        When I create identification info with national ID "12345678" and ID nationality "Brazilian"
        Then the identification info should be created successfully

    Scenario: National ID with multiple spaces is normalized
        When I create identification info with national ID "123   456   78" and ID nationality "Brazilian"
        Then the national ID should be "123 456 78"

    Scenario: ID nationality with multiple spaces is normalized
        When I create identification info with national ID "12345678" and ID nationality "United    States"
        Then the ID nationality should be "United States"

    Scenario: All fields with extra whitespace are sanitized
        When I create identification info with national ID "  12345678  " and ID nationality "  Brazilian  "
        Then the national ID should be "12345678"
        And the ID nationality should be "Brazilian"

    Scenario: Cannot create identification info with empty national ID
        When I create identification info with national ID "" and ID nationality "Brazilian"
        Then the identification info creation should fail
        And the error should be "National ID is required."

    Scenario: Cannot create identification info with null national ID
        When I create identification info with null national ID and ID nationality "Brazilian"
        Then the identification info creation should fail
        And the error should be "National ID is required."

    Scenario: Cannot create identification info with whitespace only national ID
        When I create identification info with national ID "   " and ID nationality "Brazilian"
        Then the identification info creation should fail
        And the error should be "National ID is required."

    Scenario: Cannot create identification info with national ID too long
        When I create identification info with national ID of 65 characters
        Then the identification info creation should fail
        And the error should be "National ID cannot exceed 64 characters."

    Scenario: Create identification info with national ID at maximum length
        When I create identification info with national ID of 64 characters
        Then the identification info should be created successfully

    Scenario: Cannot create identification info with empty ID nationality
        When I create identification info with national ID "12345678" and ID nationality ""
        Then the identification info creation should fail
        And the error should be "ID nationality is required."

    Scenario: Cannot create identification info with null ID nationality
        When I create identification info with national ID "12345678" and null ID nationality
        Then the identification info creation should fail
        And the error should be "ID nationality is required."

    Scenario: Cannot create identification info with whitespace only ID nationality
        When I create identification info with national ID "12345678" and ID nationality "   "
        Then the identification info creation should fail
        And the error should be "ID nationality is required."

    Scenario: Cannot create identification info with ID nationality too long
        When I create identification info with ID nationality of 65 characters
        Then the identification info creation should fail
        And the error should be "ID nationality cannot exceed 64 characters."

    Scenario: Create identification info with ID nationality at maximum length
        When I create identification info with ID nationality of 64 characters
        Then the identification info should be created successfully

    Scenario: Cannot create identification info with multiple validation errors
        When I create identification info with national ID "" and ID nationality ""
        Then the identification info creation should fail
        And the error should be "National ID is required."
        And the error should be "ID nationality is required."

    Scenario: Cannot create identification info with both fields too long
        When I create identification info with national ID of 65 characters and ID nationality of 65 characters
        Then the identification info creation should fail
        And the error should be "National ID cannot exceed 64 characters."
        And the error should be "ID nationality cannot exceed 64 characters."