using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[System.Serializable]
public class Perk
{
   // The ID for unassigned perk points
   public const int UNASSIGNED_ID = 0;

   // The maximum number of points that can be assigned to a perk
   public const int MAX_POINTS_BY_PERK = 3;

   public enum Category
   {
      None = 0,
      Healing = 1,
      Gun = 2,
      Sword = 3,
      WalkingSpeed = 4,
      MeleeDamage = 5,
      RangedDamage = 6,
      Health = 7,
      CropGrowthSpeed = 8,
      ShipMovementSpeed = 9,
      ShipDamage = 10,
      ShipHealth = 11,
      ShopPriceReduction = 12,
      ExperienceGain = 13,
      ItemDropChances = 14,
      MiningDrops = 15
   }

   // The unique ID of the perk using the points
   public int perkId;

   // The points a player has for this perk
   public int points;

   public Perk () { }

   public Perk (int perkId, int points) {
      this.perkId = perkId;
      this.points = points;
   }

   public string getCategoryDisplayName () {
      PerkData data = PerkManager.self.getPerkData(perkId);
      Perk.Category category = getCategory(data.perkCategoryId);

      return getCategoryDisplayName(category);
   }

   public static Perk.Category getCategory (int categoryId) {
      return (Perk.Category) categoryId;
   }

   public static int getCategoryId (Perk.Category category) {
      return (int) category;
   }

   public static string getCategoryDisplayName (Perk.Category category) {
      // Display names only need to be overridden if adding spaces to the camelCase name doesn't apply
      switch (category) {
         case Category.None:
            return "Unassigned";

         default:
            return category.ToString().SplitCamelCase();
      }
   }

#if IS_SERVER_BUILD
   public Perk (MySqlDataReader dataReader) {
      this.perkId = dataReader.GetInt32("perkId");
      this.points = dataReader.GetInt32("perkPoints");
   }
#endif

}