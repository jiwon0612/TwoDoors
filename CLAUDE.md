# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

Unity **6000.3.21f1** project using **URP 17.3** (PC and Mobile RP assets in `Assets/Settings/`). The repo is still close to the URP template: the only scene is `Assets/Scenes/SampleScene.unity`, and `Assets/TutorialInfo/` is the template's readme helper and can be deleted. There are no asmdefs, so everything compiles into Assembly-CSharp. Code lives in two roots:
- `Assets/TwoDoors/` (김우진): characters (`Scripts/Characters/`, namespace `TwoDoors.Characters`), prefabs (Player, Zombie, Mannequin, Golem), the `CharacterTest` scene, and `Editor/CharacterSceneBuilder.cs` (menu **TwoDoors > Create Character Test Scene**, which generates the scene, prefabs, materials and animations). See `Assets/TwoDoors/README.txt` for controls and tuning values.
- `Assets/CJW/` (최지원): code in `Code/` (room event system, editor tools in `Code/Editor/`), generated data in `Data/`.

## Characters (`TwoDoors.Characters`)

- `CharacterBase` holds **int** HP. `TakeDamage(int)`/`Heal(int)` return the amount applied and ignore dead characters. `Died` is `Action<CharacterBase>`. Subclasses override `OnDeath()` and must call `base.Awake()`.
- `PlayerCharacter` reads `Keyboard.current`/`Mouse.current` directly. Esc toggles `SetPaused`, which sets `Time.timeScale = 0`, so anything driven by `Time.deltaTime` pauses for free. The player prefab is tagged `Player`.
- `EnemyCharacter` uses NavMeshAgent to chase and melee the player. It only moves on a baked NavMesh, stays idle beyond `detectionRadius` (14), and finds the player in `Start()` (`SetTarget` overrides this). `ZombieEnemy`, `MannequinEnemy` and `GolemEnemy` are empty subclasses whose stats live on the prefabs.

- **Input:** `activeInputHandler: 1` means the **new Input System only**. `UnityEngine.Input.GetKey`/`GetAxis` will throw. Use `Assets/InputSystem_Actions.inputactions`, which is registered as the project-wide actions asset and already has Player/UI maps with Move, Look and so on. You can also use `Keyboard.current`/`Mouse.current`.
- **Packages of note:** AI Navigation (NavMesh, which fits zombie chasing), uGUI, Timeline, Test Framework.
- **Git:** `Library/`, `Temp/`, `Logs/`, `UserSettings/` and the generated `.csproj`/`.sln` files are not tracked. Always commit `.meta` files together with their assets. Scenes and prefabs are YAML and merge badly, so the team rule is to coordinate before two people edit the same file.

## Commands

You don't need a build step to edit code. Unity recompiles when the Editor regains focus. To run things from the CLI, close the Editor first because the project is locked while it's open. Adjust the path if Unity Hub installed the Editor elsewhere:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.21f1/Editor/Unity.exe" -batchmode -quit -projectPath . -logFile - 
```

- **Tests:** add `-runTests -testPlatform EditMode` (or `PlayMode`) with `-testResults results.xml`, and leave out `-quit`. To run a single test, add `-testFilter "Namespace.Class.Method"`. Test assemblies need an asmdef that references `UnityEngine.TestRunner`/`UnityEditor.TestRunner`.
- **Build:** there is no build script yet. Builds are made through File > Build Profiles (Windows standalone). The scenes in the build list are in `ProjectSettings/EditorBuildSettings.asset`.

## Room event system (`Assets/CJW/Code/Events/`, namespace `TwoDoors.Events`)

A door choice is a `StageChoice` ScriptableObject that holds two `RoomEvent` assets (red and blue).
- `RoomEvent` (abstract SO) owns every **entry effect** shared by all events: HP ±, lights off, zombie count and an optional prefab override, skip next stage, escape on clear. Subclasses only define the **clear condition**: `InstantEvent` (clears immediately), `SurvivalEvent` (survive N seconds), `HealZoneEvent : SurvivalEvent` (also turns on a heal zone), `ButtonEvent` (press N buttons). Add a new subclass only when a new clear condition appears. Variants like special zombies or "dark + heal" are just different field values or prefabs on an existing class.
- Event assets are **stateless**, because one asset is reused across runs. Elapsed time lives in `RoomEventRunner`, and room state lives in the scene.
- Events touch the world only through `IRoomContext`, which is implemented by `RoomController` on the single reusable room. `ResetRoom()` reverts room-only effects. HP persists. `ChangeHealth(float)` buffers fractions (from the heal zone) into whole-number `Heal`/`TakeDamage` calls on `PlayerCharacter`. Zombie prefabs are `EnemyCharacter` references. Buttons and the heal zone detect the player by the `Player` tag.
- `RoomEventRunner.Run(event)` fires `Completed`. Stage progression reads `SkipNextStage` and `EscapeOnClear` from the completed event. When the player dies, the runner stops without firing `Completed`. If an entry effect kills the player, cleanup is deferred until `Begin()` finishes.
- Running **Tools > TwoDoors > Generate Stage Choices** creates the designed stage assets under `Assets/CJW/Data/Stages/`. It never overwrites existing assets, so tune numbers in the inspector. Door label text comes from `RoomEvent.GetDescription()`, which is auto-built unless `customDescription` is set.

## Stage flow (`Assets/CJW/Code/Stages/`, namespace `TwoDoors.Stages`)

`StageManager` holds the ordered `StageChoice[]` and a phase (`Choosing` → `Running` → back to `Choosing`, or `Escaped`/`GameOver`). When the player enters an open `StageDoor` trigger, it calls `ChooseDoor`. That closes both doors, teleports the player to `playerSpawnPoint` (with the CharacterController disabled during the move, since the room is reused), and runs the event. When the event completes, the manager advances 1 stage (2 if `SkipNextStage`). Escape happens when `EscapeOnClear` is set or after the last stage. Death sets the phase to GameOver. `Restart()` reloads the scene. HUD can use the events `StageOpened`, `DoorChosen`, `Escaped` and `GameOver`, plus `StageNumber`/`TotalStages` and `RoomEventRunner.RemainingTime`.

## Game design (from 기획서 "두 개의 문")

A single-player, first-person PC survival game based on *Red or Blue*. Each stage has a **red door and a blue door**, each with a written trade-off. Walking through a door commits the choice and applies its effect to the **next room**.

Core rules the systems must enforce:
- HP starts at 100 and is clamped to a max of 100. HP **carries across stages**. At 0 HP the game is over and restarts from the beginning.
- There are about 6–8 stages. Passing the last door alive means the player escapes.
- In a zombie room, the player must survive N seconds before the next doors open. An event-less room goes straight to the next choice.
- Lighting and zombie changes apply to **that room only**. HP changes persist.
- Effects are limited to **HP ±, zombie count + survive time, lights off, and skip next stage**. Examples: red = +30 HP & 1 zombie for 15s, blue = −10 HP & no zombie. Red = −30 HP & skip a stage. The numbers are placeholders that will be tuned in playtesting, so keep them data-driven (ScriptableObject or similar), not hard-coded.
- Scope limits: **one reusable base room** that gets reconfigured each stage rather than a scene per stage, **one zombie type** that chases and deals damage on contact, and no weapons or combat.
- Controls: WASD to move, mouse to look, doors open automatically when the player is near, Esc to pause or return to the title screen.
- HUD: current HP, stage number, and remaining survival time in zombie rooms. Door conditions are shown above each door. Damage and healing get screen and sound feedback. There are start and end screens.

Ownership: 김우진 handles movement, door passing, zombie chase and contact, the base room and early stages. 최지원 (this repo's git user) handles HP, applying choice effects, survival timer, stage progression, late stages and wiring the game together. 윤완희 handles UI, HUD, screens, lighting and SFX polish. The deadline is 2026-10-13, and the deliverable is a Windows executable.
