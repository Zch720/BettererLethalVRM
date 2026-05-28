# BettererLethalVRM

A maintained and optimized VRM mod for Lethal Company, providing a smoother experience!

**This is a improved fork of the original [BetterLethalVRM](https://github.com/OomJan/BetterLethalVRM) by OomJan and Ooseykins.**

Replace player models with custom VRMs. This client mod requires some setup to see other players as VRM avatars. This is a tool specifically for Vtuber collabs, not a general-purpose model replacement for Lethal Company. Do not expect this mod to "just work" right away with your model, some edits to toggles and materials might need to be made. There are no models included with this mod; VRoid Studio (on Steam) models are free and fully compatible.

## What's New in BettererLethalVRM?
- **Model Height & Scale Adjustment:** You can now adjust the overall height and scale of your VRM models using the `scaleSize` config to fit perfectly in-game. The size settings are automatically synchronized across the network to all clients!
- **Advanced Facial Animations:** Per-model blink and lip-sync settings with automatic VRM expression detection. The mod auto-detects VRM expressions from the model definition. If the model already uses "Blink" or "A" expressions with blendshape bindings, it will automatically use them and ignore manual index settings. Face settings are synced over the network on connect!
- **Enhanced Camera & Immersion:** Added 360-degree free-look camera support while using the terminal or climbing ladders. You can also automatically hide the first-person helmet when a VRM is active.
- **Built-in Compatibility:** Actively maintained with structural updates.

## Configuration

### General Settings
| Key | Default | Description |
|---|---|---|
| `scaleSize` | 1.0 | Scale size of VRM models, between 0.1 and 1.5. |

### Face & Lip Sync Settings
| Key | Default | Description |
|---|---|---|
| `BlinkBlendshapeIndex` | -1 | Blendshape index for blinking. -1 to disable. Fallback for models without a "Blink" expression. |
| `BlinkIntervalMin` | 2.0 | Minimum seconds between blinks. Synced over network. |
| `BlinkIntervalMax` | 6.0 | Maximum seconds between blinks. Synced over network. |
| `BlinkDuration` | 0.15 | Duration of each blink in seconds. Synced over network. |
| `MouthBlendshapeIndex` | -1 | Blendshape index for lip sync. -1 to disable. Fallback for models without an "A" expression. |
| `LipSyncSensitivitySelf` | 2.0 | Mouth sensitivity for your own voice. Local only. |
| `LipSyncSensitivityOthers` | 2.0 | Mouth sensitivity for other players' voices. Local only. |

### Camera & Immersion Settings
| Key | Default | Description |
|---|---|---|
| `FreeLookTerminalLadder` | true | Allow free camera look while using the terminal or climbing ladders. |
| `FreeLookSensitivity` | 1.0 | Mouse sensitivity for free look while in terminal or on ladder. 1.0 = default feel. |
| `HideHelmetForVRM` | true | Hide the default helmet model when a VRM is loaded for the local player. |

## VRMs Setup
Browse to your Lethal Company installation (Find the VRM folder by going to your Steam library and right clicking Lethal Company -> Manage -> Browse Local Files.)

Create a folder in your Lethal Company folder called "VRMs" (if it does not exist already) and place your VRM file into that folder. Your VRM can be named using your steamID64 (dec) or your Steam username.

* Example of using the steamID64: `76561197974711290.vrm`
* Example of using the Steam username: `Zch.vrm`

Since it is possible that two users have the same steam name, the Steam ID is preferred before the username. **This mod requires you to send your VRM to the other people you're playing with! Make sure you trust the people you are sending your VRM models to!**

### Fallback Setup
Create a VRM named `fallback.vrm` and place it into your VRMs folder. Whenever a player joins who has no personal VRM file in your "VRMs" folder, the fallback will be used.

## FAQ

**Q:** Do my friends need the mod to see my avatar?
**A:** Yes.

**Q:** My friends have the mod, why can't they see my avatar?
**A:** The avatars have to be configured on all players clients, with the VRM files named after each appropriate ID. Most Vtubers try very hard to keep their model files safe. Accidentally joining the wrong player would send them a copy of your model, and the inconvenience can be worth the protection. Also, some VRM files are very big, and downloading them each time you join a server is a lot of data.

**Q:** Why are my arms still the default character?
**A:** This mod only changes the 3rd person model. You will be able to see your own custom model while spectating other players.

**Q:** My avatar is a plain white texture, how do I fix this?
**A:** Make sure all of the materials are MToon. If changing to MToon doesn't fix things, try using the MToon10 shader if available in your version of UniVRM.

**Q:** Something about my model looks weird, bumpy, or wrong, why?
**A:** This mod requires workarounds to make VRM work in Lethal Company. Most material properties from VRM (outline, emissive, matcap) will not carry over very well.

**Q:** Does this mod work with MToon shaders?
**A:** No, but use these shaders anyways when exporting your model. VRM does not officially support the rendering pipeline (HDRP) used by Lethal Company.

**Q:** Does this mod work with spring bones?
**A:** Yes.

**Q:** Does this mod work with VSF Avatar, or some other model format?
**A:** No.

**Q:** Why are all my avatar toggles on?
**A:** You will have to create an alternative version of your VRM with unwanted parts removed.

**Q:** Why is my thumb all messed up? Why are my hands not holding things?
**A:** The animation skeletons used for VRM and Lethal Company are both very non-standard. There isn't really a fix for this, just try to embrace the crustiness of Lethal Company.

**Q:** Does this mod work with MoreCompany?
**A:** Yes.

## For Mod Developers (Compatibility)
If your mod previously had compatibility checks for the original `BetterLethalVRM`, please note that our Plugin GUID has changed to reflect our independent maintenance. 

Please add our new GUID to your compatibility list so your features apply to our users as well:
- **New Plugin GUID:** `Zch.BettererLethalVRM`
- **Original Plugin GUID:** `OomJan.BetterLethalVRM` (For reference)

## Technical Stuff (From Original Author)

There are a lot of hacks used in this mod:
- **Loading the template materials**: I wait until the ship scene is loaded, then find the catwalk object outside the ship ("CatwalkShip"). This object has a renderer with a cutout material that I can use as a template.
- **Posing the template player skeleton**: When a scene is loaded, it manually sets the bone angles for the upper arms, legs, and hands to match the T-Pose expected by VRM.
- **Spawning a VRM**: There will only ever be one VRM spawned for each player. Lethal Company has separate renderers for player, ragdoll, or mimic, but new VRMs are not spawned for each state for performance reasons.
- **Adding reference bones**: Extra transforms are added for each humanoid bone using the template player bone rotations as a base.
- **Handling visibility**: Visibility is handled using simple checks against the player's death state.

## Special Thanks

- Huge thanks to **[OomJan](https://github.com/OomJan)** and **[Ooseykins](https://twitter.com/Ooseykins)** for creating the initial foundation of this mod and sharing their work.

## Maintainers
- [Zch720](https://github.com/Zch720)
- [Kiri487](https://github.com/Kiri487)

## Source & Compilation

**Source Code:** https://github.com/Zch720/BettererLethalVRM

### Required software
* Unity 2022.3.9f1 via Unity Hub
* Microsoft Visual Studio 2022

Check out the GIT repository above and open the Unity project containted in the folder "Unity" and build the project into a new folder named "Build" within the Unity project directory. Now you can open the solution in the root folder.

## License: MIT License
Copyright 2024 OomJan, Ooseykins
Copyright 2026 Zch720

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.