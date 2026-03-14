Feature: Accommodation Preferences Validation
As a system administrator
I want accommodation preferences to be validated
So that we maintain valid customer accommodation records

    Scenario: Create accommodation preferences with valid double room and companion
        When I create accommodation preferences with double room, double bed, and companion ID 123
        Then the accommodation preferences should be created successfully

    Scenario: Create accommodation preferences with valid single room without companion
        When I create accommodation preferences with single room, single bed, and no companion
        Then the accommodation preferences should be created successfully

    Scenario: Create accommodation preferences with single room and single bed
        When I create accommodation preferences with single room, single bed, and no companion
        Then the accommodation preferences should be created successfully

    Scenario: Create accommodation preferences with single room and double bed
        When I create accommodation preferences with single room, double bed, and no companion
        Then the accommodation preferences should be created successfully

    Scenario: Create accommodation preferences with double room without companion
        When I create accommodation preferences with double room, double bed, and no companion
        Then the accommodation preferences should be created successfully

    Scenario: Create accommodation preferences with single room and companion
        When I create accommodation preferences with single room, single bed, and companion ID 123
        Then the accommodation preferences should be created successfully

    Scenario: Double room with companion ID is valid
        When I create accommodation preferences with double room, double bed, and companion ID 456
        Then the accommodation preferences should be created successfully
        And the companion ID should be 456

    Scenario: Single room without companion ID is valid
        When I create accommodation preferences with single room, double bed, and no companion
        Then the accommodation preferences should be created successfully
        And the companion ID should be null