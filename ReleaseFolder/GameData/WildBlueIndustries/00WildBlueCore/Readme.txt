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

New Parts

- BFP-5 Backpack Paramotor: This electrically powered fan provides forward thrust to kerbals wanting to fly around with their parachutes. Carry extra batteries for longer flight times.

---CHANGES---

- Kerbals now have 6 inventory slots and slightly increased volume and carrying capacity- thanks JadeOfMaar!
- Made some KerbalGear optimizations to improve framerates, organize configurations, and cut memory usage.
- Added WBIModuleEVAAblator, an EVA part module designed to help kerbals keep cool.
- Added WBIModuleEVAResourceTransfer, an EVA part module designed to make a cargo part's resources available to the kerbal- much like part resources are usable by parts.
- Added WBIModuleEVAMotor, an EVA part module that provides motive force for a kerbal on EVA.
- The Z-100 battery pack can now be used by kerbals to power various devices if carried in their inventory.
- Fixed issue in DialogManager preventing proper initialization of GUI dialogs.
- Fixed issue with mismatched suit textures and suit meshes.
- Fixed missing localized strings issue in the KerbalGear prop editor window.

Sample Configs

WBIModuleEVAAblator

MODULE
{
    name = WBIModuleWearableItem
    moduleID = EVA Cooling Pack
    evaModules = WBIModuleEVAResourceTransfer;WBIModuleEVAAblator
}

RESOURCE
{
    name = Ablator
    amount = 10
    maxAmount = 10
}

WBIModuleEVAResourceTransfer

MODULE
{
    name = WBIModuleWearableItem
    moduleID = Resource Provider
    evaModules = WBIModuleEVAResourceTransfer
}

RESOURCE
{
    name = ElectricCharge
    amount = 100
    maxAmount = 100
}

--END CHANGES--

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