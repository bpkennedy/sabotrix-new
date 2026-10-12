# Sabotrix

Turn-based 1v1 card battler set in a nerd bar, where Sabotage cards mess with the room itself. Built in Unity as a father/son learning project.

- **Malcolm (12)** is Game Director. He owns rules, cards, heroes, and art direction.
- **Brian** is Tech Lead. He is a senior web developer and **new to Unity**.

## Source of Truth

- `docs/game-design-document.md` (GDD) defines the game. Do not invent rules, cards, or numbers. If the GDD is silent or ambiguous, ask instead of guessing.
- When a rule changes, update the GDD in the same change.
- `docs/m1-plan.md` is the current milestone plan.

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
  Scripts/Game/      MonoBehaviours, UI, presentation. Assembly Sabotrix.Game
  Scripts/Editor/    Editor-only tools. Assembly Sabotrix.Editor
  Data/              ScriptableObject assets (heroes, cards, decks)
  Scenes/
  Tests/EditMode/    Rules engine tests. Assembly Sabotrix.Tests.EditMode
```

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
