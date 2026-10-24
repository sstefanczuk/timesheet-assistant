# TimesheetAssistant.Tests

xUnit test project targeting .NET 10. Run from the repository root:

```powershell
dotnet test src/TimesheetAssistant.Tests/TimesheetAssistant.Tests.csproj
```

The project does not contain any tests yet. It has no reference to the MAUI application: UI tests are intended to control its WebView2 through Playwright .NET, as described in `context/foundation/tech-stack.md`. Playwright configuration and application tests have not been implemented yet.
