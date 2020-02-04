using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

namespace BackgroundTool
{
   public class SpriteSelectionTemplate : MonoBehaviour, IPointerDownHandler
   {
      #region Public Variables

      // The image of the sprite selection
      public Image imageIcon;

      // Path of the sprite
      public string spritePath;

      #endregion

      public void OnPointerDown (PointerEventData eventData) {
         ImageManipulator.self.createInstance(imageIcon.sprite, spritePath);
      }

      #region Private Variables

      #endregion
   }
}