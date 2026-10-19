# Architecture

## Approved foundation

Idlebound targets Android and iOS with a mobile-first 2D Unity client. The editor is pinned to Unity 6.3 LTS `6000.3.26f1`. C# is the gameplay implementation language. Rendering uses the Universal Render Pipeline with its 2D renderer. Git tracks source assets, their metadata, package definitions, and project settings.

Supabase is the planned backend. Its integration is future work: there is no backend client, schema, authentication flow, or persistence implementation in this template. Concrete backend responsibilities and interfaces require approved requirements before implementation. Privileged backend credentials must not be committed or embedded in a distributed client.

## Current repository

The repository contains the Universal 2D foundation: SampleScene, rendering settings, input actions, a Lit 2D scene template, package definitions, and Unity project configuration. No project-authored C# gameplay systems are present. Documentation is stored outside Assets in `Docs`.

## Future data-driven direction

Future gameplay should follow the approved data-driven direction, with game definitions and tuning data kept distinct from the C# behavior that consumes them. Specific data formats, asset types, schemas, validation, and ownership boundaries remain to be approved. This document does not prescribe additional packages or a folder hierarchy for unimplemented systems.

Progression formulas, reward values, offline progression behavior, synchronization rules, conflict handling, and client/server authority are not defined here. They require explicit design approval before implementation. No mechanics are established or changed by this verification task.
