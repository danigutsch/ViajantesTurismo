@BC:Admin @Agg:Tour @regression
Feature: Tour Creation
  As a tour operator
  I want to create tours with valid configurations
  So that we can offer properly structured tour packages to customers

  Tours are the core product offerings in our system. Each tour must meet
  specific business requirements to ensure quality and operational feasibility.

  Business Rules:
  - Tours must have a unique business identifier (e.g., "CUBA2024", "AMAZON2025")
  - Tour duration must be at least 5 days (minimum viable cycling tour)
  - All pricing must be positive and within reasonable business limits (0 to 100,000)
  - Customer capacity must be between 1 and 20 travelers
  - Date ranges must be valid (end after start)

  Rule: Tours must have valid date ranges
    Tour dates define when the cycling tour operates. The end date must be
    after the start date, and the tour must meet our minimum duration policy
    of 5 days to ensure a quality cycling experience.

    @Invariant:INV-TOUR-006 @smoke @happy_path
    Scenario: Creating a tour with valid dates
      Given I have tour dates from "2025-06-01" to "2025-06-10"
      When I create the tour
      Then the tour should be created successfully

    @Invariant:INV-TOUR-006 @error_case
    Scenario: End date must be after start date
      Given I have tour dates from "2025-06-10" to "2025-06-01"
      When I attempt to create the tour
      Then I should not be able to create the tour
      And I should be informed that the end date must be after the start date

    @Invariant:INV-TOUR-006 @error_case @edge_case
    Scenario: End date cannot be the same as start date
      Given I have tour dates from "2025-06-01" to "2025-06-01"
      When I attempt to create the tour
      Then I should not be able to create the tour
      And I should be informed that the end date must be after the start date

    @Invariant:INV-TOUR-007 @error_case @critical
    Scenario: Tour must meet minimum duration requirement
      Given I have tour dates from "2025-06-01" to "2025-06-03"
      When I attempt to create the tour
      Then I should not be able to create the tour
      And I should be informed that tours must last at least 5 days

    @Invariant:INV-TOUR-007 @happy_path
    Scenario: Tour with exactly minimum duration
      Given I have tour dates from "2025-06-01" to "2025-06-06"
      When I create the tour
      Then the tour should be created successfully

  Rule: Tour identification must be unique and meaningful
    Each tour needs a business identifier that staff and customers can
    reference (e.g., "CUBA2024", "PATAGONIA2025"). This identifier must
    be provided and kept to a reasonable length. Tour identifiers must
    be unique across all tours to avoid confusion.

    @Invariant:INV-TOUR-001 @error_case @critical
    Scenario: Reject duplicate tour identifier
      Given a tour exists with identifier "CUBA2024"
      When I attempt to create another tour with identifier "CUBA2024"
      Then I should not be able to create the tour
      And I should be informed that the tour identifier must be unique

    @Invariant:INV-TOUR-002 @error_case @critical
    Scenario: Tour requires an identifier
      Given I have tour details with identifier "" and name "Cuba Tour"
      When I attempt to create the tour
      Then I should not be able to create the tour
      And I should be prompted to provide a tour identifier

    @Invariant:INV-TOUR-003 @error_case
    Scenario: Tour identifier has length limits
      Given I have tour details with identifier longer than 128 characters
      When I attempt to create the tour
      Then I should not be able to create the tour
      And I should be informed that the identifier is too long

  Rule: Tour must have a descriptive name
    Tours must have a clear, descriptive name for marketing and customer
    communication purposes.

    @Invariant:INV-TOUR-004 @error_case @critical
    Scenario: Tour requires a name
      Given I have tour details with identifier "CUBA2024" and name ""
      When I attempt to create the tour
      Then I should not be able to create the tour
      And I should be prompted to provide a tour name

    @Invariant:INV-TOUR-005 @error_case
    Scenario: Tour name has length limits
      Given I have tour details with name longer than 128 characters
      When I attempt to create the tour
      Then I should not be able to create the tour
      And I should be informed that the name is too long

  Rule: Tour pricing must be positive and within business limits
    All tour prices must be positive amounts within our business operating
    range (greater than 0 and up to 100,000 in the tour's currency). This includes
    base price, room supplements, and bike rental fees.

    @Invariant:INV-TOUR-008 @error_case @critical
    Scenario Outline: All prices must be positive values
      Given I have tour details with <price_type> <amount>
      When I attempt to create the tour
      Then I should not be able to create the tour
      And I should be informed that prices must be positive

      Examples: Zero prices
        | price_type             | amount |
        | base price             | 0.00   |
        | single room supplement | 0.00   |
        | regular bike price     | 0.00   |
        | e-bike price           | 0.00   |

      Examples: Negative prices
        | price_type             | amount   |
        | base price             | -100.00  |
        | single room supplement | -50.00   |
        | regular bike price     | -30.00   |
        | e-bike price           | -40.00   |

    @Invariant:INV-TOUR-009 @error_case
    Scenario Outline: Prices cannot exceed business maximum
      Given I have tour details with <price_type> 100001.00
      When I attempt to create the tour
      Then I should not be able to create the tour
      And I should be informed that the price exceeds our maximum rate

      Examples:
        | price_type             |
        | base price             |
        | single room supplement |
        | regular bike price     |
        | e-bike price           |

    Scenario: Create tour with multiple validation errors
        Given I have tour details with multiple invalid values
        When I try to create the tour
        Then the tour creation should fail with multiple validation errors
