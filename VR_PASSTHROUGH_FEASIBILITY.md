# VR Pass-Through Mode Switching - Feasibility Investigation

**Date:** 2025-01-28  
**Investigator:** Matteo  
**Status:** ✅ **FEASIBLE - MODERATE DIFFICULTY**

---

## Executive Summary

**Chef, switching between pass-through (AR) and normal VR modes between scenes is absolutely feasible and not difficult.** It's a **moderate complexity** task that's well-supported by the Meta XR SDK you're already using.

**Bottom Line:**
- ✅ **Technically possible:** Meta Quest SDK fully supports runtime pass-through switching
- ✅ **Difficulty:** Moderate (2-4 hours implementation + testing)
- ✅ **Your setup:** You already have Meta XR SDK 78.0.0 installed and an `ARHub.unity` scene
- ⚠️ **Note:** Pass-through is currently disabled in your OculusProjectConfig.asset

---

## Current Project State

### ✅ What You Have:
1. **Meta XR SDK v78.0.0** - Installed and configured
2. **OpenXR** - Configured as XR provider
3. **ARHub.unity** - Already in build settings (line 27)
4. **Scene Transition System** - `SceneTransitionManager.cs` handles scene loading
5. **Quest 3 Support** - Configured for Quest devices

### ⚠️ What Needs Configuration:
1. **Pass-through Feature:** Currently disabled in `OculusProjectConfig.asset`
   - Line 43: `insightPassthroughEnabled: 0` (needs to be `1`)
2. **No Pass-through Manager:** No code yet to control pass-through per scene

---

## Technical Approach

### Method 1: Per-Scene Pass-through Component (Recommended)

**How it works:**
- Each scene has a `PassthroughController` component
- On scene load, component checks desired mode (pass-through vs VR)
- Enables/disables `OVRPassthroughLayer` accordingly
- Works seamlessly with your existing `SceneTransitionManager`

**Difficulty:** ⭐⭐⭐ (Moderate)
- 1-2 hours implementation
- 1-2 hours testing and refinement

**Pros:**
- Clean, scene-based configuration
- Easy to maintain
- Works with your existing scene transition system
- No global state management needed

**Cons:**
- Requires component on each scene
- Minor delay during scene transition (~50-100ms)

---

### Method 2: Central Pass-through Manager (Alternative)

**How it works:**
- Single `PassthroughManager` singleton (like your `SceneTransitionManager`)
- Scene names or tags determine pass-through mode
- Automatically switches when scenes load

**Difficulty:** ⭐⭐⭐⭐ (Moderate-High)
- 2-3 hours implementation
- More complex state management

**Pros:**
- Centralized control
- Scene-independent logic

**Cons:**
- More complex architecture
- Harder to debug scene-specific issues

---

## Implementation Details

### Core Components Needed:

1. **PassthroughController.cs** (Per-scene component)
   ```csharp
   public class PassthroughController : MonoBehaviour
   {
       [Header("Passthrough Settings")]
       public bool enablePassthrough = false; // Set in Inspector per scene
       
       private OVRPassthroughLayer passthroughLayer;
       
       void Start()
       {
           // Create or get passthrough layer
           // Enable/disable based on enablePassthrough flag
       }
   }
   ```

2. **Project Settings Update:**
   - Enable `insightPassthroughEnabled` in OculusProjectConfig.asset
   - Ensure OpenXR has passthrough extension enabled

3. **Scene Configuration:**
   - Add `PassthroughController` to ARHub scene (enablePassthrough = true)
   - Add `PassthroughController` to VR scenes (enablePassthrough = false)

---

## Technical Challenges & Solutions

### Challenge 1: Pass-through Initialization Timing
**Issue:** OVRPassthroughLayer must be initialized after XR is ready  
**Solution:** Use `Start()` or `OnEnable()` with XR readiness check

### Challenge 2: Scene Transition Smoothness
**Issue:** Brief visual glitch when switching modes  
**Solution:** Your `SceneTransitionManager` fade already handles this perfectly

### Challenge 3: Device Compatibility
**Issue:** Not all Quest devices support pass-through equally  
**Solution:** Runtime feature check with fallback to VR mode

---

## Difficulty Breakdown

| Task | Time | Difficulty |
|------|------|-----------|
| Enable pass-through in project config | 5 min | ⭐ (Trivial) |
| Create PassthroughController component | 45 min | ⭐⭐ (Easy) |
| Integrate with SceneTransitionManager | 30 min | ⭐⭐ (Easy) |
| Configure scenes (ARHub + VR scenes) | 15 min | ⭐ (Trivial) |
| Testing & refinement | 60-90 min | ⭐⭐⭐ (Moderate) |
| **Total** | **2-4 hours** | **⭐⭐⭐ (Moderate)** |

---

## Recommended Implementation Plan

### Phase 1: Enable Pass-through Feature (15 minutes)
1. Enable `insightPassthroughEnabled` in OculusProjectConfig.asset
2. Verify OpenXR passthrough extension is enabled
3. Test basic pass-through in a simple scene

### Phase 2: Create PassthroughController (1 hour)
1. Create `Assets/Scripts/VR/PassthroughController.cs`
2. Implement initialization logic
3. Add Inspector toggle for pass-through enable/disable
4. Add device compatibility checks

### Phase 3: Integration (30 minutes)
1. Add `PassthroughController` to ARHub scene
2. Add `PassthroughController` to VR scenes (Hub, ISS, GPS, Hubble, Voyager)
3. Configure each scene's desired mode

### Phase 4: Testing (1-2 hours)
1. Test ARHub → VR scene transition
2. Test VR scene → ARHub transition
3. Verify no visual glitches
4. Test on Quest device (not just Editor)

---

## Code Structure Example

```
Assets/Scripts/
  └── VR/
      └── PassthroughController.cs  (new)
```

**Integration Points:**
- Works alongside `SceneTransitionManager.cs` (no changes needed)
- Scene-specific configuration via Inspector
- Automatic initialization on scene load

---

## Important Considerations

### ✅ Pros of This Approach:
- **Scene-based control:** Each scene declares its own mode
- **Works with existing system:** No changes to `SceneTransitionManager`
- **Clean architecture:** Follows Unity component pattern
- **Easy to debug:** Issues isolated per scene

### ⚠️ Things to Watch:
- **Performance:** Pass-through has slightly higher CPU/GPU usage
- **Battery:** Pass-through may drain battery faster on Quest
- **User comfort:** Some users prefer VR-only for immersion
- **Hardware:** Quest 1 doesn't support pass-through (you're targeting Quest 3, so fine)

---

## Testing Checklist

- [ ] Pass-through works in ARHub scene
- [ ] VR mode works in Hub, ISS, GPS, Hubble, Voyager scenes
- [ ] Transitions between modes are smooth (fade helps)
- [ ] No visual artifacts when switching
- [ ] Works on Quest 3 hardware (not just Editor)
- [ ] Fallback works if pass-through unavailable

---

## Meta SDK Documentation References

- **OVRPassthroughLayer API:** Unity SDK documentation
- **OpenXR Passthrough Extension:** Meta Developer Portal
- **Feature Support:** Device compatibility matrix

---

## Recommendation

**Chef, this is absolutely doable and not difficult.** The moderate difficulty comes from:
1. Understanding Meta SDK pass-through API (1 hour learning curve)
2. Testing on actual Quest hardware (necessary for validation)

**I recommend Method 1 (Per-Scene Component)** because:
- It's the simplest approach
- Works naturally with your existing architecture
- Easy to configure per scene in Inspector
- No complex global state management

**Estimated timeline:** 2-4 hours total (implementation + testing)

---

## Next Steps

**If you want to proceed, I can:**
1. ✅ Enable pass-through in project config
2. ✅ Create `PassthroughController.cs` component
3. ✅ Configure ARHub scene with pass-through
4. ✅ Configure VR scenes without pass-through
5. ✅ Test and refine

**Or, if you want more investigation:**
- I can create a minimal prototype to demonstrate the concept
- I can search for specific Meta SDK code examples
- I can review your existing scenes to identify integration points

**Your call, Chef.** This is definitely feasible, and the implementation is straightforward. The hardest part is testing on Quest hardware, which is always necessary for VR features anyway.

---

## Questions for You

1. **Which scene should be pass-through?** (I see ARHub.unity - is that the one?)
2. **Do you want a toggle/button to switch modes at runtime?** (or just scene-based?)
3. **Should we implement this now, or do you want to see a prototype first?**

