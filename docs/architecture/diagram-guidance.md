# Diagram guidance

Use diagrams when they clarify a boundary, workflow, data shape, or deployment view better than prose.
Keep each diagram narrow enough to review in Markdown and refresh with repository tooling when the
source is discoverable.

## Diagram type selection

| Diagram type | Use when | Avoid when | GitHub and Mermaid notes |
| --- | --- | --- | --- |
| C4 System Context | Showing users, external systems, and the application boundary. | Explaining services, databases, or code internals. | Prefer stable Mermaid `flowchart` with C4-like labels. Mermaid C4 syntax is experimental. |
| C4 Container | Showing web apps, APIs, workers, databases, messaging, and major technologies. | Showing deployment nodes, replicas, or detailed classes. | Best default for repository architecture. Use generated `flowchart` unless C4 rendering is verified. |
| C4 Component | Showing one container's internal modules and responsibilities. | Dumping volatile code structure. | Use only for durable components with a maintainer audience. |
| C4 Dynamic | Showing one cross-container use case or message flow. | Documenting simple CRUD. | Mermaid `sequenceDiagram` is usually more stable on GitHub. |
| C4 Deployment | Showing environment topology, runtime nodes, and network boundaries. | Explaining logical architecture. | Use one environment per diagram. Keep AppHost diagrams in local runtime docs. |
| Mermaid flowchart | Showing dependencies, boundaries, process steps, and generated maps. | Needing exact UML/C4 semantics. | Most portable GitHub option; avoid fragile syntax. |
| Mermaid sequence | Showing request flow, async flow, retries, and handshakes. | Static dependency maps. | One scenario per diagram. Long sequences become hard to review. |
| Mermaid ER | Showing relational or logical data shape. | Non-data architecture. | Good for database and read-model docs. |
| Mermaid state | Showing lifecycles such as booking, payment, import, or retry state. | Linear flows without meaningful states. | Keep state names finite and domain-owned. |
| Structurizr DSL | Maintaining one architecture model with many C4 views. | One-off diagrams. | Consider only after generated Markdown diagrams become too hard to maintain. |

## Source-of-truth policy

- **Project dependencies**: generate from `*.csproj` project references.
- **SharedKernel dependencies**: generate from `src/SharedKernel/**/*.csproj` project references.
- **Local Aspire runtime wiring**: generate from AppHost source for local orchestration docs only.
- **GitHub workflow maps**: generate from `.github/workflows/*.yml`.
- **System overview**: keep curated in `docs/architecture/generated-diagrams.json` because trust
  boundaries, actors, external dependencies, PII classification, and planned nodes are architectural
  interpretation rather than safe code inference.
- **Endpoint maps**: generate from Minimal API route declarations and configured route-group prefixes.
- **Event-flow maps**: generate from integration-event contracts, source-created events, consumer
  registrations, and handlers. Missing producers stay explicit instead of inferred.
- **Data ownership maps**: generate after DbContext/store ownership metadata is standardized.

## Refresh path

The repository-owned config is `docs/architecture/generated-diagrams.json`. Refresh diagrams with:

```text
bash scripts/update-architecture-diagrams.sh
```

Check drift with:

```text
bash scripts/update-architecture-diagrams.sh --check
```

The script invokes `tools/SharedKernel.Documentation.Tool`, which delegates reusable generation logic
to `src/SharedKernel/SharedKernel.Documentation`.

## References

- C4 model: <https://c4model.com/>
- Mermaid syntax reference: <https://mermaid.js.org/intro/syntax-reference.html>
- Mermaid flowcharts: <https://mermaid.js.org/syntax/flowchart.html>
- Mermaid sequence diagrams: <https://mermaid.js.org/syntax/sequenceDiagram.html>
- Mermaid ER diagrams: <https://mermaid.js.org/syntax/entityRelationshipDiagram.html>
- Mermaid state diagrams: <https://mermaid.js.org/syntax/stateDiagram.html>
- GitHub Mermaid rendering: <https://docs.github.com/en/get-started/writing-on-github/working-with-advanced-formatting/creating-diagrams>
- Structurizr DSL: <https://docs.structurizr.com/dsl/language>
- .NET Aspire AppHost overview: <https://learn.microsoft.com/dotnet/aspire/fundamentals/app-host-overview>
