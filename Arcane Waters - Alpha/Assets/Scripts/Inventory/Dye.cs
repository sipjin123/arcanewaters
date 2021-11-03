﻿using System;
using UnityEngine;

[Serializable]
public class Dye : Item
{
   #region Public Variables

   #endregion

   public Dye () {

   }

   public Dye (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1) {
      this.category = Category.Dye;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;
   }

   public static Dye createFromData (DyeData data) {
      if (data == null) {
         return null;
      }

      Dye hairDye = new Dye(-1, data.itemID, "", "", 100);
      hairDye.setBasicInfo(data.itemName, data.itemDescription, data.itemIconPath);
      return hairDye;
   }

   public override bool canBeUsed () {
      return true;
   }

   public override bool canBeTrashed () {
      return true;
   }

   public override bool canBeEquipped () {
      return false;
   }

   public override bool canBeStacked () {
      return true;
   }

   public override string getIconPath () {
      return "Icons/Inventory/usables_icons";
   }

   public override string getName () {
      DyeData dyeData = DyeXMLManager.self.getDyeData(itemTypeId);

      if (dyeData == null) {
         return itemName;
      }

      return dyeData.itemName;
   }

   public override string getDescription () {
      DyeData dyeData = DyeXMLManager.self.getDyeData(itemTypeId);

      if (dyeData == null) {
         return itemDescription;
      }

      return dyeData.itemDescription;
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(Rarity.Type.None);
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color>\n\n{2}\n\n",
         "#" + colorHex, getName(), getDescription());
   }

   #region Private Variables

   #endregion
}
