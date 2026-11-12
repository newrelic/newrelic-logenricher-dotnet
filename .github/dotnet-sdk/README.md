# .NET SDK pin for CI

`global.json` in this folder is the exact .NET SDK that CI builds and tests with. The workflows install it with the agent repository's `setup-pinned-dotnet` action; it is not at the repository root, so local builds use whatever SDK you have installed.

**The SDK's major version must match the test projects' target framework** (currently `net10.0` in `tests/*/*.csproj`). The action installs only the pinned SDK and its runtime, so tests targeting another .NET version would build but could not run.

Dependabot updates the patch version here and ignores new major versions. Moving to a newer .NET version is a separate change: retarget the test projects and update this pin in the same pull request.
