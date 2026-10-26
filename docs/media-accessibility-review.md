# Media accessibility review

Editors review pending descriptions at `/catalog/tours/{id}/images/review`. Each language has its own
review state. The declared language selector accepts the application's explicit BCP 47 allowlist:
`en-US` and `pt-BR`. The API's `PublicContentLanguageDto` enum represents those declarations; `None`
and undefined values fail domain validation. Detection never changes the declared language or decides
whether an editor may approve accessible text. Decorative images may have empty alternative text.

The management response exposes localized accessibility text. Approval uses the narrow
`accessibility-review` command; its request has no storage keys, processing state, variants, or links.
Generated text remains an editable, unapproved draft until the editor approves it.

## Optional local language guidance

The management server can run the approved fastText CLI locally. No accessibility text is sent to a
cloud detector, command-line arguments, logs, or metrics. Text reaches the child process through UTF-8
standard input. A two-second deadline, bounded output, cancellation, and process-tree cleanup limit
failures. Guidance is advisory; low-confidence, unsupported, ambiguous, and unavailable results are
indeterminate and never block approval.

This adds an **operational supply-chain dependency**: fastText v0.9.2 (MIT), plus the official
`lid.176.ftz` model (CC BY-SA 3.0). Review and provision both artifacts outside the application; the app
never downloads or installs them. fastText upstream is archived, so deployments must assess ongoing
maintenance and platform support before enabling it. Keep the model's attribution with distributed
artifacts. See [official language model documentation](https://fasttext.cc/docs/en/language-identification.html)
and [upstream source](https://github.com/facebookresearch/fastText/tree/v0.9.2).

Configure absolute paths through deployment configuration:

```text
MediaLanguageIdentification__ExecutablePath=/opt/fasttext/fasttext
MediaLanguageIdentification__ModelPath=/opt/fasttext/lid.176.ftz
```

The pinned model SHA-256 is
`8f3472cfe8738a7b6099e8e999c3cbfae0dcd15696aac7d7738a8039db603e83`.
The service verifies that digest before inference. A mismatched artifact or relative configured path
raises a configuration error. Leave both paths unset to disable guidance. Restrict write access to the
executable and model to deployment administrators. Do not configure user-controlled paths.

The conservative tested minimum is 40 letters and six whitespace-separated words. Numeric/emoji-only,
short, and predominantly title-cased proper-name-like input is indeterminate. The model must return an
English or Portuguese primary-language prediction with probability at least 0.90 and a margin of at
least 0.50 over its runner-up. Longer text (at least 12 words) also requires agreement between the full
text and both halves; disagreement or uncertainty is indeterminate. These are editorial heuristics,
not proof of language or a correctness claim for every mixed-language passage. No common-word list is
used. Only the primary subtag is compared, independently of the declared region.

The model expects UTF-8, as documented upstream. The v0.9.2 sources may require the compiler option
`-include cstdint` on modern GCC; current upstream source includes that header explicitly.

The real-engine verification cases run with every other test of the WebTests project. They carry the trait
`Requires=fasttext`, and they fail with a message that names the variables when `MEDIA_TEST_FASTTEXT` and
`MEDIA_TEST_FASTTEXT_MODEL` do not hold the provisioned absolute paths. An environment without the engine leaves
them out with `--filter-not-trait Requires=fasttext`; `sharedkernel-ci run --exclude-trait Requires=fasttext`
does the same for a slice. Parser and indeterminate-rule cases remain independent of external artifacts.

The Linux Fast Validation 2 CI job provisions the same checksum-pinned source archive and model,
builds the CLI with the runner's C++ compiler, and enables these real-engine tests for coverage.
Downloads must match their committed SHA-256 values before extraction or use; a download, checksum,
or build failure fails the job. This test-only provisioning does not install the engine in production.

Language declarations follow [W3C BCP 47 guidance](https://www.w3.org/International/articles/language-tags/).
