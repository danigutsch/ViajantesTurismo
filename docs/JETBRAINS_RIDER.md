# JetBrains Rider repository policy

This repository supports JetBrains Rider and other IntelliJ-based IDEs without making IDE
state part of normal code changes.

## Commit

- `*.sln.DotSettings` when settings are intentionally team-shared.
- Shareable `.idea` project metadata that is not user-specific, generated, sensitive, or
  high churn.
- `.idea/**/.name` when `.idea` metadata is shared; JetBrains uses it as the project display
  name.

## Do not commit

- `*.sln.DotSettings.user` personal Rider/ReSharper overrides.
- `.idea/**/workspace.xml`, `tasks.xml`, `usage.statistics.xml`, `dictionaries/`, and
  `shelf/`.
- Generated, sensitive, or high-churn `.idea` files such as `contentModel.xml`, `dataSources/`,
  `dataSources.local.xml`, `sqlDataSources.xml`, `dynamic.xml`, `httpRequests/`, `caches/`, and
  plugin-local state.
- `http-client.private.env.json` or other local HTTP-client secrets.

## C#/.NET settings

Use `.editorconfig` for portable code style because Rider, Visual Studio, and VS Code all read
it. Use `ViajantesTurismo.sln.DotSettings` only for Rider/ReSharper team settings that cannot
be expressed well in `.editorconfig`. Keep personal overrides in
`ViajantesTurismo.sln.DotSettings.user`.

## Sources

- JetBrains project VCS guidance: <https://intellij-support.jetbrains.com/hc/en-us/articles/206544839-How-to-manage-projects-under-Version-Control-Systems>
- JetBrains Rider layer-based settings: <https://www.jetbrains.com/help/rider/Sharing_Configuration_Options.html>
- GitHub JetBrains ignore template: <https://github.com/github/gitignore/blob/main/Global/JetBrains.gitignore>
- GitHub Visual Studio ignore template: <https://github.com/github/gitignore/blob/main/VisualStudio.gitignore>
