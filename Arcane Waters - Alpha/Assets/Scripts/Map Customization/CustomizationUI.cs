using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCustomization;

namespace MapCreationTool
{
   public class CustomizationUI : ClientMonoBehaviour
   {
      #region Public Variables

      // Singleton instance
      public static CustomizationUI self { get; private set; }

      // Is the UI currently enabled and showing to the user
      public static bool isShowing { get; private set; }

      #endregion

      private void OnEnable () {
         self = this;

         _cGroup = GetComponent<CanvasGroup>();

         // Have the UI hidden by default
         hide();
      }

      private void Update () {
         if (!isShowing) return;

         Vector2 pointerPos = Input.mousePosition;
         if (pointerPos != _lastPointerPos) {
            if (Input.GetMouseButton(0)) {
               MapCustomizationManager.pointerDrag(Camera.main.ScreenToWorldPoint(pointerPos) - Camera.main.ScreenToWorldPoint(_lastPointerPos));
            } else {
               MapCustomizationManager.pointerHover(Camera.main.ScreenToWorldPoint(pointerPos));
            }

            _lastPointerPos = pointerPos;
         }

         if (Input.GetMouseButtonDown(0)) {
            MapCustomizationManager.pointerDown(Camera.main.ScreenToWorldPoint(pointerPos));
         }

         if (Input.GetMouseButtonUp(0)) {
            MapCustomizationManager.pointerUp(Camera.main.ScreenToWorldPoint(pointerPos));
         }
      }

      public static void show () {
         isShowing = true;

         self._cGroup.interactable = true;
         self._cGroup.blocksRaycasts = true;
         self._cGroup.alpha = 1f;
      }

      public static void hide () {
         isShowing = false;

         self._cGroup.interactable = false;
         self._cGroup.blocksRaycasts = false;
         self._cGroup.alpha = 0f;
      }

      public void exitCustomization () {
         MapCustomizationManager.exitCustomization();
      }

      #region Private Variables

      // Canvas group that wraps the entire UI
      private CanvasGroup _cGroup;

      // Last pointer position, cached by this component
      private Vector2 _lastPointerPos;

      #endregion
   }
}
