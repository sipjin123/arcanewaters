using System.Collections.Generic;
using MapCreationTool;
using UnityEngine;
using System.Linq;

namespace MapCustomization
{
   public class MapCustomizationManager : ClientMonoBehaviour
   {
      #region Public Variables

      // Singleton instance
      public static MapCustomizationManager self;

      /// <summary>
      /// Current area that is being customized, null if customization is not active currently
      /// </summary>
      public static string currentArea { get; private set; }

      #endregion

      private void OnEnable () {
         self = this;
      }

      public static bool isCustomizing => currentArea != null;

      public static void enterCustomization (string areaName) {
         if (currentArea != null) {
            D.warning("Trying to open map customization even though it is currently active");
            return;
         }

         // Gather prefabs from the scene that can be customized
         _customizablePrefabs = new HashSet<CustomizablePrefab>(AreaManager.self.getArea(areaName).GetComponentsInChildren<CustomizablePrefab>());

         // Set all customizable prefabs to semi-transparent
         foreach (CustomizablePrefab prefab in _customizablePrefabs) {
            prefab.setSemiTransparent(true);
         }

         CustomizationUI.show();

         currentArea = areaName;
      }

      public static void exitCustomization () {
         currentArea = null;
         CustomizationUI.hide();

         // Reset all customizable prefabs color
         foreach (CustomizablePrefab prefab in _customizablePrefabs) {
            prefab.setSemiTransparent(false);
         }
      }

      /// <summary>
      /// Called when pointer position changes and pointer is hovering the screen
      /// </summary>
      /// <param name="worldPosition"></param>
      public static void pointerHover (Vector2 worldPosition) {
         updatePrefabHighlights(true);

         // Set hovered prefab to fully visible
         getPrefabAtPosition(worldPosition)?.setSemiTransparent(false);
      }

      /// <summary>
      /// Called when pointer position changes and pointer is dragging
      /// </summary>
      /// <param name="worldPosition"></param>
      public static void pointerDrag (Vector2 delta) {
         if (_selectedPrefab != null) {
            Vector2 originalPos = (Vector2) _selectedPrefab.transform.position - _selectedPrefab.unappliedChanges.translation;
            _selectedPrefab.unappliedChanges.translation += delta;
            _selectedPrefab.transform.position = originalPos + _selectedPrefab.unappliedChanges.translation;
            _selectedPrefab.GetComponent<ZSnap>()?.snapZ();
         }
      }

      public static void pointerDown (Vector2 worldPosition) {
         selectPrefab(getPrefabAtPosition(worldPosition));
         updatePrefabHighlights(false);
      }

      public static void pointerUp (Vector2 worldPosition) {
         if (_selectedPrefab != null) {
            _selectedPrefab.submitChanges();
         }

         selectPrefab(null);
         updatePrefabHighlights(true);
      }

      private static void updatePrefabHighlights (bool preferSemiTransparent) {
         // Set all except selected prefabs to semi-transparent
         foreach (CustomizablePrefab prefab in _customizablePrefabs) {
            prefab.setSemiTransparent(prefab != _selectedPrefab && preferSemiTransparent);
         }
      }

      private static CustomizablePrefab getPrefabAtPosition (Vector2 position) {
         CustomizablePrefab result = null;
         float minZ = float.MaxValue;

         // Iterate over all prefabs that overlap given point
         foreach (CustomizablePrefab prefab in _customizablePrefabs.Where(p => p.interactionCollider.OverlapPoint(position))) {
            if (prefab.transform.position.z < minZ) {
               minZ = prefab.transform.position.z;
               result = prefab;
            }
         }

         return result;
      }

      private static void selectPrefab (CustomizablePrefab prefab) {
         _selectedPrefab = prefab;

         if (_selectedPrefab != null) {
            _selectedPrefab.clearChanges();
         }
      }

      #region Private Variables

      // Prefabs in the scene that can be customized
      private static HashSet<CustomizablePrefab> _customizablePrefabs;

      // Currently selected prefab
      private static CustomizablePrefab _selectedPrefab;

      #endregion
   }
}
