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
