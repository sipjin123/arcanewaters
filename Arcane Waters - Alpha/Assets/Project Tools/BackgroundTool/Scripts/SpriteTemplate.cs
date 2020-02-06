﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

namespace BackgroundTool
{
   public class SpriteTemplate : MonoBehaviour
   {
      #region Public Variables

      // Reference to the sprite outline for highlighting
      public SpriteOutline spriteOutline;

      // Reference to the sprite display
      public SpriteRenderer spriteRender;

      // Data reference
      public SpriteTemplateData spriteTemplateData = new SpriteTemplateData();

      // Determines if the sprite is created from the sprite panel
      public bool createdFromPanel;

      // Determines if this template is highlighted
      public bool isHighlighted;

      #endregion

      private void Start () {
         // TODO: Wait for Sprite Outline to generate
         Invoke("disableHighlightOnStart", .5f);
      }

      private void disableHighlightOnStart () {
         highlightObj(false);
      }

      public void highlightObj (bool isEnabled) {
         spriteOutline.setVisibility(isEnabled);
         isHighlighted = isEnabled;
      }

      public void OnMouseDown () {
         if (isHighlighted && ImageManipulator.self.draggedObjList.Count > 0) {
            ImageManipulator.self.isHoveringHighlight = true;
            return;
         }

         if (!ImageManipulator.self.isDragging && !EventSystem.current.IsPointerOverGameObject() &&
            !ImageManipulator.self.isSpawning) {
            List<SpriteTemplate> spriteTemp = new List<SpriteTemplate>();
            spriteTemp.Add(this);
            ImageManipulator.self.beginDragSpawnedGroup(spriteTemp, true);
         } else {
            if (!ImageManipulator.self.isSpawning || ImageManipulator.self.singleDrag) {
               ImageManipulator.self.endClick();
            }
         }
      }

      public void setTemplate () {
         float zAxisOffset = spriteTemplateData.layerIndex + spriteTemplateData.zAxisOffset * .1f;
         spriteRender.sortingOrder = spriteTemplateData.layerIndex;
         transform.localPosition = new Vector3(spriteTemplateData.localPosition.x, spriteTemplateData.localPosition.y, -zAxisOffset);
      }

      #region Private Variables

      #endregion
   }
}
