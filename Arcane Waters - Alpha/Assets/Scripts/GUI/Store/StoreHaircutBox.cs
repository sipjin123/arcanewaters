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

   // The metadata
   public StoreHaircutBoxMetadata metadata;

   #endregion

   private bool assignHairSpriteToImage(Image image, string backFront, string gender, string type, string hairColor) {
      Sprite[] sprites = ImageManager.getSprites("Hair/" + gender + "/" + backFront + "/" + type);
      
      if (sprites.Length == 1) {
         return false;
      }

      if (sprites.Length > 8) {
         image.sprite = sprites[8];
      }

      image.material = new Material(this.imageIcon.material);
      image.GetComponent<RecoloredSprite>().recolor(hairColor);
      return true;
   }

   public void initialize () {
      if (this.imageIcon == null || Util.isBatch() || metadata == null) {
         return;
      }

      string hairColor = PaletteDef.Hair.Yellow;

      if (Global.player is BodyEntity) {
         BodyEntity body = (BodyEntity) Global.player;
         hairColor = body.hairPalettes;
      }

      string gender = Global.player != null && Global.player.isMale() ? "Male" : "Female";

      foreach (Image image in getHairImages()) {
         string number = metadata.hairType.ToString().Split('_')[2];
         string backFront = (image == hairBack) ? "Back" : "Front";
         string typeString = gender + "_hair_" + backFront + "_" + number;
         bool assigned = assignHairSpriteToImage(image, backFront, gender, typeString, hairColor);

         if (!assigned) {
            // Try to find the corresponding back hair. If not use the same
            string oppositeBackFront = (image != hairBack) ? "Back" : "Front";
            typeString = gender + "_hair_" + oppositeBackFront + "_" + number;
            assignHairSpriteToImage(image, oppositeBackFront, gender, typeString, hairColor);
         }
      }
   }

   protected List<Image> getHairImages () {
      return new List<Image>() { hairBack, hairFront };
   }

   public bool isFemale () {
      if (metadata == null) {
         return false;
      }

      return metadata.hairType.ToString().ToLower().Contains("female");
   }

   #region Private Variables

   #endregion
}
