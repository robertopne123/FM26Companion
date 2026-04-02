# Gaffer (FM26 BepInEx plugin)

Gaffer is a BepInEx IL2CPP plugin for Football Manager 2026 on macOS.

## Confirmed FM26 baseline from Thunderstore

The FM26 community pack confirms:
- Package version: `BepInEx-BepInExPack_FootballManager26-6.0.738`
- Source build: `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.738+af0cba7.zip`
- Runtime family: BepInEx 6 IL2CPP x64 for Unity games

This project pins `BepInEx.Unity.IL2CPP` to `6.0.0-be.738` to match that baseline.

## Build

```bash
# 1) Locate your Steam app bundle (default Steam path on macOS)
FM26_APP="$HOME/Library/Application Support/Steam/steamapps/common/Football Manager 2026/Football Manager 2026.app"

# If your Steam library is on another disk, discover it with:
# find "$HOME/Library/Application Support/Steam/steamapps" -name "Football Manager 2026.app" -maxdepth 6

# 2) Set GameDir to the executable folder that contains BepInEx/interop
export GameDir="$FM26_APP/Contents/MacOS"

dotnet build -c Release
```

Output DLL:

`bin/Release/net6.0/Gaffer.dll`

Copy that DLL to:

`$GameDir/BepInEx/plugins/`

## Unity version check (Steam macOS path)

If you want to verify Unity version from the installed app bundle, use:

```bash
FM26_APP="$HOME/Library/Application Support/Steam/steamapps/common/Football Manager 2026/Football Manager 2026.app"

cat "$FM26_APP/Contents/Resources/Data/boot.config" | grep unity-version
# or
strings "$FM26_APP/Contents/MacOS/Football Manager 2026" | grep -E "^[0-9]{4}\\.[0-9]+\\.[0-9]+"
```

If those paths are missing, your Steam library is likely on a non-default location; resolve the app with `find` first, then reuse `FM26_APP`.

## Current phase-1 scaffold

- `Plugin.cs`: entry point and wiring only
- `DataReader.cs`: FM object access boundary with graceful-fail stubs
- `ClaudeClient.cs`: Anthropic API calls
- `UI/GafferUI.cs`: panel rendering + test button + toggle
- `AlertEngine.cs`: proactive alert monitoring scaffold
- `Models/`: internal, serializable game-state models
