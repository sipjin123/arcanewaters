using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StoreHatDyeBox : StoreDyeBox
{
   #region Public Variables

   // Reference to the Body layer
   public Image bodyImage;

   // Reference to the Hat layer
   public Image hatImage;

   // Reference to the Recolored sprite for the hat
   public RecoloredSprite hatSprite;

   // The hat of the current player
   public Item playerHat;

   #endregion

   public virtual void initialize () {
      this.storeTabCategory = StoreTab.StoreTabType.HatDyes;

      if (this.hatImage == null || this.bodyImage == null || this.hatSprite == null || Util.isBatch() || dye == null) {
         return;
      }

      this.palette = PaletteSwapManager.self.getPalette(dye.paletteId);

      if (this.palette == null) {
         return;
      }

      // Body
      setupBody(Global.player.gender);

      // Hat
      HatStatData hatData = EquipmentXMLManager.self.getHatData(playerHat.itemTypeId);

      if (hatData == null) {
         hatData = EquipmentXMLManager.self.hatStatList.First();
      }

      if (hatData != null) {
         setupHat(Global.player.gender, hatData.hatType, palette.paletteName);
      }
   }

   private bool setupBody (Gender.Type gender) {
      BodyLayer.Type randomBodyType = BodyLayer.getRandomBodyTypeOfGender(gender);
      Sprite bodySprite = ImageManager.getBodySprite(gender, randomBodyType, frame: 8);

      if (bodySprite == null) {
         return false;
      }

      bodyImage.sprite = bodySprite;
      bodyImage.material = new Material(this.bodyImage.material);

      return true;
   }

   private bool setupHat (Gender.Type gender, int hatType, string hatPalette) {
      Sprite hatSprite = ImageManager.getHatSprite(hatType, frame: 8);

      if (hatSprite == null) {
         return false;
      }

      hatImage.sprite = hatSprite;
      hatImage.material = new Material(this.hatImage.material);

      string mergedPalette = Item.parseItmPalette(Item.overridePalette(hatPalette, playerHat.paletteNames));

      recolorHat(mergedPalette);

      return true;
   }

   private void recolorHat (string palette) {
      hatSprite.recolor(palette);
   }

   #region Private Variables

   #endregion
}

