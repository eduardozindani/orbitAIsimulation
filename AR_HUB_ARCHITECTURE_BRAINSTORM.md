# AR Hub Architecture Brainstorm

**Date:** 2025-01-28  
**Chef's Vision:** ARHub with Earth anchored to physical 30cm globe on table  
**Status:** 🧠 Brainstorming & Architecture Decision

---

## Your Vision (Understanding)

**What you want:**

- ARHub scene = Hub in pass-through mode
- Virtual Earth perfectly aligned with your 30cm physical globe
- No stars/space background - just Earth floating in real room
- All Hub functionality works (conversations, tools, etc.)
- Anchor persists - Earth stays aligned every time you enter ARHub

**The Challenge:**

> "Once we fix here and it's there, then it works perfectly"

This is **spatial anchoring + calibration** - aligning virtual world to physical world.

---

## Architecture Options

### Option A: Separate ARHub Scene ⭐ (Recommended)

**Structure:**

```
Hub.unity (VR mode)
  ├── Stars/Space background
  ├── Earth (centered in space)
  ├── ExperienceManager
  └── All Hub functionality

ARHub.unity (Pass-through mode)
  ├── NO stars/space background
  ├── Earth (anchored to physical globe)
  ├── ExperienceManager (same, but AR mode)
  ├── ARAnchoringSystem (new component)
  └── All Hub functionality (shared via prefabs)
```

**Pros:**

- ✅ **Clear separation** - VR vs AR are distinct experiences
- ✅ **Easy to configure** - Each scene has its own setup
- ✅ **No mode switching complexity** - Scene transition handles it
- ✅ **Independent testing** - Can test AR without affecting VR Hub
- ✅ **Cleaner code** - No conditional "if AR mode" everywhere

**Cons:**

- ⚠️ **Scene duplication** - Need to keep two scenes in sync
- ⚠️ **Maintenance** - Feature changes need to be applied twice (unless using prefabs)

**Mitigation for Cons:**

- Use **prefabs** for shared components (Earth, UI, etc.)
- Use **shared scripts** for logic (ExperienceManager can work in both)
- Only scene-specific differences: background + anchoring

---

### Option B: Single Hub Scene with Mode Toggle

**Structure:**

```
Hub.unity (VR + AR modes)
  ├── Mode: VR | AR (runtime toggle)
  ├── Stars (shown only in VR mode)
  ├── Earth (centered in VR, anchored in AR)
  ├── ExperienceManager (mode-aware)
  └── All Hub functionality
```

**Pros:**

- ✅ **Single scene** - No duplication
- ✅ **Easy switching** - Toggle at runtime
- ✅ **Unified codebase** - One place for logic

**Cons:**

- ⚠️ **Complex conditional logic** - "if AR mode, do X, else Y" everywhere
- ⚠️ **Scene complexity** - One scene doing two different things
- ⚠️ **Harder to debug** - Mode-specific bugs are entangled
- ⚠️ **Pass-through initialization** - Needs careful timing
- ⚠️ **Anchoring logic** - Mixing VR camera positioning with AR anchoring

**Verdict:** ❌ **Not recommended** - More complexity than benefit

---

## Technical Implementation Breakdown

### 1. Pass-through Setup (Already Done ✅)

- ✅ `insightPassthroughEnabled: 1` in OculusProjectConfig
- ✅ Need `PassthroughController` component to enable/disable per scene

### 2. Spatial Anchoring System (The Hard Part) ⚠️

**Challenge:** Aligning virtual Earth to your 30cm physical globe

**Approach: Manual Calibration (Recommended for V1)**

```
Step 1: First-time setup
  - User enters ARHub
  - System shows calibration UI: "Point controller at center of your physical globe"
  - User points and presses button
  - System records position/orientation
  - Virtual Earth spawns at that position
  - Scale Earth to match 30cm diameter

Step 2: Save anchor
  - Create OVRSpatialAnchor at Earth position
  - Save anchor UUID to PlayerPrefs or file

Step 3: Future visits
  - Load saved anchor UUID
  - Restore anchor
  - Place Earth at anchor position
```

**Components Needed:**

1. **`AREarthAnchoring.cs`** (New component)

   - Handles calibration flow
   - Creates/loads spatial anchors
   - Positions Earth GameObject
   - Scales Earth to match physical globe (30cm)

2. **`CalibrationUI.cs`** (New component)

   - Shows calibration instructions
   - Handles user input (point + button press)
   - Visual feedback during calibration

3. **`PassthroughController.cs`** (New component)
   - Enables pass-through on scene load
   - Disables stars/space background

---

## Shared Components Strategy

### Option: Prefab-Based Approach (Recommended)

**Structure:**

```
Assets/Prefabs/
  └── Hub/
      ├── Earth.prefab (shared - same Earth in both scenes)
      ├── UI.prefab (shared - same UI in both scenes)
      ├── ExperienceManager.prefab (shared - same logic)
      └── PromptConsole.prefab (shared - same AI functionality)

Hub.unity:
  ├── Stars/Background
  └── Instances of shared prefabs

ARHub.unity:
  ├── NO Stars/Background
  ├── Instances of shared prefabs
  └── AR-specific: AREarthAnchoring, PassthroughController
```

**Benefits:**

- ✅ Single source of truth for Earth, UI, logic
- ✅ Changes to shared prefabs update both scenes automatically
- ✅ Only scene-specific differences: background + anchoring

---

## The Calibration Flow (Detailed)

### Phase 1: First-Time Calibration

```
User enters ARHub
  ↓
PassthroughController enables pass-through
  ↓
AREarthAnchoring checks: "Have we calibrated before?"
  ↓
NO → Show CalibrationUI
  ↓
User points controller at center of physical globe
  ↓
User presses trigger button
  ↓
System records:
  - Position (controller position in world space)
  - Rotation (controller forward direction)
  - Scale (calculate Earth scale to match 30cm diameter)
  ↓
Create OVRSpatialAnchor at that position
  ↓
Save anchor UUID + Earth transform data
  ↓
Spawn Earth at anchor position
  ↓
Hide CalibrationUI
```

### Phase 2: Subsequent Visits

```
User enters ARHub
  ↓
PassthroughController enables pass-through
  ↓
AREarthAnchoring checks: "Have we calibrated before?"
  ↓
YES → Load saved anchor UUID
  ↓
Query OVRSpatialAnchor system for anchor
  ↓
If anchor exists (same physical location):
  - Place Earth at anchor position ✅
  - Use saved scale/orientation

If anchor not found (different location):
  - Show CalibrationUI again
  - User recalibrates
```

### Phase 3: Re-calibration (User Request)

```
User presses "Recalibrate" button in ARHub
  ↓
Show CalibrationUI
  ↓
User recalibrates (same as Phase 1)
  ↓
Update saved anchor
  ↓
Move Earth to new position
```

---

## Technical Components Needed

### 1. `PassthroughController.cs`

**Location:** `Assets/Scripts/VR/PassthroughController.cs`

**Responsibilities:**

- Enable/disable `OVRPassthroughLayer` on scene load
- Hide VR-specific elements (stars, space background)
- Check device compatibility

**Inspector Settings:**

- `bool enablePassthrough` - Set per scene

---

### 2. `AREarthAnchoring.cs` (NEW)

**Location:** `Assets/Scripts/VR/AREarthAnchoring.cs`

**Responsibilities:**

- Manage spatial anchor lifecycle
- Handle calibration flow
- Position/scale Earth to match physical globe
- Save/load anchor data

**Key Methods:**

```csharp
public void StartCalibration()
public void OnCalibrationPoint(Vector3 position, Quaternion rotation)
public void SaveAnchor(Guid anchorUUID, TransformData earthData)
public bool LoadAnchor(out TransformData earthData)
public void PlaceEarthAtAnchor(TransformData data)
```

**Inspector Settings:**

- `GameObject earthPrefab` - Reference to Earth prefab
- `float physicalGlobeDiameterCm` - 30cm
- `string anchorSaveKey` - PlayerPrefs key for anchor UUID

---

### 3. `CalibrationUI.cs` (NEW)

**Location:** `Assets/Scripts/VR/CalibrationUI.cs`

**Responsibilities:**

- Show calibration instructions
- Handle controller pointing/button input
- Visual feedback (ray, cursor, etc.)
- Callback to AREarthAnchoring when user confirms

**UI Elements:**

- Instruction text: "Point at center of your physical globe"
- Visual ray from controller
- Button prompt: "Press trigger to calibrate"

---

### 4. Shared Script Updates

**`ExperienceManager.cs`:**

- Minor: Detect if in AR mode, skip intro cutscene (no need for deep space zoom)
- Everything else works the same

**No changes needed to:**

- `PromptConsole.cs` - Works identically
- `MissionSpaceController.cs` - VR-only scenes
- `SceneTransitionManager.cs` - Already handles scene transitions

---

## Scene Configuration

### ARHub.unity Setup:

```
Scene Hierarchy:
├── AREarthAnchoring (GameObject with AREarthAnchoring.cs)
├── PassthroughController (GameObject with PassthroughController.cs)
├── CalibrationUI (Canvas with CalibrationUI.cs)
├── Earth (Instance of Prefabs/Hub/Earth.prefab)
├── ExperienceManager (Instance of Prefabs/Hub/ExperienceManager.prefab)
├── PromptConsole (Instance of Prefabs/Core/PromptConsole.prefab)
├── UI Canvas (Instance of Prefabs/UI/Canvas.prefab)
└── CameraRig (VR cameras)

NOT included:
❌ Stars/Starfield
❌ Space background
❌ Skybox
```

### Differences from Hub.unity:

| Element        | Hub.unity             | ARHub.unity                |
| -------------- | --------------------- | -------------------------- |
| Background     | Stars/Space           | Pass-through (real room)   |
| Earth Position | Centered in space     | Anchored to physical globe |
| Earth Scale    | Large (visual)        | Scaled to 30cm diameter    |
| Intro Cutscene | Yes (deep space zoom) | No (or modified)           |
| Calibration    | No                    | Yes (first time only)      |

---

## Implementation Phases

### Phase 1: Basic Pass-through (1-2 hours)

- ✅ Enable pass-through (DONE)
- Create `PassthroughController`
- Test pass-through in ARHub scene

### Phase 2: Earth Positioning (2-3 hours)

- Create `AREarthAnchoring` component
- Manual positioning (Inspector values, no calibration yet)
- Test Earth appears in correct position

### Phase 3: Calibration System (3-4 hours)

- Create `CalibrationUI`
- Implement controller pointing
- Save/load anchor data
- Test calibration flow

### Phase 4: Scene Setup (1-2 hours)

- Configure ARHub.unity
- Remove stars/background
- Add AR-specific components
- Test scene transitions

### Phase 5: Polish & Testing (2-3 hours)

- Test on Quest hardware
- Refine calibration UX
- Handle edge cases (anchor lost, recalibration)
- Performance optimization

**Total Estimate:** 9-14 hours

---

## Challenges & Solutions

### Challenge 1: Anchor Persistence Across Sessions

**Problem:** OVRSpatialAnchor might not persist if user moves physical globe  
**Solution:**

- Check if anchor exists on load
- If not found, trigger recalibration
- User can manually recalibrate anytime

### Challenge 2: Scale Matching (30cm Physical → Virtual)

**Problem:** Need to scale Earth to match physical globe exactly  
**Solution:**

- Calculate Unity scale from 30cm physical diameter
- Earth prefab diameter in Unity units → calculate scale factor
- Apply scale during calibration

### Challenge 3: Intro Cutscene in AR

**Problem:** Deep space zoom doesn't make sense in AR (no space)  
**Solution:**

- Skip intro in ARHub scene
- Or: Modified intro (Earth fades in instead of zoom)

### Challenge 4: Shared Code Maintenance

**Problem:** Need to keep Hub and ARHub in sync  
**Solution:**

- Prefab-based approach (single source of truth)
- Shared scripts (ExperienceManager, PromptConsole, etc.)
- Only scene-specific: background + anchoring

---

## Questions for You, Chef

1. **Calibration Method:**

   - Option A: Point controller at globe center (simpler)
   - Option B: Look at globe and press button (more natural)
   - Option C: Automatic detection (harder, might be unreliable)
     **Recommendation:** Option A (controller pointing) - most reliable

2. **Anchor Persistence:**

   - Should anchor persist across app restarts? (YES, recommended)
   - Should user be able to recalibrate anytime? (YES, recommended)
   - What if user moves physical globe? (Recalibrate)

3. **Intro Cutscene:**

   - Skip intro entirely in ARHub? (Recommended - no deep space)
   - Or modified intro (Earth fades in)?

4. **Scene Transition:**

   - Tool call in Hub → Transition to ARHub? (Makes sense)
   - Or button/menu in Hub? (Also makes sense)
   - Both options? (Flexibility)

5. **Earth Scale:**
   - Fixed 30cm diameter? (Matches your physical globe)
   - Or adjustable? (For different sized globes)

---

## Recommendation

**Chef, I recommend Option A (Separate ARHub Scene) with prefab-based shared components.**

**Why:**

- Clean separation of concerns
- Easier to maintain
- Less conditional complexity
- Natural fit with your existing scene transition system

**Implementation Order:**

1. Create `PassthroughController` (enable pass-through)
2. Setup ARHub scene (remove stars, add pass-through)
3. Create `AREarthAnchoring` with manual positioning first
4. Add calibration system
5. Test on Quest hardware

**The hard part is spatial anchoring, but it's very doable with Meta's SDK. The calibration flow is straightforward - point, press, save, done.**

---

## Next Steps

**If you approve this approach, I can:**

1. ✅ Create `PassthroughController.cs`
2. ✅ Create `AREarthAnchoring.cs` (basic version with manual positioning)
3. ✅ Configure ARHub scene (remove stars, setup pass-through)
4. ✅ Add calibration system when you're ready

**Or, if you want to discuss more:**

- Refine the calibration UX
- Consider alternative approaches
- Answer the questions above

**Your call, Chef.** This is absolutely doable - the spatial anchoring is the main technical challenge, but Meta's SDK makes it straightforward.
