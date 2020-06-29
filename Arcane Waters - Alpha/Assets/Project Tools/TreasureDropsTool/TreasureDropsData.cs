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
public class LootGroupData {
   // The id of this group data in the database
   public int xmlId;

   // The name of the loot group
   public string lootGroupName;

   // The data collection
   public List<TreasureDropsData> treasureDropsCollection = new List<TreasureDropsData>();

   // The biome type
   public Biome.Type biomeType = Biome.Type.None;

   // The default loot data returned by any entity with no assigned data
   public static LootGroupData DEFAULT_LOOT_GROUP = new LootGroupData {
      biomeType = Biome.Type.None,
      xmlId = 0,
      treasureDropsCollection = new List<TreasureDropsData> {
         new TreasureDropsData{
            item = new Item{
               category = Item.Category.CraftingIngredients,
               count = 1,
               itemName = CraftingIngredients.getName(CraftingIngredients.Type.Wood),
               itemTypeId = (int) CraftingIngredients.Type.Wood,
               iconPath = CraftingIngredients.getIconPath(CraftingIngredients.Type.Wood)
            },
            spawnChance = 100,
            spawnInSecretChest = false
         }
      }
   };
}