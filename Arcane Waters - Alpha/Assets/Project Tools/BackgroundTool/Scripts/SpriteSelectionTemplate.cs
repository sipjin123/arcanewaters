﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using static BackgroundTool.ImageLoader;

namespace BackgroundTool
{
   public class SpriteSelectionTemplate : MonoBehaviour
   {
      #region Public Variables

      // The image of the sprite selection
      public SpriteRenderer spriteIcon;

      // Path of the sprite
      public string spritePath;

      // The layer type of this sprite selection
      public BGLayer layerType;

      // Determines the content category of the selection
      public BGContentCategory contentCategory;

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