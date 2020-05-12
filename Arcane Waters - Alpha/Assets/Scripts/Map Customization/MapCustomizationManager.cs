using System.Collections;
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

      // Currently made customizations to the map
      private static MapCustomizationData customizationData;


      // Current area that is being customized, null if customization is not active currently
      public static string currentArea { get; private set; }
      public static string currentAreaBaseMap { get; private set; }

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

         if (!AreaManager.self.tryGetOwnedMapManager(areaName, out OwnedMapManager ownedMapManager)) {
            D.error("Trying to customize a map that is not an owned map");
            return;
         } else {
            currentArea = areaName;
            currentAreaBaseMap = ownedMapManager.getBaseMapAreaKey(areaName);
         }

         CustomizationUI.show();
         CustomizationUI.setLoading(true);

         self.StartCoroutine(enterCustomizationRoutine());
      }

      private static IEnumerator enterCustomizationRoutine () {
         // Gather prefabs from the scene that can be customized
         CustomizablePrefab[] prefabs = AreaManager.self.getArea(currentArea).GetComponentsInChildren<CustomizablePrefab>();
         _customizablePrefabs = prefabs.ToDictionary(p => p.unappliedChanges.id, p => p);

         // Set all customizable prefabs to semi-transparent
         foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
            prefab.setSemiTransparent(true);
         }

         // Fetch customization data that is saved for this map
         Global.player.rpc.Cmd_RequestMapCustomizationManagerData(Global.player.userId, currentAreaBaseMap);
         yield return new WaitWhile(() => customizationData == null);

         CustomizationUI.setLoading(false);
      }

      public static void exitCustomization () {
         currentArea = null;
         customizationData = null;
         CustomizationUI.hide();

         self.StopAllCoroutines();

         // Reset all customizable prefabs color
         foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
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
            if (!_selectedPrefab.unappliedChanges.isLocalPositionSet()) {
               _selectedPrefab.unappliedChanges.localPosition = _selectedPrefab.anchorLocalPosition + delta;
            } else {
               _selectedPrefab.unappliedChanges.localPosition += delta;
            }

            _selectedPrefab.transform.localPosition = _selectedPrefab.unappliedChanges.localPosition;
            _selectedPrefab.GetComponent<ZSnap>()?.snapZ();
         }
      }

      public static void pointerDown (Vector2 worldPosition) {
         selectPrefab(getPrefabAtPosition(worldPosition));
         updatePrefabHighlights(false);
      }

      public static void pointerUp (Vector2 worldPosition) {
         if (_selectedPrefab != null) {
            if (!_selectedPrefab.areChangesEmpty()) {
               customizationData.add(_selectedPrefab.unappliedChanges);
               Global.player.rpc.Cmd_UpdateMapCustomizationData(customizationData, currentArea, _selectedPrefab.unappliedChanges);

               _selectedPrefab.submitChanges();
            }
         }

         selectPrefab(null);
         updatePrefabHighlights(true);
      }

      private static void updatePrefabHighlights (bool preferSemiTransparent) {
         // Set all except selected prefabs to semi-transparent
         foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
            prefab.setSemiTransparent(prefab != _selectedPrefab && preferSemiTransparent);
         }
      }

      private static CustomizablePrefab getPrefabAtPosition (Vector2 position) {
         CustomizablePrefab result = null;
         float minZ = float.MaxValue;

         // Iterate over all prefabs that overlap given point
         foreach (CustomizablePrefab prefab in _customizablePrefabs.Values.Where(p => p.interactionCollider.OverlapPoint(position))) {
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

      /// <summary>
      /// Called from the server, whenever changes to a prefab are made and saved in the database
      /// </summary>
      /// <param name="areaKey"></param>
      /// <param name="changes"></param>
      public static void receivePrefabChanges (string areaKey, PrefabChanges changes) {
         // If we are customizing this map, let the user-end handle the changes
         if (currentArea != null && currentArea.CompareTo(areaKey) == 0) return;

         // Find the prefab in the area
         CustomizablePrefab prefab = AreaManager.self.getArea(currentArea)?.GetComponentsInChildren<CustomizablePrefab>()
            .FirstOrDefault(p => p.unappliedChanges.id == changes.id);

         if (prefab != null) {
            prefab.unappliedChanges = changes;
            prefab.submitChanges();
         }
      }

      /// <summary>
      /// Called from the server, after the manager initially request all customization data
      /// </summary>
      /// <param name="data"></param>
      public static void receiveMapCustomizationData (MapCustomizationData data) {
         customizationData = data;
      }

      #region Private Variables

      // Prefabs in the scene that can be customized
      private static Dictionary<int, CustomizablePrefab> _customizablePrefabs;

      // Currently selected prefab
      private static CustomizablePrefab _selectedPrefab;

      #endregion
   }
}
