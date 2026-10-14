# Sabotrix

Turn-based 1v1 card battler set in a nerd bar, where Sabotage cards mess with the room itself. Built in Unity as a father/son learning project.

- **Malcolm (12)** is Game Director. He owns rules, cards, heroes, and art direction.
- **Brian** is Tech Lead. He is a senior web developer and **new to Unity**.

## Source of Truth

- `docs/game-design-document.md` (GDD) defines the game. Do not invent rules, cards, or numbers. If the GDD is silent or ambiguous, ask instead of guessing.
- When a rule changes, update the GDD in the same change.
- `docs/m2-plan.md` is the current milestone plan (`docs/m1-plan.md` is done). Its **Status** table says what's done and what's next. Update it as slices finish.

## Working With This Team

- This is a learning project. When you introduce a Unity concept for the first time (ScriptableObject, prefab, assembly definition, etc.), explain it in a sentence or two.
- This is a Unity game. Do not suggest web technologies.
- Build in small slices that can be played or tested, so Brian and Malcolm can see progress in the editor.

## Environment

| Item | Value |
|:--|:--|
| Unity | 6000.3.26f1 (Unity 6.3 LTS), Apple silicon |
| Editor binary | `/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity` |
| Render pipeline | URP (`Assets/Settings/PC_RPAsset.asset`) |
| Input | Input System package (not legacy Input Manager) |
| UI | uGUI |
| Build target | Windows 64-bit (Mono), built from the Mac |

## Unity MCP (editor open)

When the editor is open, use the `unityMCP` tools ([MCP for Unity](https://github.com/CoplayDev/unity-mcp), pinned in `Packages/manifest.json`) instead of batch mode:

- `refresh_unity`: import assets and compile scripts
- `read_console`: check for compile errors and logs
- `run_tests` + `get_test_job`: run EditMode tests (enable the `testing` group with `manage_tools` if needed)

Also useful:

- `manage_scriptable_object`: create/modify data assets with patches like `{"propertyPath": "_cost", "op": "set", "value": 2}`. Enums are set by index. Object references use `"ref": {"guid": "..."}`. List entries can be set directly (`_cards.Array.data[3].Card`); setting `Array.size` is not supported but lists grow automatically.
- `manage_scene`, `manage_gameobject`, `manage_components`: build and wire scenes. Save with `manage_scene` `save`.
- `manage_editor` `play`/`stop` + `manage_camera` `screenshot` (with `include_image`, `output_folder: Temp/Screenshots`): check the game visually.
- `execute_code`: drive the running game, e.g. invoke a UI button's `onClick`, or call `GameController` methods through reflection to auto-play a match. Use `UnityEngine.Object` explicitly (plain `Object` is ambiguous). Play mode stops ticking when the editor isn't focused: set `Application.runInBackground = true` first so time, blends, and coroutines advance. The Game view also stops repainting, so a plain `screenshot` can return a stale frame; pass `camera: "Main Camera"` for a fresh render (it leaves out Screen Space - Overlay UI).
- `manage_build` `build` with `target: windows64`: Windows build while the editor stays open. Wait for `Build Finished, Result` in `~/Library/Logs/Unity/Editor.log`.

All `mcp__UnityMCP` tools are pre-approved in `.claude/settings.local.json`.

The server runs inside the editor (HTTP, `127.0.0.1:8080`). Start it from **Window → MCP for Unity**. Each machine registers it with Claude Code once, through that window's client setup or with `claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp`.

## Unity CLI (editor closed)

**Only one Unity process can open the project at a time.** If the editor is open, batch mode fails. Before running a batch command, check for a running editor with `pgrep -f "Unity.app/Contents/MacOS/Unity" >/dev/null && echo running` (don't print the process args; they contain an access token). If it is running, ask Brian to close it.

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity

# Import assets + compile (also generates .meta files for new files)
"$UNITY" -batchmode -quit -projectPath . -logFile - | grep -E "error CS|Compilation failed"

# Run EditMode tests (do NOT pass -quit with -runTests)
"$UNITY" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml -logFile Logs/editmode.log

# Windows build
"$UNITY" -batchmode -quit -projectPath . -buildTarget Win64 -buildWindows64Player Builds/Windows/Sabotrix.exe -logFile Logs/build.log
```

## Project Layout

```
Assets/Sabotrix/
  Scripts/Core/      Rules engine. Pure C#. Assembly Sabotrix.Core (noEngineReferences: true)
                     Match (rules + round flow), PlayerState, CardDefinition, Rules (constants), MatchEvent
  Scripts/Game/      MonoBehaviours, UI, presentation. Assembly Sabotrix.Game
                     GameController (Title -> Setup -> Handoff/Match -> Results)
                     CameraDirector (on the Bar): switches Cinemachine cameras by name
                     SeatedPlayers (on the Bar): hides your own patron, shows the opponent's card backs
                     ResolveShow (on the Bar): replays LastRoundEvents over the table from the overhead camera
                     RoomLighting (on the Bar): light switches (Room, Tv, Table) for RoomLight props
    Data/            CardData, HeroData, DeckData ScriptableObject types
    UI/              UiBuilder (uGUI helpers), MatchScreen, MenuScreen, MatchText
  Scripts/Editor/    Editor-only tools. Assembly Sabotrix.Editor
                     BarBlockoutBuilder (menu Sabotrix → Build Bar Blockout)
  Data/              ScriptableObject assets: Cards/, Heroes/, Decks/
  Prefabs/Bar.prefab The bar room. Props named for lookup (LightBox, Seat_P1, ...)
                     Cinemachine cameras: Seat_P1, Seat_P2, Overhead, Cutaway_LightBox
  Art/Blockout/      Flat-color materials for the bar blockout
  Art/Characters/    Quaternius human models (Humanoid rig). Credits in docs/credits.md
  Art/Animations/    Universal Animation Library FBX + sitting Animator Controllers
  Art/Lighting/      Post-processing Volume profile (bloom, vignette)
  Scenes/Main.unity  The only scene (camera with CinemachineBrain, Bar, Game object with GameController). All lights live in the Bar.
  Tests/EditMode/    Assembly Sabotrix.Tests.EditMode. TestContent mirrors GDD Section 10.
```

- `StarterAssetTests` check that the data assets match `TestContent` (the GDD numbers). When a card or hero number changes, update the asset, `TestContent`, and the GDD together.
- UI text uses legacy `UnityEngine.UI.Text` with the built-in font (no TextMeshPro setup yet).

## Architecture

- **Core** owns all game rules and state. It has no `UnityEngine` references, so it runs in fast EditMode tests and can later drive a bot or online play.
- **Game** reads Core state and sends player intents (select card, withdraw, end turn). Game never changes rules state directly.
- Randomness (shuffle, initiative tie, sabotage reversal) goes through an injected, seedable RNG so tests are deterministic.
- Card and hero data live in ScriptableObjects in `Data/` and are converted to Core definitions at match start.
- Prefer building gray-box UI and scenes from code or editor scripts over hand-editing scene/prefab YAML.

## Conventions

- C#: PascalCase for types, methods, properties. `_camelCase` for private fields. Serialized fields use `[SerializeField] private`.
- Use explicit types instead of `var`.
- Prefer immutable definitions (readonly fields, get-only properties).
- Every Core rule gets an EditMode test. Name tests `Method_Condition_Result`.
- Run the compile and EditMode tests before calling a task done.

## Git

- Commit every asset with its `.meta` file. Never delete or recreate a `.meta` file on its own. Unity uses it to track references.
- Files created outside the editor get their `.meta` on the next Unity import. Run the compile command before committing.
- Binary assets (images, audio, models, fonts) go through Git LFS via `.gitattributes`.
- Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`, or IDE-generated `.csproj`/`.sln`/`.slnx` files.
