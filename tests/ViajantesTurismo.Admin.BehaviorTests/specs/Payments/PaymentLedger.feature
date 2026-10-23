@BC:Admin
@Agg:Tour
@regression
Feature: Explicit payment terms and settlement
    EUR obligations are approved separately from recording actual EUR or BRL cash.
    Recording a receipt never silently approves credit or erases debt.

    Background:
        Given an EUR booking priced at 100 with no approved payment terms

    Scenario: Draft terms never imply approval
        When the Admin drafts EUR payment terms
        Then the outstanding EUR obligation is 0
        And the booking payment status should be "Unpaid"

    Scenario: A solo Admin can approve their own terms
        When the Admin drafts EUR payment terms
        And the same Admin approves those payment terms
        Then the outstanding EUR obligation is 100
        And the schedule is visibly self approved

    Scenario: A receipt remains pending until an explicit Admin reconciliation
        Given EUR payment terms are approved
        When an Operator records actual cash of 40 in EUR
        Then the approved EUR credit is 0
        And the outstanding EUR obligation is 100
        When the Admin approves EUR credit of 40
        Then the approved EUR credit is 40
        And the outstanding EUR obligation is 60
        And the booking payment status should be "PartiallyPaid"

    Scenario: Agreed BRL settlement credits the EUR obligation
        Given EUR payment terms are approved
        And an Admin approves an agreed quote of 5 BRL per EUR
        When an Operator records actual cash of 500 in BRL
        Then the approved EUR credit is 0
        When the Admin approves EUR credit of 100
        Then the outstanding EUR obligation is 0
        And the booking payment status should be "Paid"

    Scenario: Excess cash becomes explicit customer credit
        Given EUR payment terms are approved
        When an Operator records actual cash of 150 in EUR
        And the Admin approves EUR credit of 150
        Then the outstanding EUR obligation is 0
        And the unapplied EUR customer credit is 50

    Scenario: Cancellation does not erase the approved obligation
        Given EUR payment terms are approved
        When an Operator records actual cash of 40 in EUR
        And the booking is cancelled without an obligation adjustment
        Then the outstanding EUR obligation is 100

    Scenario: Reversing a receipt restores its credited debt
        Given EUR payment terms are approved
        When an Operator records actual cash of 100 in EUR
        And the Admin approves EUR credit of 100
        And the Admin reverses that erroneous receipt
        Then the outstanding EUR obligation is 100
        And the booking payment status should be "Unpaid"
