# Character Animation Setup Guide

This guide walks you through setting up your Mixamo model with 3 animations (Idle, Run, Carry Item) in Unity.

---

## **STEP 1: Import Your Model and Animations**

### 1.1 Import Model File
- In Unity, drag your **model file** (FBX or prefab from Mixamo) into `Assets/` folder
- Select the imported model in Project folder
- In Inspector, go to **Model** tab:
  - Uncheck **"Meshes"** (if you want animations only, or keep checked)
  - Keep **"Animations"** checked
  - Click **Apply**

### 1.2 Split Animations (If Needed)
If all animations came as one file, you'll need to split them:

1. Select the model file
2. Go to **Animations** tab in Inspector
3. You should see animation clips:
   - **Idle** (frames 0-120, for example)
   - **Run** (frames 120-240)
   - **CarryItem** (frames 240-360)
4. If they're not auto-detected, manually create them:
   - Click **+** to create clip
   - Name it "Idle"
   - Set start/end frame numbers
   - Repeat for "Run" and "CarryItem"

---

## **STEP 2: Create the Animator Controller**

### 2.1 Create Animator Controller
1. Right-click in Project folder → Create → **Animator Controller**
2. Name it: `PlayerAnimator` (or similar)
3. Save it to `Assets/` or `Assets/Animations/` folder

### 2.2 Open Animator Window
1. Double-click the `PlayerAnimator` to open it
2. You should see an empty graph with **Entry** and **Any State** nodes

---

## **STEP 3: Create Animation States**

In the Animator window, you'll create 3 states:

### 3.1 Create Idle State
1. Right-click in graph → **Create State** → **Empty**
2. Name it: `Idle`
3. Drag your **Idle animation clip** onto this state (in Inspector)
4. Set **Loop Time**: checked (for idle looping)
5. In the bottom-left, you'll see a **gear icon** - click it and set the **Speed** to a reasonable value (e.g., 1.0)

### 3.2 Create Run State
1. Right-click → **Create State** → **Empty**
2. Name it: `Run`
3. Assign your **Run animation clip**
4. **Loop Time**: checked
5. Adjust speed if needed

### 3.3 Create CarryItem State
1. Right-click → **Create State** → **Empty**
2. Name it: `CarryItem`
3. Assign your **CarryItem animation clip**
4. **Loop Time**: checked

### 3.4 Set Default State
- Right-click the **Idle** state
- Select **Set as Layer Default State**
- (This makes Idle the starting animation)

---

## **STEP 4: Create Animation Parameters**

These are conditions that control transitions:

### 4.1 Add Parameters
1. In Animator window, left panel shows **Parameters** section
2. Click **+** button
3. Add **Bool** parameter: `IsRunning`
4. Add **Bool** parameter: `IsCarrying`

Your parameters are now:
- `IsRunning` (bool) - true when player is running
- `IsCarrying` (bool) - true when holding an item

---

## **STEP 5: Create Transitions**

Transitions are arrows connecting states based on parameters.

### 5.1 Idle → Run
1. Right-click **Idle** state → **Make Transition**
2. Click on **Run** state to connect them
3. Click the **arrow** to select it
4. In Inspector, set **Condition**: `IsRunning` equals `true`
5. Set **Exit Time**: 0 (immediate transition)
6. **Transition Duration**: 0.1 (smooth blend)

### 5.2 Run → Idle
1. Right-click **Run** → **Make Transition** → **Idle**
2. Click arrow, set **Condition**: `IsRunning` equals `false`
3. Set **Exit Time**: 0
4. **Transition Duration**: 0.1

### 5.3 Any State → CarryItem
1. Right-click **Any State** → **Make Transition** → **CarryItem**
2. Click arrow, set **Condition**: `IsCarrying` equals `true`
3. Set **Exit Time**: 0
4. **Transition Duration**: 0.15

### 5.4 CarryItem → Run
1. Right-click **CarryItem** → **Make Transition** → **Run**
2. Condition: `IsCarrying` equals `false` AND `IsRunning` equals `true`
3. To add multiple conditions, click **+** in the arrow's Conditions section
4. Set **Exit Time**: 0

### 5.5 CarryItem → Idle
1. Right-click **CarryItem** → **Make Transition** → **Idle**
2. Condition: `IsCarrying` equals `false` AND `IsRunning` equals `false`
3. Add second condition with **+**
4. Set **Exit Time**: 0

**Final Transition Table:**
```
Idle          → Run         (IsRunning = true)
Run           → Idle        (IsRunning = false)
Any State     → CarryItem   (IsCarrying = true)
CarryItem     → Run         (IsCarrying = false, IsRunning = true)
CarryItem     → Idle        (IsCarrying = false, IsRunning = false)
```

---

## **STEP 6: Setup Player GameObject**

### 6.1 Add Animator Component
1. Select your **player character** GameObject in scene
2. In Inspector, add **Component** → **Animator**
3. Assign your `PlayerAnimator` controller to the **Controller** field

### 6.2 Add AnimationController Script
1. Make sure `PlayerControl.cs` is already on the player
2. Add the `AnimationController.cs` script to the **same GameObject**

### 6.3 Verify Setup
1. Your player GameObject should now have:
   - ✅ **Rigidbody**
   - ✅ **PlayerControl** (script)
   - ✅ **Animator** (with PlayerAnimator controller assigned)
   - ✅ **AnimationController** (script)
   - ✅ **Skeletal Mesh** (your 3D model)

---

## **STEP 7: Test and Debug**

### 7.1 Play the Game
1. Click **Play** in Unity
2. Press movement keys (WASD for Player 1, Arrows for Player 2)
3. Watch for animations:
   - **Idle** → starts at rest
   - **Run** → press movement + run key
   - **CarryItem** → pick up an item (F for Player 1, Enter for Player 2)

### 7.2 Debug Animation State
- Press **L** in-game to log current animation state
- Check Console for output like: `Animation State - IsRunning: true, IsCarrying: false`

### 7.3 Troubleshooting
| Problem | Solution |
|---------|----------|
| Animation doesn't play | Check Animator controller is assigned |
| Wrong animation plays | Verify transition conditions in Animator |
| Animation cuts off | Increase "Transition Duration" or check "Exit Time" |
| Jittery animations | Check animation loop settings, adjust speed |
| Always in idle | Verify `PlayerControl` is detecting input correctly |

---

## **STEP 8: Fine-Tuning (Optional)**

### 8.1 Animation Speed
- Select state in Animator
- In Inspector, find **Speed** field
- Adjust if animations feel too fast/slow (1.0 = normal)

### 8.2 Transition Blending
- Select transition arrow
- Adjust **Transition Duration** (0.1-0.2 for smooth blending)
- Adjust **Exit Time** if you want animation to finish before transitioning

### 8.3 Avatar Setup (If model doesn't animate)
- Select your model file (not instance)
- Go to **Rig** tab
- Set **Avatar Definition**: Humanoid
- Configure bones if needed
- Click **Apply**

---

## **Summary**

✅ Import model + animations from Mixamo  
✅ Create Animator Controller with 3 states  
✅ Add parameters: `IsRunning`, `IsCarrying`  
✅ Create transitions between states  
✅ Add Animator to player GameObject  
✅ Add AnimationController script  
✅ Test and adjust animation speeds  

Your player should now smoothly transition between animations based on movement and item state!

---

## **Common Issues**

**Q: My model doesn't animate even though the Animator plays animations in preview**
- A: The model likely needs a Humanoid avatar. Check Step 8.3

**Q: Animation loops incorrectly**
- A: Select the animation clip, go to Inspector → Animations tab → uncheck "Loop Time" if it should only play once

**Q: Player doesn't show running animation when holding Shift**
- A: Verify PlayerControl.Run is set to correct key. Press L to debug.

**Q: CarryItem animation never plays**
- A: Check that transition from Any State has correct condition. Verify PlayerControl detects items correctly.
