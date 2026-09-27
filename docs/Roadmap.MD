
# RE7 Randomizer roadmap for 2026 and general thoughts

All estimates provided are non-binding and subject to change.

## 1. Inventory and recipe randomization

Inventory:

* Random start inventory for both Ethan and Mia
* Ability to change item stack sizes

Recipes:

* Only potential softlock: `SerumMaterialA + 1x SerumMaterialB -> 1x DybbukMedicine`
* Always enabled:
  * Softlock prevention
  * New recipes are visible in the crafting menu
* Option to replace original recipes
* Option to disable nonsensical recipes entirely => Different modes
* Probabilities for most wanted recipes?
* Pre-defined additional recipes like in RE4RR:
  * Chem Fluid + Chem Fluid => Strong Chem Fluid
  * Strong Chem Fluid + Separating agent => 3x Chem Fluid
  * Separating Agent + Ammo => Gunpowder
  * etc.

*Estimated completion: February*  
Status: **Completed ✅**

## 2. Item and weapon randomization

* Options can pretty much be copied from RE4R except for money and treasures
* Need to be especially aware of early game softlocks!
  * Bolt cutters
  * Axe for Mia 1
  * Knife from Deputy
  * etc.
* Particular focus on key items!
* Be able to randomize weapon stats

*Estimated completion: March*  
Status: **Completed ✅**

==================== BETA TESTING PHASE 1 ====================

## 3. Enemy randomization

* Change via.Transform properties => Location, Rotation, Scale
* Health can be modified easily
* Speed too; but some enemies have their own values in special ActionControllers!
* Goal: *Enhance enemy variety, especially the Molded*
* Enemy Spawn Controllers are tricky
  * Can't just copy enemies from .scn file A to .scn file B
  * Bosses are particulary finicky
* Certain bosses might not be spawnable in random locations:
  * Jack 3 requires fixed boat house waypoints, must be investigated
  * Same for the Evie final boss fight
* Introduce item drops (can be copied from Marge)
* Be particularly careful with roaming enemies (non-mutated Jack and Marge)
* Roaming Jack can't die => Careful with elevators, they won't move!
* Must be tested *very* carefully

*Estimated completion: May*  
Status: **Completed ✅**

## 4. Quality of Life

* DX11 support
* Bugfixes
* Softlock prevention
* Code refactoring

*Estimated completion: June*  
Status: **Not completed, postponed **

==================== BETA TESTING PHASE 2 ====================

## 5. 100 % DLC content in main game

* Goal: *Full item and enemy integration*
* Challenging, can't just bring the assets over
* Banned Footage content is easier to mod in than End of Zoe and Not a Hero
* In-depth reverse engineering required
* In the worst case binary patching might be necessary!
* Ethan Must Die crate quality concept in main game
* Not a Hero and End of Zoe must follow the staged compatibility and certification process in [DLCIntegrationPlan.MD](DLCIntegrationPlan.MD)

*Estimated completion: August*  
Status: **Not completed, postponed **

## 6. Chapter shuffling

* Certain triggers might break
* Possibly certain chapter order subsets must be preserved
* Testing required
* Have to manipulate flags (userdata/globalvariables.uvar.2)

*Estimated completion: September*  
Status: **Not completed, postponed **

### Initial public access: **October**
