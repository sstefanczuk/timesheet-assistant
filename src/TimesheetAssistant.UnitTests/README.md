# TimesheetAssistant.UnitTests

xUnit test project targeting .NET 10. Run from the repository root:

```powershell
dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj
```

Tests under Features/Workdays exercise shared TimesheetAssistant.Core production rules: duration grammar and formatting, normalized fields, local-date validation, totals and service CRUD/failure behavior through an in-memory fake store. The suite uses no Playwright, WebView2, EXE, native SQLite or real storage. Every test uses explicit Arrange/Act/Assert sections. Desktop workflows and actual persistence belong to the separate TimesheetAssistant.UiTests project.

CI runs this project in Release before packaging:

```powershell
dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-build --logger trx
```

`--no-build` assumes the solution has already been built in Release. No application
path or WebView2 configuration is required. The deferred desktop release gate
candidate runs UiTests against the published EXE after packaging and checks its own TRX for at
least one executed test; pure unit tests cannot satisfy that desktop gate. See
`../TimesheetAssistant.UiTests/README.md` for commands and diagnostic artifacts.
Main currently retains build/package/release without that desktop gate; keep
running UiTests locally after packaging. A green main build does not establish
desktop CI acceptance.
