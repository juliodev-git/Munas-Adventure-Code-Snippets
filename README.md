# Muna's Adventure
![screenshot](screenshot.png)
[Read More About Muna's Adventure](https://portfolium.com/entry/munas-adventure)


_______________________________________

**Muna's Adventure** is a third-person shooter with an emphasis on scavenging items from your environment to increase your chances of survival. The goal of the game is to defeat enemies to progress through the story, using a variety of consumables, plants, rations and ammunition to aid you in your quest!

_______________________________________

I challenged myself to create a fully playable prototype in one year. In that time, I developed an interactable inventory of items, specialized to store a list of instantiated scriptable objects labeled as 'items.' Using the properties of inheritance, sub-items were created that were compatible with the UI Inventory Controller System and still behave in their own unique way such as: ammunition items that reload to and fire from your weapon, or consumable items that can be used in-game to heal the player or provide defensive buffs. Additionally, I crafted all animations and made them with animation masking in mind - allowing us to use the same base animations with alternate arm animations that change depending on the weapon being used. The firing system was made using Unity's particle system with collision enabled. Adjustments to the particle system were made to account for firing speed, ricochets and aim variance - the same bullet asset being used by all firing entities by limiting bullet collision detectiom from the firing entity. Similar to bullets, an explosive grenade was created following the same collision detection properties, however these grenades have the special property of applying forces to rigidbodies in its vicinity. To wrap up the game up, I made cinematic cutscenes to introduce the characters, their immediate goals, and to drop additional information about the world as well.

_______________________________________

**Bullet.cs**: This script is attached to a particle system. Using OnParticleCollision, we can detect any colliders that were hit by our bullet particle and interact with them. In our case, we simply deduct health from any HealthControllers attached to GameObjects. The HealthController handles it from there.

**DropDownFace.cs**: This scipt is linked directly to a dropdown UI list. On Start, a list of selectable cosmetics is generated based on any cosmetic items that were found by CosmeticController. If the player selects a new cosmetic item from the list, the index of selected item is sent to CosemticController where it is enabled.

**Player.cs**: The Player script is in charge of making any changes to the main player entity (Muna). If Muna is damaged, hit by a grenade, trys to loot, or consumes an item, then any animation triggers are sent here and reflected onto her animation state. The Player's link to the Human script allows us to block certain behaviours of overhead mechanics such as menu item manipulation while reloading, hanging on a cliff, or in a damaged state. Other environmental values are found here, such as slope angles that prevent the player from running up steep inclines or perpendicular walls that can be climbed. Rotational changes are also made here based on the player's animation: by default the player faces the direction of camera-relative input, but other animations like slashing, throwing, or firing, require the player face the same direction of the camera or the input. Certain animations can also flip the animation to oppose input, or slow rotational speed. This script also offers player bone transform positions for instatiating throwables, bullets, or consumables. Links to HealthController and StaminaController have also been made to limit player movement when all stamina has been exhasuted or the player dies.
