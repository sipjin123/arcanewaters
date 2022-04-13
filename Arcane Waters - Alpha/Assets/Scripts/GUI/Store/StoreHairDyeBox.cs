using UnityEngine;
using UnityEngine.UI;

public class StoreHairDyeBox : StoreDyeBox
{
   #region Public Variables

   // The hair layers
   public Image hairBack;
   public Image hairFront;

   // The body layer
   public Image bodyImage;

   #endregion

   public void initialize () {
      this.storeTabCategory = StoreTab.StoreTabType.HairDyes;

      if (this.imageIcon == null || Util.isBatch() || dye == null) {
         return;
      }

      this.palette = PaletteSwapManager.self.getPalette(dye.paletteId);

      if (this.palette == null) {
         return;
      }

      // Setup body
      setupBodyLayer();

      // Front Hair
      setupHairLayer(hairFront, palette.paletteName);

      // Back Hair
      setupHairLayer(hairBack, palette.paletteName);

      setupBanner();
   }

   private bool setupBodyLayer () {
      if (Global.player == null) {
         return false;
      }

      Sprite bodySprite = ImageManager.getBodySprite(Global.player.gender, Global.player.bodyType, frame: 8);

      if (bodySprite == null) {
         return false;
      }

      bodyImage.sprite = bodySprite;
      bodyImage.material = new Material(this.bodyImage.material);

      return true;
   }

   private bool setupHairLayer (Image hairLayer, string hairColorPalette) {
      Gender.Type displayedGender = Global.player == null ? Gender.Type.Male : Global.player.gender;
      HairLayer.Type displayedHairType = Global.player == null ? HairLayer.Type.Male_Hair_2 : Global.player.hairType;
      string displayedHairTypeNumber = HairLayer.computeNumber(displayedHairType);

      bool isFront = hairLayer == hairFront;
      bool assigned = assignHairSpriteToImage(hairLayer, isFront, displayedGender, displayedHairTypeNumber, hairColorPalette);

      if (!assigned) {
         assigned = assignHairSpriteToImage(hairLayer, !isFront, displayedGender, displayedHairTypeNumber, hairColorPalette);
      }

      return assigned;
   }

   private bool assignHairSpriteToImage (Image image, bool front, Gender.Type gender, string number, string hairColorPalette) {
      Sprite sprite = ImageManager.getHairSprite(front, gender, number, frame: 8);

      if (sprite == null) {
         return false;
      }

      image.sprite = sprite;
      image.material = new Material(this.imageIcon.material);
      image.GetComponent<RecoloredSprite>().recolor(hairColorPalette);
      return true;
   }

   #region Private Variables

   #endregion
}

