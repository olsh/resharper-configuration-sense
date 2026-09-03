# AGENTS.md

Guidance for coding agents working in this repository. `CLAUDE.md` is a symlink to this file, so Claude Code picks it up under the name it looks for.

## Project Overview

Configuration Sense is a JetBrains plugin (ReSharper + Rider) that provides autocomplete and validation for `App settings` and `Connection strings` in .NET projects. It ships as two separate packages: a ReSharper NuGet extension and a Rider IDE plugin, both built from the same C# source.

## Build System

Nuke (`build/Build.cs`) orchestrates the build; `build.cmd` bootstraps it.

**Full build (both packages):**
```sh
./build.cmd pack-resharper pack-rider --configuration Release
```

**Quick compile check:**
```sh
dotnet build Resharper.ConfigurationSense.slnx
```

**Build the Nuke project itself:**
```sh
dotnet build build/_build.csproj
```

**Run the tests (both SDK flavours):**
```sh
./build.cmd test --configuration Release
```

### Nuke targets

Default targets: `PackResharper`, `PackRider`.

- **Restore** - `dotnet restore` for the four projects separately (the two plugin csproj and the two test csproj); `Compile` builds the solution with `EnableNoRestore()`, so a project missing here fails the build
- **Compile** - `dotnet build` the solution, with `VersionPrefix` set to the computed extension version
- **Test** - runs both test assemblies through nunit3-console; results land in `test/results/`
- **PackResharper** - `nuget pack build/Resharper.ConfigurationSense.nuspec`; the `.nupkg` lands in the repo root (this is why stray `*.nupkg` files sit there)
- **PackRider** - invokes `gradlew buildPlugin`, passing all Gradle properties on the command line; output goes to `gradle-build/distributions/*.zip`
- **PublishReSharperPlugin** - `nuget push` of the packed `.nupkg` to `https://plugins.jetbrains.com/`
- **PublishRiderPlugin** - `gradlew publishPlugin`; the Marketplace token goes through the `PUBLISH_TOKEN` environment variable rather than a `-P` property, because Nuke logs tool arguments

Both publish targets require `MarketplaceToken` and are only reached from a manually dispatched CI run. Sonar has no build target - analysis is server-side.

Both pack targets call `PublishExtensionVersion()`, which reports the version to the Nuke summary and, under GitHub Actions, exports `EXTENSION_VERSION` to `$GITHUB_ENV` so the artifact upload steps can name the packages.

### Version derivation

`SdkVersion` in the root `Directory.Build.props` is the single source of truth (e.g. `2026.2.1`). `Build.OnBuildInitialized` reads it off the solution and derives everything else from it:

- **Extension version** = `SdkVersion` locally; on GitHub Actions the run number is spliced in before any prerelease suffix, so `2025.1.0-eap02` becomes `2025.1.0.373-eap02`
- **Wave version** for the nuspec dependency = digits pulled out of the SDK version by regex (`2026.2.x` produces `262.0`)
- **Rider `ProductVersion`** = SDK version with trailing `.0` trimmed, plus a `-SNAPSHOT` suffix mangling for EAP versions

Bumping the SDK therefore only requires editing `Directory.Build.props`.

### Toolchain constraints

- Gradle needs **JDK 21** (`jvmTarget`/`sourceCompatibility` are 21); CI gets it from `actions/setup-java`.
- `build/_build.csproj` targets **net10.0** and pins **Nuke.Common 10.x** - Nuke 10 is the first version that can parse `.slnx` solutions. `NuGet.Frameworks` is pinned to 7.9.0 because Nuke otherwise pulls a 6.x that the .NET 10 SDK targets reject. Don't downgrade either without re-checking `.slnx` parsing.
- The two test projects share `test/src/`, so each pins `OutputPath` to `bin\$(MSBuildProjectName)\$(Configuration)\` to keep the outputs apart. `test/src/app.config` carries the assembly binding redirects nunit3-console needs to load the test assembly, and `test/data/nuget.config` is what lets `[TestPackages]` restore into `test/data/packages`. The test assemblies must stay under `test/src/bin/<project>/<configuration>/` - the framework finds `test/data` by walking up from the assembly.
- `gradle.properties` ships `PluginVersion` and `ProductVersion` as `_PLACEHOLDER_`, and `DotNetOutputDirectory` points at `bin/Debug`. Running `./gradlew buildPlugin` directly produces a broken build - go through `build.cmd pack-rider`, or pass all four `-P` properties yourself. `publishPlugin` additionally reads `PluginChannel` (`default`, or `eap` for a suffixed SDK version) and the `PUBLISH_TOKEN` environment variable.

## Architecture

### Dual-target from one source directory

Two csproj files live **side by side in `src/Resharper.ConfigurationSense/`**:

- `Resharper.ConfigurationSense.csproj` references `JetBrains.ReSharper.SDK`
- `Resharper.ConfigurationSense.Rider.csproj` references `JetBrains.Rider.SDK` (sets `RootNamespace` explicitly, and `SonarQubeExclude` to avoid duplicate analysis)

Both target `net472` and rely on SDK-style implicit globbing, so **a new `.cs` file is automatically compiled into both** - there are no file lists to maintain. Both consume `$(SdkVersion)` and output to `bin/<Configuration>/` (no TFM subfolder, via `AppendTargetFrameworkToOutputPath=false`).

### C#-only scope

`ZoneMarker` requires `ILanguageCSharpZone`, and every completion provider is `[Language(typeof(CSharpLanguage))]`. The plugin does not touch other languages.

### Dispatch on CLR paths

`Constants/ClrTypeConstants.cs` holds the fully-qualified member paths the plugin recognizes (`ConfigurationManager.AppSettings`, `ConfigurationManager.ConnectionStrings`, `IConfiguration`, `GetSection`, `GetValue`, `GetConnectionString`). Both halves of the feature branch on those strings:

- **Analyzers** (`Analyzers/`) - `AccessorSettingsAnalyzer` handles indexer access (`config["Key"]`), `InvocationExpressionSettingsAnalyzer` handles method calls. Each resolves the reference to a CLR path via `TreeNodeExtensions.GetAccessorPath`/`GetMethodPath` (or supertype matching for `IConfiguration`), fetches the matching settings, and emits `SettingsNotFoundHighlighting` when no key matches. Both bail out unless the call has exactly one literal-string argument.
- **SuggestionProviders** (`SuggestionProviders/`) - one `ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>` per API pattern. `IsAvailable` matches the same CLR path constants; `AddLookupItems` delegates to `IGenericSettingsProvider`.

**Adding support for a new configuration API** means: add the CLR path to `ClrTypeConstants`, add a branch in the relevant analyzer, and add a suggestion provider. The settings-reading layer usually needs no change.

### Settings resolution

`Components/GenericSettingsSuggestionProvider` (a `[ShellComponent]` behind `IGenericSettingsProvider`) turns settings into `KeyValueSettingLookupItem`s. The actual reading lives in `Extensions/ProjectExtensions.cs`:

- **XML** (`GetXmlProjectSettings`) - scans `web.config` / `app.config`, follows `configSource` attributes to external config files, and walks the tag tree for key/value attribute pairs (`key`/`value` for appSettings, `name`/`connectionString` for connectionStrings).
- **JSON** (`GetJsonProjectSettings`) - scans `appsettings.json`, flattens the object graph into colon-separated paths (`Logging:LogLevel:Default`), and merges in **user secrets** read from `%APPDATA%/microsoft/UserSecrets/<UserSecretsId>/secrets.json` (the id comes from the csproj `UserSecretsId` tag or legacy `project.json`). Secret reading is wrapped in a swallow-all try/catch by design.
- `JsonSettingType.Value` yields only leaf values (for `GetValue`/indexer completion); `JsonSettingType.All` also yields intermediate objects (for `GetSection`).
- Both paths additionally pick up **dependent files** (transforms such as `web.Release.config`, `appsettings.Development.json`) and any **user-configured additional files**.

### Additional configuration files setting

`Settings/ConfigurationSenseSettings` exposes a single `IIndexedEntry<string, string>` keyed by solution id; the value is a JSON-serialized `HashSet<string>` of project-file persistent ids (`OptionsExtensions` handles (de)serialization and resets the entry on parse failure). The ReSharper options page (`Settings/OptionsPage.cs`) edits it with a `StringCollectionEditViewModel`, filtered to JSON/XML project files.

### Rider frontend

`src/rider/main/kotlin/` contains a `SimpleOptionsPage` subclass, but **it is not registered** - the `applicationConfigurable` entry in `plugin.xml` is commented out because `StringCollectionEditViewModel` isn't supported in Rider. So the Kotlin side is currently inert scaffolding; the Rider plugin is effectively just the backend `.dll`, copied into `<plugin>/dotnet` by Gradle's `prepareSandbox` (which fails the build if the dll/pdb is missing). It still has to compile, though: `SimpleOptionsPage` lives in the `intellij.rider.rdclient.dotnet` bundled module, which `build.gradle` declares explicitly - without that `bundledModule` line `compileKotlin` fails with an unresolved reference. The platform stopped exposing it from the main jars somewhere between 253 and 262, so it is easy to trip over on an SDK bump.

Rider-specific quirk worth knowing: since 2019.1 the Rider backend has no PSI for JSON files, so `ParseJsonProjectFile` falls back to reading document text through `DocumentsOnProjectFiles` when `GetPrimaryPsiFile()` returns null. Keep that fallback when touching JSON reading.

## Testing

NUnit on the JetBrains ReSharper/Rider test infrastructure, in `test/`. The **same sources are
compiled twice** - `test/src/Resharper.ConfigurationSense.Tests.csproj` against
`JetBrains.ReSharper.SDK.Tests` and `test/src/Resharper.ConfigurationSense.Rider.Tests.csproj`
(`RIDER` define) against `JetBrains.Rider.SDK.Tests`. Both are worth running: the Rider backend has
no PSI for JSON, so `ParseJsonProjectFile` falls back to reading document text there, and the Rider
suite is the only thing that exercises that path. `test/src/TestEnvironment.cs` holds the zone
definition and the `[SetUpFixture]`; adding `JetBrains.ReSharper.SDK.Tests` is what copies the whole
backend into the test output, which is why there is no NUnit `PackageReference`.

Name fixtures `*Tests` and methods `Test<Scenario>`. `DoNamedTest2()` resolves `<Scenario>.cs` (the
`Test` prefix is dropped) from `test/data/<Category>/<Feature>/`, and **its arguments are extra files
added to the test solution** - that is how a configuration file reaches the analyzer:

```csharp
[Test] public void TestMissingAppSetting() => DoNamedTest2("app.config");
```

`ProjectExtensions` matches configuration files by exact name, so a data folder can hold only one
`app.config`, one `web.config` and one `appsettings.json`. The folder, not the test method, is
therefore the unit of configuration content - keep the "found" and "not found" cases side by side in
one folder sharing one file, and start a new folder when you need different content.

Analyzer fixtures derive from `SettingsAnalyzerTestBase`, whose `HighlightingPredicate` narrows the
gold to `SettingsNotFoundHighlighting`; without that every unrelated platform inspection would show
up. Completion fixtures derive from `SettingsCompletionTestBase`, which uses
`CodeCompletionTestType.ModernList` (`List` is obsolete) and filters to `KeyValueSettingLookupItem`.
Both bases carry `[TestNet60]` plus `[TestPackages]` for `System.Configuration.ConfigurationManager`,
`Microsoft.Extensions.Configuration.Abstractions` and `.Binder` - the analyzers dispatch on
fully-qualified CLR paths, so those types have to resolve. `[TestNetFramework46]` does **not** work:
the `net462` asset of `System.Configuration.ConfigurationManager` is a facade with no types in it.

**Never hand-write a `.cs.gold` file.** Create it empty, run the test, read the actual output the
framework writes next to it as `.cs.tmp`, check the behaviour is what you meant, then rename the
`.tmp` over the `.gold`. Marker placement, the separator width and the encoding (UTF-8 with BOM,
CRLF) are framework details.

Not covered, because the test framework cannot model them: **dependent files**
(`web.Release.config`, `appsettings.Development.json` - they need `DependentUpon` metadata, and test
projects are built from a flat file set), **user secrets** (`ReadSecretsSafe` reads `%APPDATA%`
directly and swallows every exception, so it is inert in tests) and the **additional configuration
files setting** (the stored value is a project file's runtime persistent ID).

Run `build.cmd test` before submitting.

## Conventions

`.editorconfig` sets 4-space indent for C# (2 for csproj/json/yml/xml/props), `System` usings first, and **blank-line-separated using groups** - existing files follow this closely, so match it.

## CI

GitHub Actions (`.github/workflows/build.yml`), `windows-2025`, .NET 10 and Temurin 21. On every push and pull request against `master` it runs `build.cmd Test --configuration Release`, then `build.cmd PackResharper PackRider --configuration Release`, and uploads the NUnit result files, the `.nupkg` and the Rider `.zip` as run artifacts; the two packages are named after `EXTENSION_VERSION`.

A manual `workflow_dispatch` with `publish: true` additionally runs the two publish targets and then cuts a GitHub release tagged with the extension version (`--prerelease` when the version carries an EAP suffix). That path needs the `JETBRAINS_MARKETPLACE_TOKEN` repository secret; nothing else in the workflow needs a secret.

SonarQube analysis is **server-side** - the scanner does not run in CI, and there is no Sonar target in the Nuke build.

`.github/workflows/dependabot-auto-merge.yml` auto-approves and auto-merges patch and minor Dependabot updates. It relies on `master` requiring the `Build and test` status check - GitHub's auto-merge only waits for *required* checks, so without that branch protection rule a red update merges immediately (which is how the Gradle plugin bump in #87 broke the build). The required context is the CI job name, so renaming the job means updating the rule. Dependabot (`.github/dependabot.yml`) covers Gradle, the `build/` NuGet packages and the workflow actions.
