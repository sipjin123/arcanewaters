using System;
using System.Collections.Generic;

[Serializable]
public class TreasureDropsData {
   // The type of loot
   public Item item;

   // Chance of this loot spawning
   public float spawnChance = 100;

   // If this loot will only spawn in secret chests
   public bool spawnInSecretChest = false;
}

[Serializable]
public class TreasureDropsCollection {
   // The biome type
   public Biome.Type biomeType;

   // The data collection
   public List<TreasureDropsData> treasureDropsCollection = new List<TreasureDropsData>();
}