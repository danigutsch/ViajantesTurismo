Feature: Physical Info Validation
As a system administrator
I want physical information to be validated
So that we maintain valid customer physical characteristics

    Scenario: Create physical info with valid data
        When I create physical info with weight 75 kg, height 180 cm, and bike type "Regular"
        Then the physical info should be created successfully

    Scenario: Create physical info with minimum valid weight
        When I create physical info with weight 1 kg
        Then the physical info should be created successfully

    Scenario: Create physical info with maximum valid weight
        When I create physical info with weight 500 kg
        Then the physical info should be created successfully

    Scenario: Create physical info with minimum valid height
        When I create physical info with height 50 cm
        Then the physical info should be created successfully

    Scenario: Create physical info with maximum valid height
        When I create physical info with height 300 cm
        Then the physical info should be created successfully

    Scenario: Cannot create physical info with weight below minimum
        When I create physical info with weight 0 kg
        Then the physical info creation should fail
        And the error should be "Weight must be between 1 and 500 kilograms."

    Scenario: Cannot create physical info with weight above maximum
        When I create physical info with weight 501 kg
        Then the physical info creation should fail
        And the error should be "Weight must be between 1 and 500 kilograms."

    Scenario: Cannot create physical info with negative weight
        When I create physical info with weight -10 kg
        Then the physical info creation should fail
        And the error should be "Weight must be between 1 and 500 kilograms."

    Scenario: Cannot create physical info with height below minimum
        When I create physical info with height 49 cm
        Then the physical info creation should fail
        And the error should be "Height must be between 50 and 300 centimeters."

    Scenario: Cannot create physical info with height above maximum
        When I create physical info with height 301 cm
        Then the physical info creation should fail
        And the error should be "Height must be between 50 and 300 centimeters."

    Scenario: Cannot create physical info with negative height
        When I create physical info with height -100 cm
        Then the physical info creation should fail
        And the error should be "Height must be between 50 and 300 centimeters."

    Scenario: Cannot create physical info with multiple validation errors
        When I create physical info with weight 0 kg and height 0 cm
        Then the physical info creation should fail
        And the error should be "Weight must be between 1 and 500 kilograms."
        And the error should be "Height must be between 50 and 300 centimeters."

    Scenario: Create physical info with bike type None
        When I create physical info with bike type "None"
        Then the physical info should be created successfully

    Scenario: Create physical info with bike type EBike
        When I create physical info with bike type "EBike"
        Then the physical info should be created successfully