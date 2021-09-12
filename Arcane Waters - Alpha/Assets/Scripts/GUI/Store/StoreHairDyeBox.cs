using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class StoreHairDyeBox : StoreItemBox
{
   #region Public Variables

   // The hair layers
   public Image hairBack;
   public Image hairFront;

   // The hair dye information
   public HairDyeData hairdye;

   // Palette information
   public PaletteToolData palette;

   #endregion

   public void initialize () {
      if (this.imageIcon == null || Util.isBatch() || hairdye == null) {
         return;
      }

      this.palette = PaletteSwapManager.self.getPalette(hairdye.paletteId);

      if (this.palette == null) {
         return;
      }

      // Front Hair
      setupHairLayer(hairFront, palette.paletteName);

      // Back Hair
      setupHairLayer(hairBack, palette.paletteName);
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
      Sprite sprite = ImageManager.getHairSprite(front, gender, number);

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

