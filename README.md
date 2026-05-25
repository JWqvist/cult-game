# Cult Rising

A top-down GTA1/2 style cult management game built with **Godot 4** and **C#**.

You play as a charismatic cult leader — recruit followers, accumulate money, and evade the authorities.

---

## Project Structure

```
cult-game/
├── project.godot          # Godot 4 project config (C# / .NET)
├── CultRising.csproj      # .NET project file
├── scenes/
│   ├── World.tscn         # Main scene: TileMap + Player spawn
│   └── Player.tscn        # CharacterBody2D with collision, sprite, animation
└── scripts/
    ├── Player.cs          # Top-down movement + animation state machine
    └── GameManager.cs     # Singleton: money, cult_size, heat_level
```

---

## Controls

| Key | Action |
|-----|--------|
| WASD / Arrow Keys | Move |
| Left Shift | Run |

---

## Sprint Plan

### Sprint 0 — Project Setup (Done)
- [x] Godot 4 project scaffold
- [x] C# scripting enabled
- [x] GameManager singleton autoload
- [x] Player scene with CharacterBody2D, placeholder sprite

### Sprint 1 — Player Movement (Done)
- [x] WASD + arrow key movement
- [x] Walk (80 px/s) and run (160 px/s)
- [x] 4-directional animation states (idle/walk/run per direction)
- [x] Smooth Camera2D follow via position smoothing

### Sprint 2 — World & Tilemap (Planned)
- [ ] Grid-based tile world with roads, buildings, open areas
- [ ] Collision layers for walls/obstacles
- [ ] Minimap HUD

### Sprint 3 — NPC & Recruitment (Planned)
- [ ] NPC wandering AI (simple state machine)
- [ ] Interaction / recruitment mechanic
- [ ] CultSize stat integration

### Sprint 4 — Economy & Heat (Planned)
- [ ] Money generation actions (donations, schemes)
- [ ] Heat system: police attention rises with illegal activity
- [ ] Wanted level UI overlay

### Sprint 5 — Audio & Polish (Planned)
- [ ] Ambient sound, footstep SFX
- [ ] Tile-based world art pass
- [ ] Basic menu / pause screen

---

## Building

Requires Godot 4.2+ with .NET (mono) support and .NET 6+ SDK.

```bash
dotnet build
```

Or open `project.godot` in the Godot editor.
