using UnityEngine;
using System;

#if IS_SERVER_BUILD

using MySql.Data.MySqlClient;

#endif

[Serializable]
public class Blueprint : RecipeItem
{
   #region Public Variables

   // The different blueprint statuses
   public enum Status {
      Craftable = 1,
      NotCraftable = 2,
      MissingRecipe = 3// = Missing CraftableItemRequirements data
   }

   // The type of blueprint equipment if Weapon or Armor
   public Item.Category equipmentType;

   // The id of the blueprint items //prefix 100 for weapons, prefix 200 for armors
   public int bpTypeID;

   // Prefixes for ID
   public const string WEAPON_PREFIX = "100";
   public const string ARMOR_PREFIX = "200";

   #endregion Public Variables

   public Blueprint () {
      this.bpTypeID = 1000;
   }

#if IS_SERVER_BUILD

   public Blueprint (MySqlDataReader dataReader) {
      this.bpTypeID = DataUtil.getInt(dataReader, "itmType");
      this.id = DataUtil.getInt(dataReader, "itmId");
      this.category = (Item.Category) DataUtil.getInt(dataReader, "itmCategory");
      this.itemTypeId = DataUtil.getInt(dataReader, "itmType");
      this.data = DataUtil.getString(dataReader, "itmData");

      // Defaults
      this.color1 = (ColorType) DataUtil.getInt(dataReader, "itmColor1");
      this.color2 = (ColorType) DataUtil.getInt(dataReader, "itmColor2");

      foreach (string kvp in this.data.Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }
      }
   }

#endif

   public Blueprint (int id, int recipeType, int primaryColorId, int secondaryColorId) {
      this.category = Category.Blueprint;
      this.id = id;
      this.bpTypeID = recipeType;
      this.itemTypeId = (int) recipeType;
      this.count = 1;
      this.color1 = (ColorType) primaryColorId;
      this.color2 = (ColorType) secondaryColorId;
      this.data = "";
   }

   public Blueprint (int id, int recipeType, ColorType primaryColorId, ColorType secondaryColorId) {
      this.category = Category.Blueprint;
      this.id = id;
      this.bpTypeID = recipeType;
      this.itemTypeId = (int) recipeType;
      this.count = 1;
      this.color1 = primaryColorId;
      this.color2 = secondaryColorId;
      this.data = "";
   }

   public Blueprint (int id, int itemTypeId, ColorType color1, ColorType color2, string data, int count = 1) {
      this.category = Category.Blueprint;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.bpTypeID = itemTypeId;
      this.color1 = color1;
      this.color2 = color2;
      this.data = data;
   }

   public override string getDescription () {
      return "A blueprint design for: "+getItemData(bpTypeID).getDescription();
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}",
         "#" + colorHex, getName(), color1, color2, getDescription());
   }

   public override string getName () {
      string currName = "";
      string idString = bpTypeID.ToString();
      Item.Category newCategory = getEquipmentType(bpTypeID);

      switch (newCategory) {
         case Category.Armor:
            idString = idString.Replace(ARMOR_PREFIX, "");
            currName = int.Parse(idString).ToString();
            break;
         case Category.Weapon:
            idString = idString.Replace(WEAPON_PREFIX, "");
            currName = ((Weapon.Type) int.Parse(idString)).ToString();
            break;
      }

      if (newCategory == Category.Weapon || newCategory == Category.Armor) {
         return getName(bpTypeID) + " Design";
      }
      return "Missing Design";
   }

   public static string getName (int recipeTypeID) {
      return getItemData(recipeTypeID).getName();
   }

   public static Item getItemData (int bpTypeID) {
      string stringID = bpTypeID.ToString();

      // Determine if the blueprint is for a weapon
      if (stringID.StartsWith(WEAPON_PREFIX)) {
         
         // Extract the typeId of the weapon
         stringID = stringID.Substring(WEAPON_PREFIX.Length, (stringID.Length - WEAPON_PREFIX.Length));

         // Convert to a weapon type
         Weapon.Type weaponType = (Weapon.Type) int.Parse(stringID);
         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weaponType);

         // Create the weapon object
         Weapon newWeapon = new Weapon(0, weaponType);
         newWeapon.setBasicInfo(weaponData.equipmentName, weaponData.equipmentDescription, weaponData.equipmentIconPath);

         return newWeapon;
      } else {
         // Extract the typeId of the armor
         stringID = stringID.Substring(ARMOR_PREFIX.Length, (stringID.Length - ARMOR_PREFIX.Length));

         // Convert to a armor type
         int armorType = int.Parse(stringID);
         ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(armorType);

         // Create the armor object
         Armor newArmor = new Armor(0, armorType);
         newArmor.setBasicInfo(armorData.equipmentName, armorData.equipmentDescription, armorData.equipmentIconPath);

         return newArmor;
      }
   }

   public static Blueprint getEmpty () {
      return new Blueprint(0, 1000, ColorType.None, ColorType.None);
   }

   public static Item.Category getEquipmentType (int blueprintType) {
      Item.Category prefixCategory = Category.None;
      string prefixExtracted = "";
      if (blueprintType.ToString().StartsWith(WEAPON_PREFIX)) {
         prefixCategory = Category.Weapon;
         prefixExtracted = WEAPON_PREFIX;
      } else if (blueprintType.ToString().StartsWith(ARMOR_PREFIX)) {
         prefixCategory = Category.Armor;
         prefixExtracted = ARMOR_PREFIX;
      }
      
      int idComparison = int.Parse(blueprintType.ToString().Replace(prefixExtracted,""));

      if (prefixCategory == Category.Weapon) {
         if (Enum.IsDefined(typeof(Weapon.Type), idComparison)) {
            return Item.Category.Weapon;
         }
      } else if (prefixCategory == Category.Armor) {
         return Item.Category.Armor;
      }

      return Item.Category.None;
   }

   public override bool canBeTrashed () {
      return base.canBeTrashed();
   }

   public override bool canBeStacked () {
      return true;
   }

   public override string getIconPath () {
      string stringID = bpTypeID.ToString();

      if (stringID.StartsWith(WEAPON_PREFIX)) {
         // Extract the typeId of the weapon
         stringID = stringID.Substring(WEAPON_PREFIX.Length, (stringID.Length - WEAPON_PREFIX.Length));

         // Get the icon path based on the weapon type
         return "Icons/Blueprint/" + (Weapon.Type) int.Parse(stringID);
      } else if (bpTypeID.ToString().StartsWith(ARMOR_PREFIX)) {
         // Extract the typeId of the armor
         stringID = stringID.Substring(ARMOR_PREFIX.Length, (stringID.Length - ARMOR_PREFIX.Length));

         // Get the icon path based on the armor type
         return "Icons/Blueprint/" + int.Parse(stringID);
      } else {
         return "Icons/Blueprint/" + this.bpTypeID;
      }
   }
}