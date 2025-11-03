Feature: Booking Entity
As a domain model
I want to ensure Booking entity operations work correctly
So that booking data integrity is maintained independently of the aggregate root

    Scenario: Create booking with valid data
        When I create a booking with base price 1000, room type "DoubleRoom", room cost 0, and regular bike 100 for principal
        Then the booking should be created successfully
        And the booking total price should be 1100

    Scenario: Create booking with principal and companion customers
        When I create a booking with base price 1000, room type "DoubleRoom", room cost 200, regular bike 100 for principal, and eBike 200 for companion
        Then the booking should be created successfully
        And the booking total price should be 1500

    Scenario: Create booking with single room supplement
        When I create a booking with base price 1000, room type "SingleRoom", room cost 0, and regular bike 100 for principal
        Then the booking should be created successfully
        And the booking total price should be 1100

    Scenario: Cannot create booking with zero base price
        When I try to create a booking with base price 0
        Then the booking creation should fail
        And the error should be for field "basePrice"

    Scenario: Cannot create booking with negative base price
        When I try to create a booking with base price -100
        Then the booking creation should fail
        And the error should be for field "basePrice"

    Scenario: Cannot create booking with base price exceeding maximum
        When I try to create a booking with base price 100001
        Then the booking creation should fail
        And the error should be for field "basePrice"

    Scenario: Cannot create booking with negative room cost
        When I try to create a booking with base price 1000 and room cost -100
        Then the booking creation should fail
        And the error should be for field "roomAdditionalCost"

    Scenario: Cannot create booking with room cost exceeding maximum
        When I try to create a booking with base price 1000 and room cost 100001
        Then the booking creation should fail
        And the error should be for field "roomAdditionalCost"

    Scenario: Cannot create booking with notes exceeding maximum length
        When I try to create a booking with notes of 2001 characters
        Then the booking creation should fail
        And the error should be for field "notes"

    Scenario: Create booking with notes at maximum length
        When I create a booking with notes of 2000 characters
        Then the booking should be created successfully

    Scenario: Create booking sanitizes whitespace in notes
        When I create a booking with notes "   "
        Then the booking should be created successfully
        And the booking notes should be null

    Scenario: Update booking payment status
        Given a booking exists
        When I update the payment status to "Paid"
        Then the booking payment status should be "Paid"

    Scenario: Confirm pending booking
        Given a booking exists with status "Pending"
        When I confirm the booking
        Then the booking status should be "Confirmed"

    Scenario: Confirm confirmed booking is idempotent
        Given a booking exists with status "Confirmed"
        When I confirm the booking
        Then the booking status should be "Confirmed"

    Scenario: Cannot confirm cancelled booking
        Given a booking exists with status "Cancelled"
        When I try to confirm the booking
        Then the status transition should fail
        And the error should mention "Cancelled" and "Confirmed"

    Scenario: Cannot confirm completed booking
        Given a booking exists with status "Completed"
        When I try to confirm the booking
        Then the status transition should fail
        And the error should mention "Completed" and "Confirmed"

    Scenario: Cancel pending booking
        Given a booking exists with status "Pending"
        When I cancel the booking
        Then the booking status should be "Cancelled"

    Scenario: Cancel confirmed booking
        Given a booking exists with status "Confirmed"
        When I cancel the booking
        Then the booking status should be "Cancelled"

    Scenario: Cancel cancelled booking is idempotent
        Given a booking exists with status "Cancelled"
        When I cancel the booking
        Then the booking status should be "Cancelled"

    Scenario: Cannot cancel completed booking
        Given a booking exists with status "Completed"
        When I try to cancel the booking
        Then the status transition should fail
        And the error should mention "Completed" and "Cancelled"

    Scenario: Complete pending booking
        Given a booking exists with status "Pending"
        When I complete the booking
        Then the booking status should be "Completed"

    Scenario: Complete confirmed booking
        Given a booking exists with status "Confirmed"
        When I complete the booking
        Then the booking status should be "Completed"

    Scenario: Complete completed booking is idempotent
        Given a booking exists with status "Completed"
        When I complete the booking
        Then the booking status should be "Completed"

    Scenario: Cannot complete cancelled booking
        Given a booking exists with status "Cancelled"
        When I try to complete the booking
        Then the status transition should fail
        And the error should mention "Cancelled" and "Completed"

    Scenario: Update booking notes
        Given a booking exists
        When I update the booking notes to "Updated notes"
        Then the booking notes should be "Updated notes"

    Scenario: Clear booking notes
        Given a booking exists with notes "Initial notes"
        When I update the booking notes to null
        Then the booking notes should be null

    Scenario: Sanitize whitespace in booking notes
        Given a booking exists with notes "Initial notes"
        When I update the booking notes to "   "
        Then the booking notes should be null

    Scenario: Cannot update notes exceeding maximum length
        Given a booking exists
        When I try to update the booking notes to 2001 characters
        Then the notes update should fail
        And the error should be for field "notes"

    Scenario: Status transition from pending to confirmed to cancelled
        Given a booking exists with status "Pending"
        When I confirm the booking
        And I cancel the booking
        Then the booking status should be "Cancelled"

    Scenario: Status transition from confirmed to completed
        Given a booking exists with status "Confirmed"
        When I complete the booking
        Then the booking status should be "Completed"