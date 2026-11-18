# Privacy classification and redaction

ViajantesTurismo treats GDPR and Brazil LGPD as baseline privacy requirements.
The engineering rule is: collect less, store less, expose less, log less.

## Technical taxonomy

- Personal: email, phone, name, address, nationality, occupation, social profile handles, customer
  identifiers, booking identifiers linked to a traveller, and other values that identify a person.
- Sensitive: national ID, health or medical information, allergy information, physical data, booking
  notes, and other confidential free text that can contain personal data.
- Credential: passwords, tokens, API keys, authentication headers, session or ticket identifiers,
  connection secrets, and payment secrets.
- Financial: payment references and non-public pricing or payment details linked to a customer or booking.
- Operational data: status codes, operation names, low-cardinality outcomes, and opaque correlation IDs.

These are technical handling classes only. They do not encode legal basis, consent, purpose, or
retention periods.

### Admin data inventory

| Owner and API representation | Fields and classes | Handling |
| --- | --- | --- |
| `Customer` with `PersonalInfo` and `GetCustomerDto` / `PersonalInfoDto` | Customer ID, first and last names, gender, nationality and occupation are personal. Date of birth is sensitive. The list projection also carries email, mobile and bike choice. | Require the Admin customer read permissions. Use the list projection for list views and request the details only for the selected customer. Keep IDs and values out of telemetry. |
| `IdentificationInfo` and `IdentificationInfoDto` | National ID is sensitive. Issuing nationality is personal and the combination identifies the document holder. | Keep values within authorized customer details and approved document sources. Never include these values in generic errors, events or telemetry. |
| `ContactInfo`, `Address` and `EmergencyContact` with the corresponding detail DTOs | Email, mobile, Instagram and Facebook handles, street, complement, neighborhood, postal code, city, state and country are personal. The emergency contact name and mobile identify another person. | Restrict customer and emergency-contact data to the authorized workflow. Omit contact values and complete addresses from diagnostics. |
| `PhysicalInfo`, `AccommodationPreferences`, `MedicalInfo` and their DTOs | Weight, height, allergies and additional medical notes are sensitive. Bike, room and bed preferences and companion ID are personal. Treat free text as sensitive. | Return these values in the authorized detail scope. Keep health, physical measurements and notes out of public and integration payloads and telemetry. |
| `Booking` and `GetBookingDto` | Booking ID, booking date, customer and companion IDs and names, room type, bike type and booking status describe an identifiable booking. Notes and discount reasons can contain sensitive free text. Total, discount, currency, paid amount, balance and the approved obligation are financial in this context. Tour catalog fields alone are public only when published. | Booking reads that include financial data require booking and payment read permission. Treat every joined booking projection as personal, including when it uses public tour names or identifiers. |
| Booking financial history and payment DTOs | Receipt references, quotes, payment instructions, settlement amounts and evidence links are financial. Payment-routing secrets and credentials use the credential handling class. | Keep financial evidence in Admin and use operation-specific permissions. Logs and metrics may carry bounded outcomes. They must omit all references, account values and amounts. |
| Customer CSV and `ImportResultDto` | The upload may hold all customer field classes. Conflict rows carry email. Success rows may carry email and customer ID. Error rows may carry email, row number, field and error message. | Treat the uploaded file and the staff error report as personal data. Prevent raw rows, filenames, local paths and scanner output from reaching logs or traces. Treat the downloaded error report as a restricted copy. |

### Data flows and copies

| Flow | Current boundary and handling |
| --- | --- |
| Management Web to Admin | Customer and booking reads and writes traverse the authenticated application API. Admin authorization owns customer, sensitive-customer, booking and payment permissions. Avoid shared response caches for these records and avoid URLs containing field values. |
| Admin persistence | PostgreSQL stores the customer record and its owned personal, identification, contact, address, physical, accommodation, emergency-contact and medical rows. Booking and financial records remain in Admin's database. Apply the same data classifications to database backups, restored copies and export jobs. |
| CSV validation and import | Management Web accepts the file and prepares column mappings. The Admin import endpoints enforce the configured file bound and scan the raw upload before parsing. Preview and conflict handling return only authorized import results. Commit persists accepted customer rows. The endpoint disposes its input and seekable lease, while the browser's original file and downloaded error report remain staff-controlled copies. |
| Staff exports and documents | The implemented import error report is generated from failed rows for a staff download. Generated customer documents can contain private booking, traveler and financial values, so use the document-specific authorization, audit and artifact policies for preparation and delivery. Record any future bulk export as its own data flow. |
| Admin to Catalog and Public Web | `AdminTourCreatedIntegrationEvent` contains the event ID and timestamp plus the Admin tour ID, business identifier and name. Customer and booking fields remain in Admin. Catalog public projections and public DTOs expose published tour and media content. Review new event and projection schemas before adding fields. |
| Errors, logs, traces and metrics | Error responses expose bounded error codes or field names. Staff import results have a separate data boundary. Application providers and the trusted Collector apply the controls below. Keep raw exception payloads, SQL values, URLs with identifiers and uploaded content out of exported signals. |

## Logging and telemetry

- Do not log request or response bodies that may contain personal data.
- Do not log personal data in URL paths, query strings, telemetry tags, metric dimensions, or log scopes.
- Prefer source-generated `LoggerMessage` methods.
- Apply `PersonalData`, `SensitiveData`, `CredentialData`, or `FinancialData` only to
  source-generated logging parameters that can carry the corresponding class. Do not decorate DTOs
  or operational fields merely because they appear near personal data.
- Prefer omitting sensitive values entirely. Classification and redaction are for a currently required
  structured field, not justification to add one.
- Default redaction erases classified values before logs leave the process.
- `AddRedaction()` plus `EnableRedaction()` protects both formatted messages and structured parameter
  values before providers run. The fallback remains the erasing redactor; do not replace it with a
  pass-through fallback.
- Classification attributes do not make arbitrary scope objects safe. Scopes must contain operational
  values only; application OTLP logging does not export scopes or preformatted messages.
- Log bounded exception type names, not exception objects, messages, stack traces, request or response
  bodies, headers, query values, customer identifiers, booking identifiers, or object-storage keys.
- Keep `http.route`, request method, server address, response status, operation, and low-cardinality
  outcome fields. Remove raw `url.path`, `url.full`, query values, and customer or booking identifiers
  before trace export.

Redaction reduces accidental disclosure risk. It is not permission to collect or log more data.

## Current implementation

Shared observability enables `Microsoft.Extensions.Compliance` logging redaction with an erasing
fallback redactor. Classified source-generated logging parameters are removed from formatted log
messages and structured state before providers receive them. Service defaults remove preconfigured
logging providers so only the sanitized OpenTelemetry pipeline is enabled by default, and also remove exception
payloads, raw paths, query values, and customer or booking identifiers from OTLP-bound logs and traces.
Scanner diagnostics retain status and bounded failure type only; scanner messages and storage keys are
not logged. ASP.NET Core, HttpClient, gRPC, Entity Framework Core, Npgsql, AWS SDK, and custom tracing
remain enabled. AWS tracing suppresses duplicate downstream HTTP spans and retains S3 source-tag
redaction. Npgsql does not enable parameter logging and suppresses its optional first-response event.

Provider failure spans can contain immutable exception events that an application processor cannot
remove after the activity stops. Every supported AppHost profile therefore routes normal
Aspire-annotated OTLP traffic through the pinned trusted Collector. The Collector removes sensitive
resource attributes from traces, logs, and metrics. It also drops all span events, clears status
descriptions, and removes explicit URL, header, body, query, parameter, identifier, and AWS span
attributes before forwarding telemetry to the Aspire dashboard or optional backends. Raw
telemetry exists on the trusted application-to-Collector hop. A manually constructed direct exporter or
a process outside AppHost can bypass this boundary. Signal-specific OTLP endpoint variables can also
override the generic endpoint rewritten by AppHost. Deployments must reject or rewrite those overrides,
restrict direct backend access, and configure authenticated and encrypted Collector ingress and
downstream transport.

## References

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/core/extensions/compliance>
- Microsoft Learn data classification: <https://learn.microsoft.com/en-us/dotnet/core/extensions/data-classification>
- Microsoft Learn data redaction: <https://learn.microsoft.com/en-us/dotnet/core/extensions/data-redaction>
- [API I/O and streaming](API_IO_AND_STREAMING.md)
- [Document retention and legal-hold policy](operations/document-retention-and-legal-hold.md)
- GDPR Article 5, 9, 25, and 32: <https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng>
- Brazil LGPD law: <https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm>
