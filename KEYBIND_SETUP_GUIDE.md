# Keybind Configuration Setup Guide

This guide explains how to add keybind configuration directly to your existing How to Play panel.

## Components Created

1. **KeybindManager.cs** - Manages all keybind rebinding logic
2. **Updated PauseManager.cs** - No changes needed (simplified integration)

## UI Setup Instructions

### Step 1: Update How to Play Panel Structure

Inside your `HowToPlayPanel`, add two new containers for the keybind buttons:

```
HowToPlayPanel (Panel)
├── Title (Text) - "How to Play"
├── [Your existing content]
├── ScrollView (ScrollView)
│   └── Content (VerticalLayoutGroup)
│       ├── [Existing gameplay instructions]
│       ├── Player1KeybindsHeader (Text) - "--- Player 1 Controls ---"
│       ├── Player1Container (Panel)
│       │   └── VerticalLayoutGroup
│       │
│       ├── Player2KeybindsHeader (Text) - "--- Player 2 Controls ---"
│       └── Player2Container (Panel)
│           └── VerticalLayoutGroup
└── CloseButton (Button)
```

### Step 2: Configure Player Containers

For **Player1Container** and **Player2Container**:
- Add a `VerticalLayoutGroup` component
- Add a `Layout Element` component with:
  - Preferred Width: 300
  - Preferred Height: auto
- Add `Content Size Fitter` with `Fit X: Preferred Size`, `Fit Y: Preferred Size`

### Step 3: Add KeybindManager to Scene

1. Create a new empty GameObject called `KeybindManager` in your scene (not on canvas)
2. Add the `KeybindManager.cs` script to it
3. In the Inspector, set:
   - **How To Play Panel**: Drag your `HowToPlayPanel` here
   - **Player1KeybindsContainer**: Leave empty OR drag `Player1Container` for auto-detection
   - **Player2KeybindsContainer**: Leave empty OR drag `Player2Container` for auto-detection

**Note**: If left empty, KeybindManager will automatically search for `Player1Container` and `Player2Container` as children of the HowToPlayPanel.

### Step 4: That's It!

The keybind buttons will be automatically created and populated when the scene loads.

## How It Works

1. When How to Play panel opens, KeybindManager creates keybind buttons for each control
2. Player clicks a button (e.g., "Move Up: W")
3. Button turns blue and shows "Press any key..."
4. Player presses a key and the keybind updates instantly
5. Press ESC to cancel rebinding

## Configurable Keybinds

**Player 1** (Default):
- Move Up: W
- Move Left: A
- Move Down: S
- Move Right: D
- Interact: F
- Run: Left Shift
- Throw: Q
- Emote: Z

**Player 2** (Default):
- Move Up: Up Arrow
- Move Left: Left Arrow
- Move Down: Down Arrow
- Move Right: Right Arrow
- Interact: Return (Enter)
- Run: Right Shift
- Throw: Right Ctrl
- Emote: Backspace

## Troubleshooting

- **Buttons not appearing**: Make sure containers have `VerticalLayoutGroup` and named correctly
- **Containers not found**: Check that they're named exactly `Player1Container` and `Player2Container`
- **Keybinds not changing**: Verify KeybindManager found both PlayerControl instances
- **Visual layout issues**: Use a ScrollView with Content Size Fitter on the content area

## Notes

- Keybinds are **not** saved between sessions (they reset on game restart)
- Press ESC while rebinding to cancel (ESC cannot be bound to any action)
- To add or remove keybinds, edit the `keybindConfigs` list in KeybindManager

