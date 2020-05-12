using System.Collections;
using System.Collections.Generic;
using MapCreationTool;
using UnityEngine;
using System.Linq;
using System;

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
         // Fetch customization data that is saved for this map
         _waitingServerResponse = true;
         Global.player.rpc.Cmd_RequestEnterMapCustomization(Global.player.userId, currentArea, currentAreaBaseMap);
         yield return new WaitWhile(() => _waitingServerResponse);

         // Set customization data
         MapManager.self.setCustomizations(AreaManager.self.getArea(currentArea), customizationData);

         // Gather prefabs from the scene that can be customized
         CustomizablePrefab[] prefabs = AreaManager.self.getArea(currentArea).GetComponentsInChildren<CustomizablePrefab>();
         _customizablePrefabs = prefabs.ToDictionary(p => p.unappliedChanges.id, p => p);

         // Set all customizable prefabs to semi-transparent
         foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
            prefab.setSemiTransparent(true);
         }

         CustomizationUI.setLoading(false);
      }

      public static void exitCustomization () {
         Global.player.rpc.Cmd_ExitMapCustomization(Global.player.userId, currentArea);

         currentArea = null;
         customizationData = null;
         _selectedPrefab = null;
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
               Global.player.rpc.Cmd_AddPrefabCustomization(Global.player.userId, currentArea, currentAreaBaseMap, _selectedPrefab.unappliedChanges);
               customizationData.add(_selectedPrefab.unappliedChanges);
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

      public static void serverAllowedEnterCustomization (MapCustomizationData currentMapCustomizationData) {
         _waitingServerResponse = false;
         customizationData = currentMapCustomizationData;
      }

      public static void serverDeniedEnterCustomization (string message) {
         _waitingServerResponse = false;
         ChatPanel.self.addChatInfo(new ChatInfo(0, message, DateTime.Now, ChatInfo.Type.System));
         exitCustomization();
      }

      public static bool validatePrefabChanges (string areaKey, PrefabChanges newChanges, out string errorMessage) {
         if (UnityEngine.Random.value < 0.1f) {
            errorMessage = "Randomly generated testing error";
            return false;
         }

         errorMessage = null;
         return true;
      }

      public static void serverAddPrefabChangeSuccess (string areaKey, PrefabChanges changes) {
         // If we are customizing this map, the customization process will handle the change
         if (currentArea != null && currentArea.CompareTo(areaKey) == 0) return;

         Area area = AreaManager.self.getArea(currentArea);
         if (area != null) {
            MapManager.self.addCustomizations(area, changes);
         }
      }

      public static void serverAddPrefabChangeFail (PrefabChanges changes, string message) {
         ChatPanel.self.addChatInfo(new ChatInfo(0, "Failed to apply changes: " + message, DateTime.Now, ChatInfo.Type.System));

         // TODO: reset failed changes on user's end
      }

      #region Private Variables

      // Prefabs in the scene that can be customized
      private static Dictionary<int, CustomizablePrefab> _customizablePrefabs = new Dictionary<int, CustomizablePrefab>();

      // Currently selected prefab
      private static CustomizablePrefab _selectedPrefab;

      // Are we waiting for the server to respond
      private static bool _waitingServerResponse;

      #endregion
   }
}
