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

   // The recolor, if any
   public HairLayer.Type hairType;

   #endregion

   public void OnEnable () {
      if (this.imageIcon == null || Util.isBatch()) {
         return;
      }

      ColorType hairColor = ColorType.Yellow;

      if (Global.player is BodyEntity) {
         BodyEntity body = (BodyEntity) Global.player;
         hairColor = body.hairColor1;
      }

      string gender = Global.player != null && Global.player.isMale() ? "Male" : "Female";
      
      foreach (Image image in getHairImages()) {
         string number = hairType.ToString().Split('_')[2];
         string backFront = (image == hairBack) ? "Back" : "Front";
         string typeString = gender + "_hair_" + backFront + "_" + number;
         Sprite[] sprites = ImageManager.getSprites("Hair/" + gender + "/" + backFront + "/" + typeString);
         if (sprites.Length == 0) {
            sprites = ImageManager.getSprites("Empty_Layer");
         }
         image.sprite = sprites[8];
         image.material = new Material(this.imageIcon.material);
         image.GetComponent<RecoloredSprite>().recolor(image.material, hairColor, hairColor);
      }
   }

   protected List<Image> getHairImages () {
      return new List<Image>() { hairBack, hairFront };
   }

   public bool isFemale () {
      return hairType.ToString().ToLower().Contains("female");
   }

   #region Private Variables

   #endregion
}
