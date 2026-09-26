# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

`volver-a-base` ("return to base") is a Unity 6 **2D** game about descending a mountain: the player rappels down a cliff on a rope, fights wind gusts that swing them into the rock wall, and manages health while an altitude readout counts down from the Everest summit (8848.86 m) to 0.

- **Engine:** Unity `6000.4.4f1` (open the project folder in this exact editor version).
- **Render pipeline:** Universal RP, 2D. **UI:** UI Toolkit (UXML), not uGUI.
- **Input:** classic `Input` Manager API (`Input.GetAxisRaw`, `Input.GetMouseButton`). The new Input System package is installed but the gameplay code does not use it — match the existing style unless deliberately migrating.

## Working in this repo

- All hand-written code lives in `Assets/Scripts/`. Everything under `Library/`, `Temp/`, `Logs/`, `obj/` and the generated `Assembly-CSharp.csproj` is Unity-generated — never edit it, and ignore `Library/PackageCache/` when searching (it's thousands of vendored package files).
- There is **no CLI build/test/lint**. Build, enter Play mode, and run tests through the Unity Editor GUI (Test Runner window for the `com.unity.test-framework`). There are currently no test files under `Assets/`.
- Scenes and prefabs are Unity YAML — tuning values (`[SerializeField]` fields) are set per-object in the Inspector and serialized into `Assets/Scenes/FirstScene.unity`, so the defaults in code are only fallbacks. The main playable scene is `Assets/Scenes/FirstScene.unity`.
- Git: branches are named `<issue-number>-<slug>` (e.g. `1-player-should-move-like-on-a-mountain`); work is merged to `main` via PRs.

## Architecture

`PlayerMovement` (`Assets/Scripts/PlayerMovement.cs`) is the hub — a `Rigidbody2D`-driven controller with three modes handled in `FixedUpdate`:
1. **Grounded slide/steer** — velocity is driven directly toward a target (mass-independent), with left-mouse "grip" bleeding speed to zero against the icy slope, jump (Space), and a double-left-click "climb hop".
2. **Rope descent** (`DescendOnRope`) — while right mouse is held near an `EdgeAnchor`, gravity is effectively cancelled by writing velocity outright each step; the player hangs under the anchor via a spring pull-to-center and sinks at a capped speed until the rope reaches `maxRopeLength`.

Key cross-component contracts (understanding these requires reading several files together):

- **`PlayerMovement` exposes `IsOnRope` and `CurrentRopeLength`.** `WindZone` reads both to decide whether/how hard to push, and `PlayerMovement.OnCollisionEnter2D` uses rope length to scale swing-impact damage.
- **`WindZone`** (trigger collider) only affects roped players. It uses `AddForce` rather than a velocity write **on purpose**: forces are applied in Unity's physics step *after* every script's `FixedUpdate`, so wind lands on top of the rope code's velocity assignment regardless of script execution order. Force is scaled by `rb.mass` so `windStrength` reads as a plain acceleration. Longer rope = harder sway.
- **`MountainHazard`** is a data-only marker on the mountain's solid collider (min impact speed, base damage, speed multiplier). `PlayerMovement.OnCollisionEnter2D` reads it to size the hit; a swinging rope hit adds bonus damage.
- **`EdgeAnchor`** is a data-only marker on a trigger collider exposing `RopeOrigin`; `PlayerMovement` grabs the nearest one it overlaps.
- **`PlayerHealth`** owns health/invulnerability and fires a `Died` C# `event`. On death it disables the player GameObject.
- **`InGameUIController`** (`[RequireComponent(UIDocument)]`) drives the HUD. It subscribes to `PlayerHealth.Died` (shows the Game Over panel and detaches the Cinemachine follow camera) and each frame updates altitude (derived from vertical travel below the start Y) and health.

### UI Toolkit gotcha

`InGameUIController` looks up elements by **exact name** via `root.Q<Label>("...")`. The names must match `Assets/UI Toolkit/InGameAsset.uxml`: `Health`, `altitude-label`, and the `GameOverScreen` VisualElement. Renaming an element in the UXML without updating the controller (or vice versa) silently breaks the HUD.
