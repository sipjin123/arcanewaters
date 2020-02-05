using UnityEngine;
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

      // Reference to the sprite display
      public SpriteRenderer spriteRender;

      // Data reference
      public SpriteTemplateData spriteTemplateData = new SpriteTemplateData();

      // Determines if the sprite is created from the sprite panel
      public bool createdFromPanel;

      #endregion

      public void OnMouseDown () {
         if (!ImageManipulator.self.isDragging && !EventSystem.current.IsPointerOverGameObject()) {
            List<SpriteTemplate> spriteTemp = new List<SpriteTemplate>();
            spriteTemp.Add(this);
            ImageManipulator.self.beginDragSpawnedGroup(spriteTemp, true);
         } else {
            ImageManipulator.self.endClick();
         }
      }

      public void setTemplate () {
         transform.localPosition = new Vector3(spriteTemplateData.localPosition.x, spriteTemplateData.localPosition.y, -spriteTemplateData.layerIndex);
         spriteRender.sortingOrder = spriteTemplateData.layerIndex;
      }

      public void OnMouseEnter () {
         if ((!spriteTemplateData.isLocked &&
            !ImageManipulator.self.spriteHighlightObj.activeSelf &&
            ImageManipulator.self.currentEditType == ImageManipulator.EditType.Move && 
            !ImageManipulator.self.isDragging &&
            EventSystem.current.currentSelectedGameObject == null) || createdFromPanel) {
            ImageManipulator.self.beginHoverObj(this);
         } else {
            ImageManipulator.self.stopHoverObj(this);
         }
      }

      public void OnMouseExit () {
         if (!spriteTemplateData.isLocked && ImageManipulator.self.currentEditType == ImageManipulator.EditType.Move && !ImageManipulator.self.isDragging) {
            ImageManipulator.self.stopHoverObj(this);
         }
      }

      #region Private Variables

      #endregion
   }
}
