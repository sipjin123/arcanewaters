using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

namespace BackgroundTool
{
   public class SelectionPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
   {
      #region Public Variables

      #endregion

      public void OnPointerEnter (PointerEventData eventData) {
         ImageManipulator.self.isHoveringOnSelectionPanel = true;
      }

      public void OnPointerExit (PointerEventData eventData) {
         ImageManipulator.self.isHoveringOnSelectionPanel = false;
      }

      #region Private Variables

      #endregion
   }
}