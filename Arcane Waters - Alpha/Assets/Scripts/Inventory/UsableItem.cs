using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class UsableItem : Item {
   #region Public Variables

   // The Type
   public enum Type {
      None = 0, HairDye = 1, ShipSkin = 2, Haircut = 3,
      STR_Buff = 4, VIT_Buff = 5, PRE_Buff = 6, SPT_Buff = 7, LUK_Buff = 8, INT_Buff = 9,
      Healing_Low = 10, Healing_Mid = 11, Healing_High = 12,
      ALL_ATK_Buff = 13, PHYS_ATK_Buff = 14, EARTH_ATK_Buff = 15, WATER_ATK_Buff = 16, WIND_ATK_Buff = 17, FIRE_ATK_Buff = 18,
      ALL_DEF_Buff = 19, PHYS_DEF_Buff = 20, EARTH_DEF_Buff = 21, WATER_DEF_Buff = 22, WIND_DEF_Buff = 23, FIRE_DEF_Buff = 24
   }

   // The usable item type
   public Type itemType;

   #endregion

   public UsableItem (int id, Category category, int itemTypeId, int count, string newPalettes, string data) {
      this.id = id;
      this.category = category;
      this.itemTypeId = itemTypeId;
      this.itemType = (Type) itemTypeId;
      this.paletteNames = newPalettes;
      this.count = count;
      this.data = data;
   }

   public void use () {

   }

   public override string getName () {
      switch (itemType) {
         case Type.HairDye:
            return "Hair Dye (" + PaletteSwapManager.getColorName(Item.trimItmPalette(paletteNames)) + ")";
         case Type.ShipSkin:
            string name = "Ship Skin (" + getSkinType() + ")";
            return name.Replace("_", " ");
         case Type.Haircut:
            return "Haircut";
      }

      return "Usable item";
   }

   public override string getDescription () {
      switch (itemType) {
         case Type.HairDye:
            return "This item allows you to change your hair color to " + PaletteSwapManager.getColorName(Item.trimItmPalette(paletteNames)) + ".";
         case Type.Haircut:
            return "This item allows you to change your haircut.";
      }

      return "Unknown!";
   }

   public override bool canBeUsed () {
      return true;
   }

   public Ship.SkinType getSkinType () {
      foreach (string kvp in data.Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }

         // Get the left and right side of the equal
         string key = kvp.Split('=')[0];
         string value = kvp.Split('=')[1];

         if ("skinType".Equals(key)) {
            return (Ship.SkinType) System.Convert.ToInt32(value);
         }
      }

      return Ship.SkinType.None;
   }

   public HairLayer.Type getHairType () {
      foreach (string kvp in data.Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }

         // Get the left and right side of the equal
         string key = kvp.Split('=')[0];
         string value = kvp.Split('=')[1];

         if ("hairType".Equals(key)) {
            return (HairLayer.Type) System.Convert.ToInt32(value);
         }
      }

      return HairLayer.Type.Female_Hair_1;
   }

   #region Private Variables

   #endregion
}
