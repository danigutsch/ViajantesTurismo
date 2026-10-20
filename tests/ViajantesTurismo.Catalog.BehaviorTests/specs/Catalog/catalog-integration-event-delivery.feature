@BC:Catalog
@Agg:CatalogTour
@regression
@integration-event-transport
Feature: Catalog integration event delivery
    As a catalog editor
    I want repeated Admin notifications to be harmless
    So that each Admin tour has one editable draft

    @happy_path
    @Entity:CatalogTour
    Scenario: Redeliver an Admin tour-created integration event
        Given an Admin tour-created event for identifier "ANDES2030"
        When Catalog receives that same integration event twice
        Then Catalog exposes exactly one unchanged draft for that Admin tour
