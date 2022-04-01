using UnityEngine;
using System;

[Serializable]
public class Prop : Item
{
   #region Public Variables

   #endregion

   public Prop (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1) {
      this.category = Category.Prop;

      this.id = id;
      this.itemTypeId = itemTypeId;
      this.count = count;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;
   }

   public static Prop createNewItem (int definitionId, int count = 1) {
      return new Prop(0, definitionId, "", "", 100, count);
   }

   public override string getDescription () {
      if (!cacheData()) {
         return base.getDescription();
      }

      return _data.description;
   }

   public override string getName () {
      if (!cacheData()) {
         return base.getName();
      }

      return _data.name;
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      //string palettes = Item.trimItmPalette(paletteNames);
      string palettes = PaletteSwapManager.self.getPalettesDisplayName(paletteNames);

      if (!string.IsNullOrEmpty(palettes)) {
         palettes = " (" + palettes + ")";
      }

      return string.Format("<color={0}>{1}</color>" + palettes + "\n\n{2}",
         "#" + colorHex, getName(), getDescription());
   }

   public override string getIconPath () {
      if (!cacheData()) {
         return base.getIconPath();
      }

      return _data.iconPath;
   }

   private bool cacheData () {
      if (_data != null) {
         return true;
      }

      if (ItemDefinitionManager.self.tryGetDefinition(itemTypeId, out _data)) {
         return true;
      }

      return false;
   }

   #region Private Variables

   // Data associated with this item
   private PropDefinition _data;

   #endregion
}
