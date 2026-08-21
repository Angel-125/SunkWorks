SunkWorks Maritime Technologies

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		SunkWorks
		WildBlueCore
	ModuleManager.dll (the latest version is included)


HOW TO FIX MISSING PART MODULES ERROR
This update renames a number of Wild Blue Industries' part modules and may cause KSP to complain when you try to load your craft files.
To fix this issue, follow the steps here: https://github.com/Angel-125/WildBlueCore/wiki/How-To-Fix-Missing-Part-Modules-Warning

---CHANGES---

IMPORTANT NOTE:

SunkWorks now requires Harmony for KSP. Be sure to download Harmony for KSP before downloading the latest Sandcastle.
You can find it here: https://github.com/KSPModdingLibs/HarmonyKSP
And on CKAN.


SunkWorks

New Parts

- SCAV-3 Supercavitator: Based on a real-world concept, this part lets submarines fly underwater at inappropriate speeds.

Changes

- Deprecated the boat hull parts- development halted on them years ago...
- Made several improvements to WBIDiveComputer to improve auto-trim control, buoyancy, and the ability to maintain depth. It's not perfect but it's better than before, and you still need to do manual tuning of your specific boat.
- The Sonar Range Finder now has Sonar Vision- the ability to render a wireframe of the seabed to (slightly) improve your chances of not slamming into the ocean floor.
- New SunkWorks Settings: Lets you enable/disable the use of aquatic engines and RCS while supercavitated. By default, you can't use them.
- Added WBINeutralBuoyancy- automatically adjusts buoyancy for underwater bases and tracks parts added through EVA construction.
- Added WBISupercavitator- calculates cavity formation, geometry, strength, resource consumption, orientation, and coverage.
- Added WBISupercavitatorFX- creates the procedural cavity mesh and renders the transparent shell, diagnostic rings, and animated foam.
- Added WBISupercavitationController- a VesselModule that calculates cavity coverage and drag changes once per physics tick.
- Added WBISupercavitationDragPatch- Harmony patch for stock water drag.
- Added WBISonarView- a configurable, wireframe overlay of the seabed.
- Added EVARagdollBuoyancyPatch- Harmony support for EVA buoyancy behavior.
- Fixed issue where kerbals walking on the seabed would suddenly lose buoyancy control, ragdoll, and shoot to the surface.
- Fixed issue where tanks didn't retain their tank type state after launching from the VAB/SPH.

Wild Blue Core

New Parts

- BFP-5 Backpack Paramotor: This electrically powered fan provides forward thrust to kerbals wanting to fly around with their parachutes. Carry extra batteries for longer flight times.

Changes

- Kerbals now have 6 inventory slots and slightly increased volume and carrying capacity- thanks JadeOfMaar!
- Made some KerbalGear optimizations to improve framerates, organize configurations, and cut memory usage.
- Added WBIModuleEVAAblator, an EVA part module designed to help kerbals keep cool.
- Added WBIModuleEVAResourceTransfer, an EVA part module designed to make a cargo part's resources available to the kerbal- much like part resources are usable by parts.
- Added WBIModuleEVAMotor, an EVA part module that provides motive force for a kerbal on EVA.
- The Z-100 battery pack can now be used by kerbals to power various devices if carried in their inventory.
- Fixed issue in DialogManager preventing proper initialization of GUI dialogs.
- Fixed issue with mismatched suit textures and suit meshes.
- Fixed missing localized strings issue in the KerbalGear prop editor window.

--END CHANGES--

---LICENSE---
Art Assets, including .mu, .png, and .dds files are copyright 2021 by Michael Billard, All Rights Reserved.

Sound effects licensed from Pond 5 and may NOT be redistributed outside of this mod.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2026 by Michael Billard (Angel-125)

    This source code is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.