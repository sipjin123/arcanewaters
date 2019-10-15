using UnityEngine;
using System;

#if IS_SERVER_BUILD

using MySql.Data.MySqlClient;

#endif

[Serializable]
public class Blueprint : RecipeItem
{
   #region Public Variables
   
   // The type of blueprint equipment if Weapon or Armor
   public Item.Category equipmentType;

   // The id of the blueprint items //prefix 100 for weapons, prefix 200 for armors
   public int bpTypeID;

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
            idString = idString.Replace("200", "");
            currName = ((Armor.Type) int.Parse(idString)).ToString();
            break;
         case Category.Weapon:
            idString = idString.Replace("100", "");
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

   public static Item getItemData (int recipeType) {
      string recipepString = recipeType.ToString();

      Item.Category itemCategory = getEquipmentType(recipeType);
      if (itemCategory == Category.Weapon) {
         Weapon.Type weaponType = Weapon.Type.None;

         foreach (var item in Enum.GetValues(typeof(Weapon.Type))) {
            string weaponString = item.ToString();
            recipepString = recipepString.Replace("100", "");
            if (recipepString == ((int)item).ToString()) {
               weaponType = (Weapon.Type) item;
            }
         }
         Weapon newWeapon = new Weapon {
            category = itemCategory,
            type = weaponType,
            itemTypeId = (int) weaponType,
            id = 0,
            count = 1,
         };
         return newWeapon;
      } else {
         Armor.Type armorType = Armor.Type.None;

         foreach (var item in Enum.GetValues(typeof(Armor.Type))) {
            string armorString = item.ToString();
            recipepString = recipepString.Replace("200", "");
            if (recipepString == ((int)item).ToString()) {
               armorType = (Armor.Type) item;
            }
         }
         Armor newArmor = new Armor {
            category = itemCategory,
            type = armorType,
            itemTypeId = (int) armorType,
            id = 0,
            count = 1,
         };
         return newArmor;
      }
   }

   public static Blueprint getEmpty () {
      return new Blueprint(0, 1000, ColorType.None, ColorType.None);
   }

   public static Item.Category getEquipmentType (int blueprintType) {
      Item.Category prefixCategory = Category.None;
      string prefixExtracted = "";
      if (blueprintType.ToString()[0] == '1') {
         prefixCategory = Category.Weapon;
         prefixExtracted = "100";
      } else if (blueprintType.ToString()[0] == '2') {
         prefixCategory = Category.Armor;
         prefixExtracted = "200";
      }
      
      int idComparison = int.Parse(blueprintType.ToString().Replace(prefixExtracted,""));

      if (prefixCategory == Category.Weapon) {
         foreach (Weapon.Type val in Enum.GetValues(typeof(Weapon.Type))) {
            if ((int) val == idComparison) {
               return Item.Category.Weapon;
            }
         }
      } else if (prefixCategory == Category.Armor) {
         foreach (Armor.Type val in Enum.GetValues(typeof(Armor.Type))) {
            if ((int) val == idComparison) {
               return Item.Category.Armor;
            }
         }
      }

      return Item.Category.None;
   }

   public override bool canBeTrashed () {
      return base.canBeTrashed();
   }

   public override string getIconPath () {
      return "Icons/Blueprint/" + this.bpTypeID;
   }
}