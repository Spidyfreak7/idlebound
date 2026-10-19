# Idlebound

A 2D mobile-first idle RPG targeting Android and iOS, using Unity, C#, and a planned Supabase backend.

## Requirements

- Unity Hub and Unity **6000.3.26f1** (Unity 6.3 LTS), matching `ProjectSettings/ProjectVersion.txt`.
- Git and a C# IDE compatible with Unity.
- For Android builds: the matching editor's Android Build Support, SDK/NDK Tools, and OpenJDK modules installed through Unity Hub.
- For iOS builds: iOS Build Support; final compilation and signing require macOS with a compatible Xcode installation and signing configuration.

## Open locally

1. Clone `https://github.com/Spidyfreak7/idlebound.git` or update your existing checkout.
2. In Unity Hub, choose Add project from disk and select the repository root containing `Assets`, `Packages`, and `ProjectSettings`.
3. Open with **6000.3.26f1**. Allow Unity to resolve the committed package manifest/lockfile and import assets. Do not upgrade the editor or packages as part of verification.
4. Open `Assets/Scenes/SampleScene.unity`. Check the Console for compilation/import errors, enter Play mode, and inspect the scene.
5. Open Window > General > Test Runner to inspect EditMode and PlayMode tests. No project-authored C# tests currently exist; zero tests is not a passing gameplay test.
6. For mobile validation, use File > Build Profiles to inspect Android/iOS support and perform a development build on the appropriate host. Device behavior and signing still require separate verification.

No Supabase setup is required to inspect the current template. Backend configuration and integration are not implemented.

## Project structure

- `Assets/Scenes/`: initial `SampleScene.unity`, enabled in build settings.
- `Assets/Settings/`: Universal Render Pipeline asset, 2D renderer, volume profile, input actions, and Lit 2D scene template with its template scene.
- `Packages/`: committed manifest and dependency lockfile; includes URP 17.3.0 and Unity Test Framework 1.6.0.
- `ProjectSettings/`: committed editor, rendering, input, and other project configuration.
- `Docs/DevelopmentProgress.md`: stage status and outstanding validation.
- `Docs/Architecture.md`: approved architecture scope and deferred decisions.

## Git conventions and verification

Commit `Assets` together with their `.meta` files, `Packages`, and `ProjectSettings`. Preserve existing GUIDs. Visible Meta Files and text serialization are configured. `.gitignore` excludes generated Library, Temp, Obj, build, log, user-settings, and IDE outputs while retaining project source and metadata.

Static verification confirmed the editor pin, valid package JSON, tracked asset metadata, ignore coverage, and URP 2D configuration. The cloud environment has no available Unity Editor, so compilation, package import, scene execution, and mobile builds have not been verified. Use the local steps above before treating the project as runtime-validated.

Cloud tasks should use the existing isolated checkout; no extra Git worktree is needed.
