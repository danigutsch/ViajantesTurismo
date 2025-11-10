@BC:Admin @Agg:Customer @Entity:PersonalInfo @regression
Feature: Last Name Validation
  As a customer service representative
  I want to validate customer last names
  So that we maintain accurate and complete customer records

  Last names are essential for customer identification and legal documentation.
  Names must accommodate diverse cultural naming conventions while ensuring
  data completeness and quality.

  Business Rules:
  - Last name is required (cannot be empty or whitespace)
  - Maximum length is 128 characters
  - Names can contain letters, hyphens, apostrophes, spaces, and accented characters
  - Names are automatically trimmed and normalized

  Rule: Last name is required and cannot be empty
    Every customer must have a last name for identification.
    Empty or whitespace-only values are not accepted.

    @Invariant:INV-CUST-005 @smoke @happy_path
    Scenario: Register customer with valid last name
      Given I have valid personal information
      When I create the personal info
      Then the creation should succeed
      And the personal info should contain the provided data

    @Invariant:INV-CUST-005 @error_case @critical
    Scenario: Last name is required
      When I attempt to create personal info with null last name
      Then I should not be able to create the personal info
      And I should be informed that last name is required

    @Invariant:INV-CUST-005 @error_case
    Scenario: Last name cannot be empty
      When I attempt to create personal info with last name ""
      Then I should not be able to create the personal info
      And I should be informed that last name is required

    @Invariant:INV-CUST-005 @error_case
    Scenario: Last name cannot be whitespace only
      When I attempt to create personal info with last name "   "
      Then I should not be able to create the personal info
      And I should be informed that last name is required

  Rule: Last name must be within length limits
    Names must be reasonable length to ensure compatibility with
    systems and prevent data quality issues.

    @Invariant:INV-CUST-006 @error_case
    Scenario: Last name cannot exceed maximum length
      When I attempt to create personal info with last name of 129 characters
      Then I should not be able to create the personal info
      And I should be informed that last name cannot exceed 128 characters

    @Invariant:INV-CUST-006 @happy_path @edge_case
    Scenario: Last name at maximum length is accepted
      When I create personal info with last name of 128 characters
      Then the creation should succeed
      And the personal info should contain the provided data

  Rule: Last name supports diverse naming conventions
    Names must accommodate various cultural and linguistic patterns
    including compound names, hyphens, apostrophes, and accented characters.

    @Invariant:INV-CUST-007 @happy_path
    Scenario: Single-word last name is accepted
      When I create personal info with last name "Johnson"
      Then the creation should succeed
      And the personal info should contain the provided data

    @Invariant:INV-CUST-007 @happy_path
    Scenario: Hyphenated last name is accepted
      When I create personal info with last name "Smith-Jones"
      Then the creation should succeed
      And the personal info should contain the provided data

    @Invariant:INV-CUST-007 @happy_path
    Scenario: Compound last name is accepted
      When I create personal info with last name "Van der Berg"
      Then the creation should succeed
      And the personal info should contain the provided data

    @Invariant:INV-CUST-007 @happy_path
    Scenario: Last name with apostrophe is accepted
      When I create personal info with last name "O'Connor"
      Then the creation should succeed
      And the personal info should contain the provided data

    @Invariant:INV-CUST-007 @happy_path
    Scenario: Last name with accents is accepted
      When I create personal info with last name "García"
      Then the creation should succeed
      And the personal info should contain the provided data
