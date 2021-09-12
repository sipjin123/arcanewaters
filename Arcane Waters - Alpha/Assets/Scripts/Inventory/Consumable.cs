using System;
using UnityEngine;

[Serializable]
public class Consumable : Item
{
   #region Public Variables

   // Type of consumable
   public enum Type
   {
      // No type
      None = 0,

      // Resets the Perk points
      PerkResetter = 1
   }

   public Consumable.Type consumableType;

   #endregion

   public Consumable () {

   }

   public Consumable (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1, Consumable.Type consumableType = Consumable.Type.None) {
      this.category = Category.Consumable;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;

      this.consumableType = consumableType;
   }

   public static Consumable createFromData (ConsumableData data) {
      if (data == null) {
         return null;
      }

      Consumable consumable = new Consumable(-1, data.itemID, "", "", 100, 1, data.consumableType);
      consumable.setBasicInfo(data.itemName, data.itemDescription, data.itemIconPath);
      return consumable;
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
      return this.itemName;
   }

   public override string getDescription () {
      return this.itemDescription;
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);
      return string.Format("<color={0}>{1}</color>\n\n{2}\n\n",
         "#" + colorHex, getName(), getDescription());
   }

   public override Rarity.Type getRarity () {
      if (consumableType == Type.PerkResetter) {
         return Rarity.Type.Rare;
      }

      return base.getRarity();
   }

   #region Private Variables

   #endregion
}
