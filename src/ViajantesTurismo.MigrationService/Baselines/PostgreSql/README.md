# PostgreSQL baseline rehearsal

These baseline scripts describe fresh databases at the recorded EF migration boundaries. The
governed runner rehearses the exact script bytes on disposable PostgreSQL databases. Initialization
of a populated target requires a separate baseline operation that reconciles its history and
observed schema before recording the governed baseline.

| Asset | Owner project | Final EF migration or provider source |
| --- | --- | --- |
| `admin.sql` | `ViajantesTurismo.Admin.Infrastructure` | `20261119182941_RecordFinalizedArtifactProvenance` |
| `catalog.sql` | `ViajantesTurismo.Catalog.Infrastructure` | `20261119170602_AddCatalogMediaUploadFingerprint` |
| `branding.sql` | `ViajantesTurismo.Branding.Infrastructure` | `20261118105700_BoundBrandingLogoStorage` |
| `management-security.sql` | `ViajantesTurismo.Management.Security` | `20261111091545_InitialManagementSecurity` |
| `catalog-event-store.sql` | Catalog via the pinned PostgreSQL event-sourcing provider | `PostgreSqlEventSourcingSchema.Initialize` at SharedKernel `d1a0676a24a8c0fbe876cf0986bf9db378c6377d` |

Use the [owning design-time contexts](../../README.md#design-time-ef-core) to reproduce the four
EF scripts. From the owning project directory, pass `migrations script 0 <migration-id>`,
`--context <context>`, `--no-transactions` and `--output <file>` to the pinned `dotnet-ef` tool.
The governed runner owns the surrounding transaction. Store the output as UTF-8 without the
byte-order mark, with LF line endings and one final newline. The Catalog event-store snapshot uses
the default `event_sourcing` schema of the pinned provider. The rehearsal compares it with
initialization through that provider. Run `sha256sum --check SHA256SUMS` in this directory to check
the reviewed bytes.

Catalog runs its two expansion scripts before Branding. Catalog owns the shared transport table.
Branding uses its qualified EF history and a separate owner and governed journal in the Catalog
database. Admin and Management Security use separate databases. The rehearsal grants DDL to
test-owned migration principals in disposable databases. Actual release grants, provider bootstrap
data, and protected promotion are covered by the
[production adoption decision](../../../../docs/adr/20261118-governed-postgresql-migrations.md#adoption-and-rehearsal).

Run the PostgreSQL checks using the [hosted test prerequisites](../../../../tests/ViajantesTurismo.HostedTests/README.md).

```bash
dotnet test --project tests/ViajantesTurismo.HostedTests --filter-class '*MigrationBaselinePostgreSqlTests'
```

The suite checks fresh expansion, a repeated execution, EF migration history, and relation,
column, constraint, index and trigger definitions against databases initialized by the four EF
contexts and the pinned event-store initializer. It also checks that fresh initialization fails
against an already initialized Management Security database and preserves its existing EF history.
