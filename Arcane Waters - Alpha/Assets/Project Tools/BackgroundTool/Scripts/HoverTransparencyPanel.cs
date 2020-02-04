using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

namespace BackgroundTool
{
   public class HoverTransparencyPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
   {
      #region Public Variables

      // Reference to the canvas group
      public CanvasGroup canvasGroup;

      // Value of the alpha when disabled
      public float disabledValue = .4f;

      #endregion

      private void Start () {
         canvasGroup.alpha = disabledValue;
      }

      public void OnPointerEnter (PointerEventData eventData) {
         canvasGroup.alpha = 1;
      }

      public void OnPointerExit (PointerEventData eventData) {
         canvasGroup.alpha = disabledValue;
      }

      #region Private Variables

      #endregion
   }
}