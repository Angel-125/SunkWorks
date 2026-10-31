WildBlueCore

A Lightweight plugin used by several Wild Blue mods.

---INSTALLATION---

Copy the contents of the mod's GameData directory into your GameData folder. Specifically you'll need:

GameData
	WildBlueIndustries
		WildBlueCore

If these directories already exist, then delete the existing ones before installing the latest update.

HOW TO FIX MISSING PART MODULES ERROR
This update renames a number of Wild Blue Industries' part modules and may cause KSP to complain when you try to load your craft files.
To fix this issue, follow the steps here: https://github.com/Angel-125/WildBlueCore/wiki/How-To-Fix-Missing-Part-Modules-Warning

---CHANGES---

Changes

- Fixed an EVA motor throttle-gauge NullReferenceException during scene teardown. UI callbacks are removed before motor cleanup, and throttle updates check that FlightGlobals is available.
- Fixed EVA parachute visibility when wearing KerbalGear, including the packed chute appearing immediately on EVA and the canopy remaining visible during deployment.
- Added evaChuteModel to WBIModuleWearableItem to control both packed chute visibility and model selection. Options are None (default), Stock, Standalone, Jetpack, and Container.
- Changed hideStockPacksWhenWorn to default to false. Back-mounted wearables no longer hide stock packs solely because of their attachment location; replacement backpacks must explicitly enable hiding.
- Removed showChuteTransforms. Any evaChuteModel other than None preserves an eligible carried parachute even when hideStockPacksWhenWorn is true. Stock preserves KSP's inventory-based model selection; Standalone, Jetpack, and Container preserve the specified model. None follows normal pack visibility and does not force the chute invisible when stock packs are allowed.
- Updated the paramotor to hide other stock packs while showing the standalone parachute against the Kerbal's back. The selected chute hierarchy stays active, fixing the packed chute's shifted position and tilt without position offsets.
- Chute model changes are deferred while the canopy is open. Removing the requesting wearable restores stock model selection and pack visibility, including hidden flag decals.

Wearable configuration defaults:

    hideStockPacksWhenWorn = false
    evaChuteModel = None

The paramotor explicitly uses:

    hideStockPacksWhenWorn = true
    evaChuteModel = Standalone

For older configs, replace showChuteTransforms = true with evaChuteModel = Stock or an explicit model. Remove showChuteTransforms = false and omit evaChuteModel (or set it to None). Explicit Stock now preserves the chute when other stock packs are hidden. Diving packs should inherit None. Invalid evaChuteModel values warn and fall back to None. Open canopies remain visible regardless of the setting.

---END CHANGES---

---ACKNOWLEDGEMENTS

---LICENSE---
Art Assets, including .mu, .mbm, and .dds files are copyright 2022 by Michael Billard, All Rights Reserved.
All source code is GPLV3

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Portions of this code were done in collaboration with ChatGPT. Thanks for handling the drudgery!

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
