Purpose: Many mods for Elden Ring, such as 

Sword Mastery (https://www.nexusmods.com/eldenring/mods/1890), 

Call of the Abyss/Garden of Eyes/Faded Burn/Other Anime Movesets, 

Moveset Animation Remix's (https://www.nexusmods.com/eldenring/mods/920),

and Clever's Moveset Modpack (https://www.nexusmods.com/eldenring/mods/1928)

feature greatly enhanced player speeds, however they do not enhance enemy speeds to compensate, leading to a fundamental balancing disparity. This is made worse by the fact that enemies in Elden Ring frequently have long delays. This tool (? Sort of a tool?) aims to fix that by providing easy speed-up gradients to the start of enemy attacks and projectile spawns to make combat feel less one-sided. In the space of less than an hour, you can modify the startup speeds of every single enemy in the game.

Instructions: Open the Program.cs in Visual Studio, click "Run", and follow the instructions on the Console.

Further functionalities planned...

Adding support for a more realistic jumping animation for enemies,

Lengthening the Blend frames for animations slightly on certain attacks,

Reducing the amount of downtime between enemy attacks,

Adding a berserk mode for certain attacks,

Adding random delays to certain attacks, with a user-controlled percentage.

Adding the spEffect of your choice if an enemy is in the air (for Dynamic Camera mods) or before an attack (user-controlled, for Perilous Attacks in Deflect mods).

Adding downtime after certain big attacks.

Many thanks to the following...

Created with great help from Nordgaren (who wrote ~50% of the code and helped me learn Visual Studio) and Gomp-DS (whose GetRumbleCamIds https://github.com/GompDS/GetRumbleCamIds project was used as a base for this project).

Meowmaritus for providing SoulsAssetPipeline (https://github.com/Meowmaritus/SoulsAssetPipeline), and for tips and tricks on other TAE-related things.

AinTunez, whose fork (https://github.com/AinTunez/RoguelikeSouls) of Grimrukh's Roguelike Souls (https://github.com/Grimrukh/RoguelikeSouls) I dissected as reference material.

JKAnderson (and his crew's) SoulsFormats (https://github.com/JKAnderson/SoulsFormats).

Hopefully I got everyone, if you felt like you were part of this project's creation just ping me on Discord or something and I'll add you.
