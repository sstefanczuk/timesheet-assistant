# Source conventions

Use English for all project and code content under `src/`, including identifiers, comments, documentation, README files, test names, configuration descriptions, and user-facing text. Apply this rule to new files and updates to existing files.

## Feature-oriented organization

Organize product code by feature under `Features/<FeatureName>/` within each applicable project. Keep a feature's Razor pages/components, form models, domain types, contracts, services and persistence adapters together in its feature folder, subject to project dependency boundaries. Mirror feature folders in test projects. Choose names from actual product responsibilities; examples in conversations are not prescribed feature names.

Do not introduce project-wide `Models/`, `Interfaces/`, `Services/` or similar folders as the primary organization. Start with a flat feature folder; add subfolders only when its size warrants them. Keep code with its owning feature until concrete reuse justifies moving it to a narrowly named shared area; avoid generic dumping grounds such as `Common/` or `Helpers/`.

This convention does not merge projects or dependency layers: portable logic remains in Core, Windows adapters and UI remain in the application, and tests remain in their respective projects. Keep framework-owned structure (`Platforms/`, `Resources/`, `wwwroot/`), app composition (`MauiProgram.cs`), and shared Razor shell/routing (`Components/Layout/`, `Components/Routes.razor`) in their conventional locations. Feature Razor pages may live under `Features/`; update imports/namespaces as needed and retain their routes. Do not move unrelated existing files solely to enforce this convention.

## Unit test readability

Use Shouldly for unit-test assertions. Keep xUnit as the test runner and use Shouldly assertions instead of `Assert.*`.

Separate Arrange, Act and Assert sections with a blank line before `// Act` and `// Assert`.

Every unit test must have three clearly marked sections, in order: `// Arrange`, `// Act`, `// Assert`. Keep the test body focused on the scenario's essential inputs, the operation under test and its expected observable outcome.

Move incidental setup, object graphs, default values, fake configuration and repetitive plumbing into intention-revealing builders, extension methods, private methods or focused fixtures. Keep scenario-defining values and relevant expectations visible in the test; helper names and parameters should explain the setup without requiring the reader to inspect their implementation. Keep the Act operation explicit. Assertion helpers may encapsulate repetitive mechanics, but must make the expected behavior clear at the call site and preserve useful failure messages.

Prefer small feature-local helpers and builders, following the feature-oriented folder convention. Extract shared test infrastructure only for actual reuse; avoid a generic test framework, opaque setup chains or shared mutable state between tests. Do not add abstraction merely to wrap a trivial expression: the goal is a test whose behavior is immediately understandable, with its incidental mechanics elsewhere.

## C# style and spacing

- Name private fields with an underscore and camelCase, including static fields: `_store`, `_today`. Keep constants in PascalCase.
- Always use braces for control-flow bodies, including single statements in `if`, `else`, loops and `using` statements. Use multi-line blocks. A `using var` declaration does not introduce a body and remains allowed.
- Insert a blank line before `return` and `yield return` statements. Do not insert a blank line immediately after an opening brace when a return is the first statement in that block.
- Separate logical steps (validation, preparation, mutation and result construction) with blank lines. Separate members with blank lines. Avoid dense one-line blocks and multiple statements on one line; do not separate every consecutive assignment or assertion mechanically.
- Prefer named result factories that communicate intent, such as `WorkdayResult.Success()` and `WorkdayResult.Failure(WorkdayFailure.NotFound)`, over positional boolean construction such as `new(true)` or `new(false, ...)`. Name the corresponding error property `FailureReason` when `Failure` is the factory name.
- Follow the root `.editorconfig` for machine-enforceable C# style rules. Apply these conventions to new and edited code; avoid unrelated repository-wide formatting.
