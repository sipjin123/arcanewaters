using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[Serializable]
public class TreasureDropsData {
   // The type of loot
   public Item item;

   // Chance of this loot spawning
   public float spawnChance = 100;

   // Determines the rarity of this object
   [XmlElement(Namespace = "RarityType")]
   public Rarity.Type rarity = Rarity.Type.Common;

   // Chance of this power up spawning
   public float powerupChance = 100;

   // The powerup type
   [XmlElement(Namespace = "PowerupType")]
   public Powerup.Type powerUp = Powerup.Type.None;

   // If this loot will only spawn in secret chests
   public bool spawnInSecretChest = false;

   // The min and max drops 
   public int dropMinCount = 1;
   public int dropMaxCount = 1;

   // The xml id for empty drops
   public const int EMPTY_DROPS = 15;
}

[Serializable]
public class LootGroupData {
   // The name of the loot group
   public string lootGroupName;

   // The id of this group data in the database
   public int xmlId;

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