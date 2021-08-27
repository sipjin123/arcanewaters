using System;

[Serializable]
public class GemItem : Item {
   #region Public Variables

   #endregion

   public GemItem () {

   }

   public GemItem (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1) {
      this.category = Category.Gems;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;
   }

   public override bool canBeUsed () {
      return false;
   }

   public override bool canBeTrashed () {
      return false;
   }

   public override bool canBeEquipped () {
      return false;
   }

   public override bool canBeStacked () {
      return false;
   }

   public override string getIconPath () {
      return "Sprites/GUI/Character Creation/Tab Icons/hair_icon";
   }

   public override string getName () {
      return this.itemName;
   }

   public override string getDescription () {
      return this.itemDescription;
   }

   #region Private Variables

   #endregion
}
