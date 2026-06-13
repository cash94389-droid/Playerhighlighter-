# PlayerHighlighter

A BONELAB MelonLoader mod that highlights other players with glowing outlines and nametags.

## Features

- **Player outlines**: Glowing outlines around other players' avatars
- **Nametags**: Floating world-space nametags above each player's head
- **Pulse effect**: Animated pulsing glow
- **Color presets**: Quick-set common colors via BoneMenu
- **LabFusion support**: Works seamlessly with multiplayer mods
- **Live controls**: Change all settings in-game via BoneMenu

## Installation

1. Install [MelonLoader](https://melonwiki.xyz/) on BONELAB
2. Install [BoneLib](https://thunderstore.io/c/bonelab/p/gnonme/BoneLib/) mod
3. Download `PlayerHighlighter.dll` and place in `BONELAB/Mods/`
4. Run the game!

## Building

### Prerequisites

- [.NET 6 SDK](https://dotnet.microsoft.com/download) or later
- [MelonLoader](https://melonwiki.xyz/) installed on BONELAB
- [BoneLib](https://thunderstore.io/c/bonelab/p/gnonme/BoneLib/) in `BONELAB/Mods/`

### Build Steps

#### Windows (PowerShell)

```powershell
.\Build.ps1
```

Or with auto-deploy:

```powershell
.\Build.ps1 -BonelabPath "C:\Program Files (x86)\Steam\steamapps\common\BONELAB" -AutoDeploy
```

#### Linux / macOS

```bash
dotnet build PlayerHighlighter.csproj -c Release /p:BONELAB_PATH="/path/to/BONELAB"
```

The compiled DLL will be in `./Mods/PlayerHighlighter.dll`.

## Usage

- Open **BoneMenu** in-game
- Navigate to **Player Highlighter**
- Toggle highlighting on/off, adjust colors, outline width, and effects
- Use **Presets** for quick color changes

## BoneMenu Controls

- **Enabled**: Toggle highlighting
- **Outline Width**: Thicker/thinner outlines
- **Show Nametags**: Toggle nametag visibility
- **Nametag Scale**: Size of floating names
- **Pulse Effect**: Enable/disable pulsing glow
- **Pulse Speed**: Speed of the pulse animation
- **Highlight Color**: Fine-tune R, G, B, A values
- **Presets**: Quick color presets (Cyan, Red, Gold, Green, Purple, White)

## Configuration

Settings are saved to `UserData/MelonPreferences.cfg` and persist between sessions.

## Compatibility

- **BONELAB** (Steam / VR)
- **MelonLoader** 0.6+
- **BoneLib** (required)
- **LabFusion** (optional; soft dependency via reflection)

## License

MIT
