using System;
using UnityEngine;

[Serializable]
public class ShipSkin : Item {
   #region Public Variables

   // The type of the ship skin
   public Ship.SkinType skinType;

   // The type of the ship
   public Ship.Type shipType;

   #endregion

   public ShipSkin () {

   }

   public ShipSkin (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1, Ship.SkinType skinType = Ship.SkinType.None, Ship.Type shipType = Ship.Type.None) {
      this.category = Category.ShipSkin;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;

      this.skinType = skinType;
      this.shipType = shipType;
   }

   public static ShipSkin createFromData(ShipSkinData data) {
      if (data == null) {
         return null;
      }

      ShipSkin shipSkin = new ShipSkin(-1, data.itemID, "", "", 100, 1, data.skinType, data.shipType);
      shipSkin.setBasicInfo(data.itemName, data.itemDescription, data.itemIconPath);
      return shipSkin;
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
      ShipSkinData skinData = ShipSkinXMLManager.self.getShipSkinData(itemTypeId);

      if (skinData == null) {
         return itemName;
      }

      return skinData.itemName;
   }

   public override string getDescription () {
      ShipSkinData skinData = ShipSkinXMLManager.self.getShipSkinData(itemTypeId);

      if (skinData == null) {
         return this.itemDescription;
      }

      Ship.Type shipType = skinData.shipType;
      ShipData data = ShipDataManager.self.shipDataList.Find(_ => _.shipType == shipType);

      if (data == null) {
         return this.itemDescription;
      }

      string newDesc = $"Skin for the '{data.shipName}'.";

      if (string.IsNullOrWhiteSpace(this.itemDescription)) {
         return newDesc;
      } else {
         return this.itemDescription + " " + newDesc;
      }
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
