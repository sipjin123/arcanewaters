using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class Haircut : Item {
   #region Public Variables

   #endregion

   public Haircut () {

   }

   public Haircut (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1) {
      this.category = Category.Haircut;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;
   }

   #if IS_SERVER_BUILD

   public static Haircut create (MySqlDataReader reader) {
      Haircut haircutItem = new Haircut();
      haircutItem.category = Category.Haircut;
      haircutItem.id = DataUtil.getInt(reader, "hcId");
      haircutItem.itemName = DataUtil.getString(reader, "hcName");
      haircutItem.itemDescription = DataUtil.getString(reader, "hcDescription");
      haircutItem.itemTypeId = DataUtil.getInt(reader, "hcType");

      // Gender is handled as metadata
      return haircutItem;
   }

   #endif

   public override bool canBeUsed () {
      return true;
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
