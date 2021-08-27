using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class StoreHaircutBox : StoreItemBox {
   #region Public Variables

   // The hair layers
   public Image hairBack;
   public Image hairFront;

   // The haircut item
   public HaircutData haircut;

   #endregion

   public void initialize () {
      if (this.imageIcon == null || Util.isBatch() || haircut == null) {
         return;
      }

      // Set palette
      string hairColor = PaletteDef.Hair.Yellow;

      if (Global.player is BodyEntity) {
         BodyEntity body = (BodyEntity) Global.player;
         hairColor = body.hairPalettes;
      }

      // Front Hair
      setupHairLayer(hairFront, hairColor);

      // Back Hair
      setupHairLayer(hairBack, hairColor);
   }

   private bool setupHairLayer(Image hairLayer, string hairColor) {
      bool isFront = hairLayer == hairFront;
      bool assigned = assignHairSpriteToImage(hairLayer, isFront, haircut.getGender(), haircut.getNumber(), hairColor);

      if (!assigned) {
         assigned = assignHairSpriteToImage(hairLayer, !isFront, haircut.getGender(), haircut.getNumber(), hairColor);
      }

      return assigned;
   }

   private bool assignHairSpriteToImage (Image image, bool front, Gender.Type gender, string number, string hairColor) {
      Sprite sprite = ImageManager.getHairSprite(front, gender, number);

      if (sprite == null) {
         return false;
      }

      image.sprite = sprite;
      image.material = new Material(this.imageIcon.material);
      image.GetComponent<RecoloredSprite>().recolor(hairColor);
      return true;
   }

   #region Private Variables

   #endregion
}
