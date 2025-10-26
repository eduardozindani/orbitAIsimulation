# Unity Prefabs Setup - Checklist

## Phase 1: Create Prefabs from Hub.unity ✓

- [ ] Open Hub.unity scene
- [ ] Create folder structure in Assets/Prefabs/
  - [ ] Environment/
  - [ ] Camera/
  - [ ] UI/
  - [ ] Core/

### Convert to Prefabs:
- [ ] Drag **PlanetRoot** → `Prefabs/Environment/` → Original Prefab
- [ ] Drag **CameraRig** → `Prefabs/Camera/` → Original Prefab
- [ ] Drag **Canvas** (with InputText/OutputText) → `Prefabs/UI/` → Original Prefab
- [ ] Drag **TransitionCanvas** → `Prefabs/UI/` → Original Prefab
- [ ] Drag **MissionContext** → `Prefabs/Core/` → Original Prefab
- [ ] Drag **SceneTransitionManager** → `Prefabs/Core/` → Original Prefab
- [ ] Drag **TimeController** → `Prefabs/Core/` → Original Prefab
- [ ] Drag **PromptConsole** → `Prefabs/Core/` → Original Prefab

- [ ] Save scene (Ctrl+S / Cmd+S)
- [ ] Verify blue cube icons appear in Hierarchy

---

## Phase 2: Verify Prefab System Works ✓

- [ ] Open `PlanetRoot.prefab` (double-click in Project)
- [ ] Change Planet scale to 1.1, 1.1, 1.1
- [ ] Exit prefab mode (click < arrow at top)
- [ ] Verify Earth is bigger in Scene view
- [ ] Change scale back to 1, 1, 1
- [ ] Exit prefab mode

✅ If Earth changed size → Prefabs working!

---

## Phase 3: Create ISS Mission Scene ✓

- [ ] File → New Scene → Empty
- [ ] File → Save As → `Assets/Scenes/ISS_Mission.unity`

### Add Prefabs:
- [ ] Drag `PlanetRoot.prefab` into Hierarchy
- [ ] Drag `CameraRig.prefab` into Hierarchy
- [ ] Drag `Canvas.prefab` into Hierarchy
- [ ] Drag `TransitionCanvas.prefab` into Hierarchy
- [ ] Drag `MissionContext.prefab` into Hierarchy
- [ ] Drag `SceneTransitionManager.prefab` into Hierarchy
- [ ] Drag `TimeController.prefab` into Hierarchy
- [ ] Drag `PromptConsole.prefab` into Hierarchy

### Add Unity Systems:
- [ ] Right-click Hierarchy → UI → Event System
- [ ] Right-click Hierarchy → Light → Directional Light

### Add ISS Logic:
- [ ] Right-click Hierarchy → Create Empty → Name: `ISS_MissionController`
- [ ] Add Component → Mission Space Controller
- [ ] Configure in Inspector:
  - Mission Name: `ISS`
  - Specialist Name: `ISS Flight Engineer`
  - Is Circular: ✓
  - Altitude Km: `420`
  - Inclination Deg: `51.6`
  - Personality: `Professional engineer - clear, technical, friendly`
  - Knowledge Domain: (paste ISS details)
  - Orbit Controller: (assign reference)
  - Prompt Console: (drag PromptConsole GameObject)
  - Intro Delay: `0.5`

- [ ] Save scene (Ctrl+S / Cmd+S)

---

## Phase 4: Add Scenes to Build Settings ✓

- [ ] File → Build Settings
- [ ] Drag `Hub.unity` into "Scenes In Build" (should be [0])
- [ ] Drag `ISS_Mission.unity` into "Scenes In Build" (should be [1])
- [ ] Verify order:
  ```
  [0] Hub
  [1] ISS_Mission
  ```
- [ ] Close Build Settings

---

## Phase 5: Test the System ✓

### Test Hub → ISS:
- [ ] Open Hub.unity
- [ ] Press Play ▶️
- [ ] Wait for intro
- [ ] Type: "Tell me about the ISS"
- [ ] Verify: Fade out → Black screen → Fade in → ISS scene loads
- [ ] Verify: ISS orbit appears (420km, 51.6°)

### Test ISS → Hub:
- [ ] Type: "Go back to hub"
- [ ] Verify: Fade out → Fade in → Hub scene loads

### Test Prefab Updates:
- [ ] Stop Play mode
- [ ] Open `PlanetRoot.prefab`
- [ ] Change Planet material color slightly
- [ ] Exit prefab mode
- [ ] Open Hub.unity → Verify Earth has new color
- [ ] Open ISS_Mission.unity → Verify Earth has same new color
- [ ] Change color back to original

✅ If both scenes updated → Prefabs working perfectly!

---

## Phase 6: Create Remaining Missions ✓

Repeat Phase 3 for each mission:

### GPS Mission:
- [ ] Create `GPS_Mission.unity`
- [ ] Add all prefabs
- [ ] Configure MissionSpaceController:
  - Mission: `GPS`
  - Altitude: `20200`
  - Inclination: `55`
- [ ] Add to Build Settings ([2])

### Voyager Mission:
- [ ] Create `Voyager_Mission.unity`
- [ ] Add all prefabs
- [ ] Configure MissionSpaceController:
  - Mission: `Voyager`
  - Is Circular: ❌
  - Periapsis: `200`, Apoapsis: `100000`
- [ ] Add to Build Settings ([3])

### Hubble Mission:
- [ ] Create `Hubble_Mission.unity`
- [ ] Add all prefabs
- [ ] Configure MissionSpaceController:
  - Mission: `Hubble`
  - Altitude: `540`
  - Inclination: `28.5`
- [ ] Add to Build Settings ([4])

---

## Final Verification ✓

- [ ] Test routing: Hub → ISS → Hub
- [ ] Test routing: Hub → GPS → Hub
- [ ] Test routing: Hub → Voyager → Hub
- [ ] Test routing: Hub → Hubble → Hub
- [ ] Change Earth prefab texture → Verify all 5 scenes update
- [ ] Change UI prefab style → Verify all 5 scenes update

---

## Success Criteria

✅ 5 scenes exist (Hub + 4 missions)
✅ All scenes use prefabs (blue cube icons)
✅ Changing prefab updates all scenes automatically
✅ Scene transitions work smoothly (fade in/out)
✅ Each mission shows correct orbit configuration
✅ Can return to Hub from any mission

---

## Common Issues & Solutions

**Blue icons don't appear:**
- Solution: Make sure you selected "Original Prefab" when dragging

**Prefab changes don't apply:**
- Solution: Edit the PREFAB file (Project window), not the instance (Hierarchy)

**Scene won't load:**
- Solution: Add scene to Build Settings (File → Build Settings)

**Orbit doesn't appear:**
- Solution: Check MissionSpaceController has OrbitController reference assigned

**Audio doesn't play:**
- Solution: Verify ElevenLabsSettings is assigned to PromptConsole

---

**Estimated Time:**
- Phase 1-2: 10 minutes (create prefabs)
- Phase 3-5: 15 minutes (create ISS, test)
- Phase 6: 15 minutes (create remaining missions)
- **Total: ~40 minutes**

Good luck! Check off each item as you complete it.
