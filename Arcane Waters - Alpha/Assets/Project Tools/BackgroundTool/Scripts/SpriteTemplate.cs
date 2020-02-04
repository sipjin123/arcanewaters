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

      public void setTemplate () {
         transform.localPosition = new Vector3(spriteTemplateData.localPosition.x, spriteTemplateData.localPosition.y, -spriteTemplateData.layerIndex);
         transform.localScale = new Vector3(spriteTemplateData.scaleAlteration, spriteTemplateData.scaleAlteration, spriteTemplateData.scaleAlteration);
         transform.localEulerAngles = new Vector3(0, 0, spriteTemplateData.rotationAlteration);
         spriteRender.sortingOrder = spriteTemplateData.layerIndex;
      }

      public void OnMouseDown () {
         if (!EventSystem.current.IsPointerOverGameObject() || createdFromPanel) {
            ImageManipulator.self.beginDragObj(this);
         } 
      }

      public void OnMouseEnter () {
         if (!EventSystem.current.IsPointerOverGameObject() || createdFromPanel) {
            ImageManipulator.self.beginHoverObj(this);
         }
      }

      public void OnMouseExit () {
         ImageManipulator.self.stopHoverObj(this);
      }

      #region Private Variables

      #endregion
   }
}
