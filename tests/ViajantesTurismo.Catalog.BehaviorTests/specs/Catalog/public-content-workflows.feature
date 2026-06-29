@BC:Catalog
@Agg:PublicContent
@regression
Feature: Public content workflows
As a catalog editor
I want localized public content to require complete translations
So that visitors do not see incomplete website copy

    Rule: Public content requires all supported localized variants

        @smoke
        @happy_path
        @Entity:PublicContent
        Scenario: Save localized public content that requires review
            Given localized public content for key "home.hero" includes English and Portuguese variants
            When I save the public content
            Then the public content should be stored for key "HOME.HERO"
            And the public content should require review before publication

        @error_case
        @Entity:PublicContent
        Scenario: Reject public content with a missing supported language
            Given localized public content for key "home.hero" includes only English
            When I save the public content
            Then the public content workflow should report a localization validation problem
