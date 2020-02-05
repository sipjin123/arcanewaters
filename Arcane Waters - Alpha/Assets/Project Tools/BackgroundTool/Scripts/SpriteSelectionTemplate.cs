using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

namespace BackgroundTool
{
   public class SpriteSelectionTemplate : MonoBehaviour
   {
      #region Public Variables

      // The image of the sprite selection
      public SpriteRenderer spriteIcon;

      // Path of the sprite
      public string spritePath;

      #endregion

      public void OnMouseDown () {
         if (!ImageManipulator.self.isDragging) {
            bool withinRectTrans = RectTransformUtility.RectangleContainsScreenPoint(ImageManipulator.self.rectReference, new Vector2(Input.mousePosition.x, Input.mousePosition.y));
            if (withinRectTrans) {
               spriteIcon.color = Color.white;
               List<SpriteSelectionTemplate> spriteSelectionTemp = new List<SpriteSelectionTemplate>();
               spriteSelectionTemp.Add(this);
               ImageManipulator.self.beginDragSelectionGroup(spriteSelectionTemp, true);
            }
         }
      }

      public void OnMouseEnter () {
         bool withinRectTrans = RectTransformUtility.RectangleContainsScreenPoint(ImageManipulator.self.rectReference, new Vector2(Input.mousePosition.x, Input.mousePosition.y));
         if (withinRectTrans) {
            spriteIcon.color = Color.blue;
         } else {
            spriteIcon.color = Color.white;
         }
      }

      public void OnMouseExit () {
         spriteIcon.color = Color.white;
      }


      #region Private Variables

      #endregion
   }
}