using UnityEngine;
using UnityEngine.UI;
using MapCustomization;

namespace MapCreationTool
{
   public class CustomizationUI : Panel
   {
      #region Public Variables

      // Singleton instance
      public static CustomizationUI self { get; private set; }

      // Is the UI showing the loading screen
      public static bool isLoading { get; private set; }

      // Title text of UI
      public Text titleText;

      #endregion

      private void OnEnable () {
         self = this;

         _cGroup = GetComponent<CanvasGroup>();
      }

      public override void Update () {
         if (!isShowing() || isLoading) return;

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

      public static void setLoading (bool loading) {
         isLoading = loading;
         self.titleText.text = loading ? "Loading..." : "Customizing a map";
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
