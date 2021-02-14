using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GuildIconSmall : GuildIcon
{
   #region Public Variables

   #endregion

   protected override Sprite getBorderSprite (string borderName) {
      if (borderName == null) {
         return ImageManager.getSprite(BLANK_SPRITE_PATH + "empty_layer");
      }
      return ImageManager.getSprite(BORDER_SMALL_PATH + borderName + "_small");
   }

   protected override Sprite getMaskSprite (string borderName) {
      if (borderName == null) {
         return ImageManager.getSprite(BLANK_SPRITE_PATH + "empty_layer");
      }
      return ImageManager.getSprite(MASK_SMALL_PATH + borderName + "_small_mask");
   }

   protected override Sprite getBackgroundSprite (string backgroundName) {
      if (backgroundName == null) {
         return ImageManager.getSprite(BLANK_SPRITE_PATH + "empty_layer");
      }
      return background.sprite = ImageManager.getSprite(BACKGROUND_SMALL_PATH + backgroundName + "_small");
   }

   protected override Sprite getSigilSprite (string sigilName) {
      if (sigilName == null) {
         return ImageManager.getSprite(BLANK_SPRITE_PATH + "empty_layer");
      }
      return ImageManager.getSprite(SIGIL_SMALL_PATH + sigilName + "_small");
   }

   #region Private Variables

   #endregion
}
