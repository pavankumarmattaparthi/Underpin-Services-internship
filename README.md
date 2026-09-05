# Underpin Services Internship — Slot Game

A 3-reel slot machine game built in Unity, developed during the Underpin Services Game Development internship.

## Overview

The player bets gold on one of three fixed amounts, pulls the lever, and spins three independent reels. Matching symbols across the reels pay out a multiple of the bet.

**Symbols and payouts** (3-of-a-kind / 2-of-a-kind multipliers):

| Symbol | 3 Match | 2 Match |
| ------ | ------- | ------- |
| Seven  | 20x     | 5x      |
| Bar    | 10x     | 3x      |
| Bell   | 5x      | 2x      |
| Cherry | 3x      | 1x      |

## Project Structure

```
Assets/Slot Game/
├── Art/                 Reel symbols, background/machine art, animated GIF
├── Scenes/               SampleScene.unity — main gameplay scene
├── Scripts/
│   ├── Game/              SlotGameManager.cs — bets, gold, spin flow, prize calculation
│   ├── Reels/             SlotReel.cs — per-reel spin, symbol alignment/recycling
│   └── UI/                SlotUI.cs — bet buttons, gold/bet display, exit
├── Sounds/               Audio assets
└── UI/                   UI sprites (popup, buttons, gradients)
```

## Core Scripts

- **SlotGameManager** — singleton that tracks total gold and current bet, validates bets, triggers the lever animation and reel spins, and calculates prizes once all reels stop.
- **SlotReel** — animates a reel's symbol children scrolling downward, recycles symbols that scroll off-screen, and aligns the reel to the nearest symbol when the spin ends.
- **SlotUI** — wires up the bet buttons (10 / 50 / 100 gold) and exit button, and keeps the gold/bet text in sync with `SlotGameManager`.

## Requirements

- Unity **6000.3.11f1** (Unity 6)
- Universal Render Pipeline (URP) 17.3.0
- TextMesh Pro (included in `Assets/TextMesh Pro`)

## Getting Started

1. Open the project in Unity Hub using version `6000.3.11f1` (or later Unity 6 release).
2. Open `Assets/Slot Game/Scenes/SampleScene.unity`.
3. Press Play, choose a bet amount, and pull the lever to spin.

## Notes

- `Library/`, `Temp/`, `obj/`, `Build/`, and IDE-generated files are excluded via `.gitignore` — Unity will regenerate them on first open.
