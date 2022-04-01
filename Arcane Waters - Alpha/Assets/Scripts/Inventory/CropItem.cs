using UnityEngine;
using System;

[Serializable]
public class CropItem : Item
{
   #region Public Variables

   #endregion

   public CropItem (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1) {
      this.category = Category.Crop;

      this.id = id;
      this.itemTypeId = itemTypeId;
      this.count = count;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;
   }

   public static CropItem createNewItem (int cropDataId, int count) {
      return new CropItem(0, cropDataId, "", "", 100, count);
   }

   public bool tryGetCropType (out Crop.Type cropType) {
      if (!cacheData()) {
         cropType = Crop.Type.None;
         return false;
      }

      cropType = (Crop.Type) _data.cropsType;
      return true;
   }

   public override string getDescription () {
      if (!cacheData()) {
         return base.getDescription();
      }

      return _data.xmlDescription;
   }

   public override string getName () {
      if (!cacheData()) {
         return base.getName();
      }

      return _data.xmlName;
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

      return "Cargo/" + ((Crop.Type) _data.cropsType).ToString();
   }

   private bool cacheData () {
      if (_data != null) {
         return true;
      }

      if (CropsDataManager.self.tryGetCropData(itemTypeId, out _data)) {
         return true;
      }

      return false;
   }

   #region Private Variables

   // Data associated with this item
   private CropsData _data;

   #endregion
}
