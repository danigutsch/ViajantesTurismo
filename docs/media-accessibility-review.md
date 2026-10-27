# Media accessibility review

Editors review pending descriptions at `/catalog/tours/{id}/images/review`. Each language has its own
review state. The declared language selector accepts the application's explicit BCP 47 allowlist:
`en-US` and `pt-BR`. The API's `PublicContentLanguageDto` enum represents those declarations; `None`
and undefined values fail domain validation. Language is declared by the editor and never detected;
it does not decide whether an editor may approve accessible text. Decorative images may have empty alternative text.

The management response exposes localized accessibility text. Approval uses the narrow
`accessibility-review` command; its request has no storage keys, processing state, variants, or links.
Generated text remains an editable, unapproved draft until the editor approves it.

Language declarations follow [W3C BCP 47 guidance](https://www.w3.org/International/articles/language-tags/).
