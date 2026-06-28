@BC:Catalog
@Agg:CatalogTour
@regression
Feature: Catalog tour workflows
As a catalog editor
I want tour drafts to be prepared and published through the catalog
So that public visitors only see approved tour presentations

    Rule: Catalog editors manage tour presentation before public release

        @smoke
        @happy_path
        @Entity:CatalogTour
        Scenario: Publish a prepared catalog tour
            Given a catalog tour draft exists for identifier "CUBA2025"
            When I publish the catalog tour with title "Cycling Cuba" and slug "cycling-cuba"
            Then the catalog tour should be available to catalog editors
            And the catalog tour should be visible publicly by slug "cycling-cuba"

        @happy_path
        @Entity:CatalogTour
        Scenario: Keep an unpublished catalog tour unavailable publicly
            Given a catalog tour draft exists for identifier "ANDES2025"
            When I save the catalog tour presentation with title "Andes Preview" and slug "andes-preview"
            Then the catalog tour should be available to catalog editors
            And the catalog tour should not be visible publicly by slug "andes-preview"

    Rule: Catalog tour presentation requests must target an available draft

        @error_case
        @Entity:CatalogTour
        Scenario: Reject publication for a missing catalog tour
            Given no catalog tour draft exists
            When I try to publish a missing catalog tour
            Then the catalog tour workflow should report that the tour is unavailable

        @error_case
        @Entity:CatalogTour
        Scenario: Reject an invalid catalog tour presentation
            Given a catalog tour draft exists for identifier "PATAGONIA2025"
            When I try to publish the catalog tour without a title
            Then the catalog tour workflow should report a presentation validation problem
