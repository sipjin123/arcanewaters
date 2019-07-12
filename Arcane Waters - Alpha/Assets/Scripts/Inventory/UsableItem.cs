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
   }

   // The usable item type
   public Type itemType;

   #endregion

   public UsableItem (int id, Category category, int itemTypeId, int count, ColorType color1, ColorType color2, string data) {
      this.id = id;
      this.category = category;
      this.itemTypeId = itemTypeId;
      this.itemType = (Type) itemTypeId;
      this.color1 = color1;
      this.color2 = color2;
      this.count = count;
      this.data = data;
   }

   public void use () {

   }

   public override string getName () {
      switch (itemType) {
         case Type.HairDye:
            ColorDef colorDef = ColorDef.get(color1);
            return "Hair Dye (" + colorDef.colorName + ")";
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
            ColorDef colorDef = ColorDef.get(color1);
            return "This item allows you to change your hair color to " + colorDef.colorName + ".";
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
