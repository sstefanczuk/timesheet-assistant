# TimesheetAssistant.UnitTests

xUnit test project targeting .NET 10. Run from the repository root:

```powershell
dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj
```

The project has no tests yet. It is reserved for pure logic without Playwright, WebView2, an EXE or real storage. Desktop workflows belong to the separate TimesheetAssistant.UiTests project. Empty discovery does not verify application behavior.

CI runs this project in Release before packaging:

```powershell
dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-build --logger trx
```

`--no-build` assumes the solution has already been built in Release. No application
path or WebView2 configuration is required. The deferred desktop release gate
candidate runs UiTests against the published EXE after packaging and checks its own TRX for at
least one executed test; an empty unit suite cannot satisfy that gate. See
`../TimesheetAssistant.UiTests/README.md` for commands and diagnostic artifacts.
Main currently retains build/package/release without that desktop gate; keep
running UiTests locally after packaging. A green main build does not establish
desktop CI acceptance.
