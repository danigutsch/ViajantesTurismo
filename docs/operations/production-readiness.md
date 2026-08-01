# Production readiness and operations

This runbook defines the current minimum production-readiness baseline for the Aspire-hosted
application. It is intentionally platform-neutral until a target hosting platform is selected.

## Deployment and promotion model

Use the AppHost model as the source of truth for service relationships, dependency order, and
health probes. Use the release workflow as the source of truth for version calculation and artifact
publication.

Promotion order:

1. Build and test the commit that will be released.
2. Calculate the release version with the existing release tooling.
3. Publish immutable application artifacts and container images for that version.
4. Deploy the same artifact version to each environment; do not rebuild between environments.
5. Apply migrations before starting dependent API and web services, preserving the AppHost ordering.
6. Run smoke checks against the deployed endpoints before promoting to the next environment.

Target environment assumptions:

- The first production deployment target is not selected yet.
- Each environment owns its secrets, connection strings, DNS names, and platform resource names.
- Application artifacts are immutable between environments.
- The migration service is the deployment gate for database schema changes.
- Public Web and Management Web are externally reachable; backend APIs should be private unless the
  selected platform requires a controlled ingress path for smoke checks.

Configuration boundaries:

- Secrets and connection strings belong in the deployment platform or managed secret store.
- Runtime options documented in [Configuration Standards](../CONFIGURATION.md) may be promoted as app settings or environment variables.
- Local AppHost values stay local unless operators explicitly need an environment-specific setting.
- Image tags and digests for infrastructure resources stay pinned independently from application versions.

## Health and readiness strategy

Every ASP.NET Core service that calls `MapDefaultEndpoints()` exposes:

| Endpoint | Scope | Expected use | Public detail |
| --- | --- | --- | --- |
| `/alive` | liveness-only checks tagged `live` | restart unhealthy process | status text only |
| `/health` | full health/readiness checks | deployment gate, load-balancer readiness, smoke checks | status text only |

Production policy:

- Keep health payloads terse; do not expose exception details, connection strings, dependency names with secrets, or stack traces.
- Add dependency checks only when the service cannot safely accept traffic without that dependency.
- Use `/alive` for process restarts; use `/health` for readiness and promotion gates.
- Keep backend APIs internal in production where the hosting platform supports private service networking.
- Treat a failing `/health` after deployment as a failed promotion until triaged.

## Smoke-test strategy

Run smoke checks after deployment and before promotion. Checks must be read-only unless a future
runbook explicitly marks a test account and cleanup path as production-safe.

Minimum scope:

| Surface | Check | Expected result |
| --- | --- | --- |
| Public Web | `GET /` and `GET /health` | page returns success; health is `200 Healthy` |
| Management Web | `GET /health` and auth shell route | health is `200 Healthy`; protected UI does not allow anonymous data access |
| Admin API | `GET /health` through internal or management network | `200 Healthy` |
| Catalog API | `GET /health` through internal or management network | `200 Healthy` |
| Worker dependencies | API health after migration and worker start | no readiness failure caused by database or transport setup |

Manual smoke path:

1. Confirm deployed artifact version matches the release version.
2. Call `/alive` on each reachable service.
3. Call `/health` on each reachable service.
4. Open the public web home page.
5. Open the management web entry point and verify authentication is required before protected data is shown.
6. Review structured logs for startup errors, migration failures, and repeated dependency retries.

CI and release path:

- Keep smoke checks read-only by default.
- Run the same checks against local Aspire before release when practical.
- Run deployed-environment smoke checks after migration completion and before traffic promotion.
- Use internal networking or a temporary restricted runner path for backend API health checks.
- Do not add production data mutations until a test account, cleanup rule, and audit trail exist.

Worker dependency validation is indirect today: verify migration completion, API readiness, and worker
startup logs. Add a direct worker smoke probe only after a production-safe queue or message transport
check is defined.

## Backup, restore, and disaster recovery

Persistent inventory:

| Resource | Current source | Backup scope |
| --- | --- | --- |
| Admin database | AppHost PostgreSQL `admin-database` | schema, data, migration history |
| Catalog database | AppHost PostgreSQL `catalog-database` | schema, data, migration history |
| Redis cache | AppHost Redis `cache` | no durable backup expected unless promoted to durable session state |
| Media assets | Catalog/media application paths | original files, variants, metadata, malware-scan state |
| Release artifacts | release workflow output | immutable images, tags, digests, SBOM/provenance when available |

Minimum backup expectations until platform selection:

- Provision the Admin PostgreSQL database with `LC_CTYPE` set to `C.UTF-8`, `C.utf8`,
  `en_US.UTF-8`, or `en_US.utf8`. The initial migration rejects other locales because Customer email
  uniqueness uses `citext`, whose case folding is fixed when the database is created.
- Back up PostgreSQL before destructive migrations and before production promotion.
- Retain enough restore points to return to the previous known-good release window.
- Store backups outside the application runtime environment.
- Treat media assets as durable business data; back up metadata and object bytes together.
- Test restore into a non-production environment before trusting a backup policy.

Restore validation:

1. Restore database and media into an isolated environment.
2. Run migration status checks without applying unapproved forward migrations.
3. Start APIs and web frontends against the restored data.
4. Run the smoke-test strategy.
5. Record restore duration, restored artifact IDs, backup timestamp, and validation result.

Follow-up implementation issues are required after the hosting platform and recovery objectives are
chosen. No hard RPO, RTO, frequency, retention period, or backup product is encoded here because those
are product and operations decisions.

The [document retention and legal-hold policy proposal](document-retention-and-legal-hold.md) defines
the additional decisions required for generated artifacts, audit records, held data, backup expiry,
and restore-time suppression. It remains pending legal review and does not establish production
authority.

## Common failures

### Migration failure

Signals:

- Migration service exits with failure.
- API services do not become ready.
- `/health` stays unhealthy or unavailable.

Actions:

1. Stop promotion.
2. Inspect migration logs first, then database connectivity and applied migration state.
3. Do not start dependent API services against a partially migrated database.
4. Restore the previous database state if the failed migration made destructive changes and a verified backup exists.
5. Redeploy the previous known-good application version if the migration is not required for rollback compatibility.

### Database unavailable

Signals:

- API `/health` fails.
- Worker logs show connection or retry failures.
- Public or management pages fail when data is required.

Actions:

1. Check database platform health and connection-secret validity.
2. Check network rules between services and the database.
3. Keep services deployed but out of rotation until `/health` recovers.
4. If recovery exceeds the incident threshold, start the restore plan for the owning database.

### Cache unavailable

Signals:

- Management Web `/health` fails if cache is required.
- Management UI sessions or cached data fail.

Actions:

1. Check cache platform health and connection-secret validity.
2. Restart only the cache resource if the platform supports safe restart.
3. Restart Management Web after cache recovery if stale connections persist.

### API dependency unavailable

Signals:

- Public Web or Management Web `/health` fails.
- Frontend logs show service-discovery or HTTP dependency failures.

Actions:

1. Check the dependent API `/health` directly from the internal network.
2. Check service discovery and configured endpoint references.
3. Restore the dependent API before restarting frontend services.

## Rollback

Rollback target:

- Prefer redeploying the previous known-good immutable application artifact.
- Preserve database state unless the new migration is incompatible with the previous version.
- Use verified backups only for database rollback or restore.

Rollback steps:

1. Stop promotion and mark the new version unhealthy.
2. Remove the new version from traffic.
3. Deploy the previous known-good application version.
4. If required, restore database backup or run an approved corrective migration.
5. Run the smoke-test strategy again.
6. Keep incident notes with release version, commit SHA, artifact IDs, migration state, and operator actions.

Follow-up needed after target platform selection:

- Document exact hosting environment names, traffic-shift commands, and backup tooling.
- Add platform-specific restore validation steps.
- Automate the read-only smoke checks in CI or release orchestration.
- Create implementation issues for agreed RPO, RTO, retention, restore drills, and media backup tooling.
