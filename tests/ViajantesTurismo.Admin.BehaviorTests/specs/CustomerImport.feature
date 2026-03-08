@BC:Admin @Agg:Customer @regression
Feature: Customer Import from CSV
  As a tour operator
  I want to import customers from a CSV file
  So that I can bulk-load customer records efficiently

  Background:
    Given I am authenticated as a tour operator

  Rule: A valid CSV file is accepted and all rows are imported

    @happy_path @smoke
    Scenario: Import a single valid customer row
      Given I have a valid canonical CSV with 1 customer row
      When I run the import
      Then 1 customer should be imported successfully
      And 0 rows should have errors

  Rule: Rows with a missing required column header report errors for that row

    @error_case
    Scenario: CSV without the Email column header reports a row error
      Given I have a canonical CSV without the Email column header
      When I run the import
      Then 0 customers should be imported successfully
      And 1 row should have errors

  Rule: Rows with blank required field values report errors

    @error_case
    Scenario: Row with an empty Email value is reported as an error
      Given I have a canonical CSV with a blank Email value
      When I run the import
      Then 0 customers should be imported successfully
      And 1 row should have errors

  Rule: Dry-run mode returns row counts without persisting data

    @happy_path
    Scenario: Dry-run import reports success count without creating customers
      Given I have a valid canonical CSV with 1 customer row
      When I run the import in dry-run mode
      Then 1 customer success should be reported
      And no customers should exist in the store

  Rule: Duplicate emails already in the database are surfaced for user resolution before commit

    @duplicate_resolution
    Scenario: Duplicate email in CSV is surfaced for resolution before commit
      Given an existing customer with email "john.import@example.com"
      And I have a canonical CSV with duplicate email "john.import@example.com"
      When I run the import workflow pre-check
      Then 1 duplicate conflict should be surfaced
      And the duplicate conflict should contain email "john.import@example.com"

  Rule: Final summary reflects conflict resolution outcomes and retry flow

    @summary
    Scenario: Summary counts are correct when duplicate conflicts are kept
      Given an existing customer record with email "john.import@example.com" and first name "Existing"
      And I have a canonical CSV with the following customer rows
        | FirstName | Email                    |
        | Incoming  | john.import@example.com  |
        | NewOne    | new.import@example.com   |
      When I commit the import workflow with resolutions
        | Email                   | Decision |
        | john.import@example.com | keep     |
      Then import summary should report 1 created, 0 updated, 1 skipped, and 0 failed
      And customer with email "john.import@example.com" should have first name "Existing"
      And customer with email "new.import@example.com" should exist in the store

    @summary
    Scenario: Summary counts are correct when duplicate conflicts are overwritten
      Given an existing customer record with email "john.import@example.com" and first name "Existing"
      And I have a canonical CSV with the following customer rows
        | FirstName | Email                    |
        | Incoming  | john.import@example.com  |
        | NewOne    | new.import@example.com   |
      When I commit the import workflow with resolutions
        | Email                   | Decision  |
        | john.import@example.com | overwrite |
      Then import summary should report 1 created, 1 updated, 0 skipped, and 0 failed
      And customer with email "john.import@example.com" should have first name "Incoming"
      And customer with email "new.import@example.com" should exist in the store

    @summary
    Scenario: Successful imported row can open customer details from summary
      Given I have a valid canonical CSV with 1 customer row
      When I run the import
      Then 1 customer should be imported successfully
      And imported customer with email "john.import.1@example.com" should have a stable identifier

    @summary
    Scenario: Retry with corrected input succeeds after initial row failure
      Given I have a canonical CSV with a blank Email value
      When I run the import
      Then 0 customers should be imported successfully
      And 1 row should have errors
      When I replace blank emails with generated valid emails and rerun the import
      Then 1 customer should be imported successfully
      And 0 rows should have errors
