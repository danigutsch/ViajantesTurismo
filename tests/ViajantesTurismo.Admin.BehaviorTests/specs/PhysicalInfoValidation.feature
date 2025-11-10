@BC:Admin @Agg:Customer @VO:PhysicalInfo @regression
Feature: Physical Info Validation

Physical information captures customer weight, height, and bike preference for tour planning.
Weight and height must fall within reasonable ranges to ensure safety and proper equipment sizing.
This information helps tour operators prepare appropriate bikes and accommodations.

    Rule: Weight must be within valid range for safety
        @Invariant:INV-CUST-017
        Scenario: I create physical info with weight at minimum boundary
            When I create physical info with weight 1 kg
            Then the physical info should be created successfully

        @Invariant:INV-CUST-017
        Scenario: I create physical info with weight at maximum boundary
            When I create physical info with weight 500 kg
            Then the physical info should be created successfully

        @Invariant:INV-CUST-017
        Scenario: I create physical info with typical weight
            When I create physical info with weight 75 kg
            Then the physical info should be created successfully

        @Invariant:INV-CUST-017
        Scenario: I attempt to create physical info with weight below minimum
            When I create physical info with weight 0 kg
            Then the physical info creation should fail
            And the error should be "Weight must be between 1 and 500 kilograms."

        @Invariant:INV-CUST-017
        Scenario: I attempt to create physical info with weight above maximum
            When I create physical info with weight 501 kg
            Then the physical info creation should fail
            And the error should be "Weight must be between 1 and 500 kilograms."

        @Invariant:INV-CUST-017
        Scenario: I attempt to create physical info with negative weight
            When I create physical info with weight -10 kg
            Then the physical info creation should fail
            And the error should be "Weight must be between 1 and 500 kilograms."

    Rule: Height must be within valid range for equipment sizing
        @Invariant:INV-CUST-018
        Scenario: I create physical info with height at minimum boundary
            When I create physical info with height 50 cm
            Then the physical info should be created successfully

        @Invariant:INV-CUST-018
        Scenario: I create physical info with height at maximum boundary
            When I create physical info with height 300 cm
            Then the physical info should be created successfully

        @Invariant:INV-CUST-018
        Scenario: I create physical info with typical height
            When I create physical info with height 180 cm
            Then the physical info should be created successfully

        @Invariant:INV-CUST-018
        Scenario: I attempt to create physical info with height below minimum
            When I create physical info with height 49 cm
            Then the physical info creation should fail
            And the error should be "Height must be between 50 and 300 centimeters."

        @Invariant:INV-CUST-018
        Scenario: I attempt to create physical info with height above maximum
            When I create physical info with height 301 cm
            Then the physical info creation should fail
            And the error should be "Height must be between 50 and 300 centimeters."

        @Invariant:INV-CUST-018
        Scenario: I attempt to create physical info with negative height
            When I create physical info with height -100 cm
            Then the physical info creation should fail
            And the error should be "Height must be between 50 and 300 centimeters."

    Rule: Multiple validation errors are reported together
        @Invariant:INV-CUST-017 @Invariant:INV-CUST-018
        Scenario: I attempt to create physical info with both invalid weight and height
            When I create physical info with weight 0 kg and height 0 cm
            Then the physical info creation should fail
            And the error should be "Weight must be between 1 and 500 kilograms."
            And the error should be "Height must be between 50 and 300 centimeters."

    Rule: Bike type preference can be specified
        Scenario: I create physical info with standard bike preference
            When I create physical info with bike type "Regular"
            Then the physical info should be created successfully

        Scenario: I create physical info with electric bike preference
            When I create physical info with bike type "EBike"
            Then the physical info should be created successfully

        Scenario: I create physical info with no bike preference initially
            When I create physical info with bike type "None"
            Then the physical info should be created successfully
