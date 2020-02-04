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

      #endregion

      public void OnMouseDown () {
         ImageManipulator.self.beginDragObj(this);
      }

      public void OnMouseEnter () {
         ImageManipulator.self.beginHoverObj(this);
      }

      public void OnMouseExit () {
         ImageManipulator.self.stopHoverObj();
      }

      #region Private Variables

      #endregion
   }
}
