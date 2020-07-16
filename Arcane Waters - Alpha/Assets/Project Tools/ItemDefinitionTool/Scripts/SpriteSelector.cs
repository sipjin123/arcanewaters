using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace ItemDefinitionTool
{
   public class SpriteSelector : MonoBehaviour
   {
      #region Public Variables

      // Image that is displaying the currently selected sprite
      public Image previewImage;

      // Path that is currently selected;
      public string value;

      #endregion

      public void setValueWithoutNotify (string value) {
         this.value = value;

         Sprite sprite = ImageManager.getSprite(value);
         if (sprite == null) {
            sprite = ImageManager.self.blankSprite;
         }
         previewImage.sprite = sprite;
      }

      #region Private Variables

      #endregion
   }
}