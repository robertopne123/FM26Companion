# Gaffer — FM26 AI Companion Mod

A BepInEx IL2CPP plugin for Football Manager 2026 (Mac) that injects a real-time AI coaching panel powered by Claude.

---

## Prerequisites

| Tool | Where to get it |
|------|----------------|
| .NET 6 SDK | https://dotnet.microsoft.com/download/dotnet/6.0 |
| BepInEx 6 (IL2CPP, Mac) | https://github.com/BepInEx/BepInEx/releases — grab the `macos-x64` IL2CPP build |
| Il2CppDumper | https://github.com/Perfare/Il2CppDumper/releases |
| Anthropic API key | https://console.anthropic.com/ |

---

## Step 0 — Determine FM26's Unity version

The Unity version determines which BepInEx build to use and affects interop DLL generation.

```bash
# Read the Unity version from the app bundle
cat "/Applications/Football Manager 2026.app/Contents/Resources/Data/boot.config" \
  | grep unity-version
# or
strings "/Applications/Football Manager 2026.app/Contents/MacOS/Football Manager 2026" \
  | grep -E "^[0-9]{4}\.[0-9]+\.[0-9]+"
```

Note the version (e.g. `2022.3.x`) and confirm the BepInEx release targets the same Unity LTS series.

---

## Step 1 — Install BepInEx into FM26

1. Download the **BepInEx 6 IL2CPP macOS** release (`.zip`).
2. Extract it. You will get a folder structure like:
   ```
   BepInEx/
   doorstop_config.ini
   libdoorstop.dylib
   ```
3. Copy these into the FM26 app contents:
   ```bash
   FM26_APP="/Applications/Football Manager 2026.app/Contents/MacOS"
   cp -r BepInEx/          "$FM26_APP/BepInEx"
   cp doorstop_config.ini  "$FM26_APP/"
   cp libdoorstop.dylib    "$FM26_APP/"
   ```
4. Launch FM26 once. BepInEx will:
   - Generate `BepInEx/interop/` — the IL2CPP proxy DLLs your plugin references.
   - Generate `BepInEx/config/` — where `FM26Companion.cfg` will live.
   - Produce logs at `BepInEx/LogOutput.log`.

> **macOS Gatekeeper note:** You may need to `xattr -cr` the app or approve it in
> System Settings → Privacy & Security after adding BepInEx files.

---

## Step 2 — Run Il2CppDumper to identify FM26 classes

FM26 compiles its C# to native machine code via IL2CPP. The class names, field names,
and method signatures are stripped from the binary but recorded in metadata. Il2CppDumper
reads both files and produces a stub DLL you can reference from your plugin.

### 2a — Locate the files

```bash
FM26_APP="/Applications/Football Manager 2026.app/Contents/MacOS"

# The compiled native library (all FM C# code, compiled to ARM64/x86-64 machine code)
ls "$FM26_APP/GameAssembly.dylib"

# The IL2CPP metadata — contains all type/method/field names
ls "$FM26_APP/Football Manager 2026_Data/il2cpp_data/Metadata/global-metadata.dat"
```

### 2b — Run Il2CppDumper

```bash
# On Mac, run the .NET version of Il2CppDumper:
dotnet Il2CppDumper.dll \
  "$FM26_APP/GameAssembly.dylib" \
  "$FM26_APP/Football Manager 2026_Data/il2cpp_data/Metadata/global-metadata.dat" \
  ./dump-output/

# Outputs in ./dump-output/:
#   dump.cs          — all class/field/method stubs with offsets
#   il2cpp.h         — C header with struct layouts
#   script.json      — for use with IDA/Ghidra
#   DummyDll/        — compilable stub DLLs
```

### 2c — Find FM class names

```bash
# Search dump.cs for match/player/manager related classes:
grep -n "class.*Match"   dump-output/dump.cs | head -40
grep -n "class.*Player"  dump-output/dump.cs | head -40
grep -n "class.*Manager" dump-output/dump.cs | head -40

# Once you find a promising class, inspect its fields:
grep -A 30 "class FMMatch" dump-output/dump.cs
```

### 2d — Add the DummyDll to your project

Copy `dump-output/DummyDll/Assembly-CSharp.dll` into a local `lib/` folder, then
uncomment the `Assembly-CSharp` reference in `FM26Companion.csproj`.

Now populate `DataReader.cs` with real field reads.

---

## Step 3 — Build the plugin

```bash
# Set GameDir to the FM26 binary folder (where BepInEx/interop/ was generated)
export GameDir="/Applications/Football Manager 2026.app/Contents/MacOS"

cd /path/to/FM26Companion
dotnet build -c Release
```

The output DLL will be at `bin/Release/net6.0/FM26Companion.dll`.

---

## Step 4 — Deploy

```bash
FM26_APP="/Applications/Football Manager 2026.app/Contents/MacOS"
PLUGINS="$FM26_APP/BepInEx/plugins/FM26Companion"

mkdir -p "$PLUGINS"
cp bin/Release/net6.0/FM26Companion.dll "$PLUGINS/"
```

> Only the `FM26Companion.dll` needs to be deployed. Dependencies (BepInEx core,
> Il2CppInterop) are already part of the BepInEx installation.

---

## Step 5 — Set your Claude API key

BepInEx generates the config file the first time the plugin loads.

Open `BepInEx/config/FM26Companion.cfg` and add your key:

```ini
[Claude]

## Your Anthropic API key (sk-ant-...). Get one at https://console.anthropic.com/
# Setting type: String
# Default value:
ApiKey = sk-ant-YOUR-KEY-HERE
```

---

## Step 6 — Launch FM26 and verify

1. Start Football Manager 2026.
2. Check `BepInEx/LogOutput.log` for:
   ```
   [Info  :FM26Companion] Gaffer: plugin loaded — FM26 IL2CPP injection confirmed.
   [Info  :FM26Companion] Gaffer: UI host initialised.
   ```
3. You should see a **Gaffer** panel in the top-right corner of the FM26 window.
4. Press **G** to send a test message to Claude. The panel will display the response.
5. Press **Shift+G** to toggle the panel on/off.

---

## Project structure

```
FM26Companion/
├── FM26Companion.csproj     # Project file — references BepInEx NuGet + interop DLLs
├── NuGet.Config             # Points to BepInEx's NuGet feed
├── src/
│   ├── Plugin.cs            # BepInEx entry point — registers IL2CPP types, creates UI host
│   ├── DataReader.cs        # All FM object reads — populate after Il2CppDumper
│   ├── ClaudeClient.cs      # All Claude API calls — async HTTP, BepInEx config key
│   └── UI/
│       ├── GafferPanel.cs   # Main overlay window (uGUI Canvas, draggable, close button)
│       └── DragHandler.cs   # Drag-to-reposition behaviour
└── README.md
```

### Architecture rules

- **DataReader.cs** is the only place that touches FM's IL2CPP objects.
- **ClaudeClient.cs** is the only place that makes HTTP calls.
- **GafferPanel.cs** calls both and renders results — no business logic.

---

## Troubleshooting

| Symptom | Likely cause |
|---------|-------------|
| `interop/` folder is empty | FM26 didn't finish loading with BepInEx — check `LogOutput.log` for errors |
| `MissingMethodException` on component add | Forgot `ClassInjector.RegisterTypeInIl2Cpp<T>()` for a new MonoBehaviour |
| Panel doesn't appear | Check log for `GafferPanel.Awake()` — Canvas sort order may be overridden by FM |
| `401 Unauthorized` from Claude | API key missing or wrong in `FM26Companion.cfg` |
| `CS0246` build errors | Interop DLLs not yet generated — run FM26 with BepInEx first |
| macOS quarantine crash | Run `xattr -cr "/Applications/Football Manager 2026.app"` |

---

## Phase 2 (planned)

Push live game state to a cloud backend so a mobile companion app can poll it.
`DataReader.cs` already exposes the data models (`MatchData`, `PlayerData`) that
will be serialised and sent to the backend endpoint.
