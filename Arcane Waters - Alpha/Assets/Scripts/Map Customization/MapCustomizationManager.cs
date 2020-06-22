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

      // How many pixels are in one length unit of a tile
      const int PIXELS_PER_TILE = 16;

      // When user creats new prefabs, from where to start their IDS
      const int NEW_PREFAB_ID_START = 100000000;

      // Singleton instance
      public static MapCustomizationManager self;

      // Custom camera for rendering images at runtime
      public Camera utilCam;

      // Current area that is being customized, null if customization is not active currently
      public static Area currentArea { get; private set; }

      // Owner userId of the current area
      public static int areaOwnerId { get; private set; }

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

         if (!AreaManager.self.tryGetCustomMapManager(areaName, out CustomMapManager customMapManager)) {
            D.error("Trying to customize a map that is not an owned map: " + areaName);
            return;
         }

         if (!CustomMapManager.isUserSpecificAreaKey(areaName)) {
            D.error("Trying to customize a map by a key that is not user-specific: " + areaName);
            return;
         }

         currentArea = AreaManager.self.getArea(areaName);
         int userId = CustomMapManager.isUserSpecificAreaKey(areaName) ? CustomMapManager.getUserId(areaName) : Global.player.userId;
         NetEntity entity = EntityManager.self.getEntity(userId);

         // Owner of the target map has to be in the server
         // TODO: remove this constraint
         if (entity == null) {
            D.log("Owner of the map is not currently in the server.");
            return;
         }

         areaOwnerId = CustomMapManager.getUserId(areaName);

         PanelManager.self.pushIfNotShowing(Panel.Type.MapCustomization);
         CustomizationUI.setLoading(true);

         self.StartCoroutine(enterCustomizationRoutine());
      }

      private static IEnumerator enterCustomizationRoutine () {
         // Fetch customization data that is saved for this map
         _waitingServerResponse = true;
         Global.player.rpc.Cmd_RequestEnterMapCustomization(Global.player.userId, currentArea.areaKey);

         // Gather data about prefabs that can be placed by the user and set it in the UI
         updatePlaceablePrefabData();
         CustomizationUI.setPlaceablePrefabData(_placeablePrefabData);

         yield return new WaitWhile(() => _waitingServerResponse);

         // Gather prefabs from the scene that can be customized
         CustomizablePrefab[] prefabs = currentArea.GetComponentsInChildren<CustomizablePrefab>();
         _customizablePrefabs = prefabs.ToDictionary(p => p.customizedState.id, p => p);
         _serverApprovedState = prefabs.ToDictionary(p => p.customizedState.id, p => p.customizedState);

         // Set all customizable prefabs to semi-transparent
         foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
            prefab.setSemiTransparent(true);
         }

         CustomizationUI.setLoading(false);
      }

      public static void exitCustomization () {
         Global.player.rpc.Cmd_ExitMapCustomization(Global.player.userId, currentArea.areaKey);

         currentArea = null;
         _selectedPrefab = null;
         PanelManager.self.popPanel();

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
               _selectedPrefab.unappliedChanges.localPosition = _selectedPrefab.customizedState.localPosition + delta;
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
            if (_selectedPrefab.anyUnappliedState()) {
               Global.player.rpc.Cmd_AddPrefabCustomization(areaOwnerId, currentArea.areaKey, _selectedPrefab.unappliedChanges);
               _selectedPrefab.submitUnappliedChanges();
            }
         }

         selectPrefab(null);
         updatePrefabHighlights(true);
      }

      public static void newPrefabClick (PlaceablePrefabData newPrefabData, Vector3 globalPosition) {
         // Calculate local position for the prefab
         Vector3 localPosition = currentArea.prefabParent.transform.InverseTransformPoint(globalPosition);

         // Calculate the state of the new prefab
         PrefabState state = new PrefabState {
            id = Math.Max(NEW_PREFAB_ID_START, _customizablePrefabs.Keys.Max() + 1),
            created = true,
            localPosition = localPosition,
            serializationId = newPrefabData.serializationId
         };

         CustomizablePrefab newPref = MapManager.self.createPrefab(currentArea, state, false);
         _customizablePrefabs.Add(newPref.unappliedChanges.id, newPref);
         selectPrefab(newPref);
         updatePrefabHighlights(false);
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
      }

      public static void serverAllowedEnterCustomization () {
         _waitingServerResponse = false;
      }

      public static void serverDeniedEnterCustomization (string message) {
         _waitingServerResponse = false;
         ChatPanel.self.addChatInfo(new ChatInfo(0, message, DateTime.Now, ChatInfo.Type.System));
         exitCustomization();
      }

      public static bool validatePrefabChanges (string areaKey, PrefabState changes, out string errorMessage) {
         if (UnityEngine.Random.value < 0.1f) {
            errorMessage = "Randomly generated testing error";
            return false;
         }

         errorMessage = null;
         return true;
      }

      public static void serverAddPrefabChangeSuccess (string areaKey, PrefabState changes) {
         // If we are customizing this map, the customization process will handle the change
         // Just keep track of changes that were approved by the server
         if (currentArea != null && currentArea.areaKey.CompareTo(areaKey) == 0) {
            if (_serverApprovedState.ContainsKey(changes.id)) {
               _serverApprovedState[changes.id] = _serverApprovedState[changes.id].add(changes);
            } else {
               _serverApprovedState.Add(changes.id, changes);
            }
            return;
         }

         // Otherwise, someone else changed the map. Check if we have the map, if so, update it
         Area area = AreaManager.self.getArea(areaKey);
         if (area != null) {
            MapManager.self.addCustomizations(area, changes);
         }
      }

      public static void removeTracked (CustomizablePrefab prefab) {
         if (_customizablePrefabs.ContainsKey(prefab.customizedState.id)) {
            _customizablePrefabs.Remove(prefab.customizedState.id);
         }
      }

      public static void serverAddPrefabChangeFail (PrefabState changes, string message) {
         ChatPanel.self.addChatInfo(new ChatInfo(0, "Failed to apply changes: " + message, DateTime.Now, ChatInfo.Type.System));

         // If player is modifying a prefab that had failed changes, stop modification
         if (_selectedPrefab != null && _selectedPrefab.customizedState.id == changes.id) {
            _selectedPrefab.revertUnappliedChanges();
            selectPrefab(null);
            foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
               prefab.setSemiTransparent(true);
            }
         }

         // Revert to most recent approved change
         if (_customizablePrefabs.TryGetValue(changes.id, out CustomizablePrefab changedPrefab)) {
            // If we were trying to create a new prefab, delete it
            if (changes.created) {
               removeTracked(changedPrefab);
               Destroy(changedPrefab.gameObject);
            } else if (_serverApprovedState.TryGetValue(changes.id, out PrefabState approvedState)) {
               changedPrefab.unappliedChanges = approvedState;
               changedPrefab.submitUnappliedChanges();
            }
         }
      }

      private static void updatePlaceablePrefabData () {
         // Delete old entries
         for (int i = 0; i < _placeablePrefabData.Count; i++) {
            if (_placeablePrefabData[i].displaySprite != null) {
               Destroy(_placeablePrefabData[i].displaySprite.texture);
               Destroy(_placeablePrefabData[i].displaySprite);
            }
         }
         _placeablePrefabData.Clear();

         // Get new entries from asset serialization maps
         foreach (KeyValuePair<int, GameObject> indexPref in AssetSerializationMaps.allBiomes.indexToPrefab) {
            // Only prefabs with CustomizablePrefab component can be placed
            CustomizablePrefab cPref = indexPref.Value.GetComponent<CustomizablePrefab>();
            if (cPref != null) {
               _placeablePrefabData.Add(new PlaceablePrefabData {
                  serializationId = indexPref.Key,
                  prefab = cPref,
                  displaySprite = createPrefabSprite(cPref)
               });
            }
         }
      }

      private static Sprite createPrefabSprite (CustomizablePrefab prefab) {
         Vector2Int pixelSize = prefab.size * PIXELS_PER_TILE;
         Vector3 renderSpot = new Vector3(0, -5000f, 0);

         // Set camera and texture settings
         RenderTexture renTex = new RenderTexture(pixelSize.x, pixelSize.y, 16);
         self.utilCam.targetTexture = renTex;
         self.utilCam.orthographicSize = prefab.size.y * 0.5f * 0.16f;
         self.utilCam.transform.position = renderSpot - Vector3.forward * 10f;

         // Instantiate the target prefab
         CustomizablePrefab prefInstance = Instantiate(prefab, renderSpot, Quaternion.identity);

         // Render the image and turn into texture
         self.utilCam.Render();
         RenderTexture.active = renTex;
         Texture2D result = new Texture2D(renTex.width, renTex.height);
         result.filterMode = FilterMode.Point;
         result.ReadPixels(new Rect(0, 0, renTex.width, renTex.height), 0, 0);
         result.Apply();
         RenderTexture.active = null;

         // Clean up
         prefInstance.gameObject.SetActive(false);
         Destroy(renTex);
         Destroy(prefInstance.gameObject);

         return Sprite.Create(result, new Rect(Vector2.zero, pixelSize), Vector2.one * 0.5f);
      }

      #region Private Variables

      // Prefabs in the scene that can be customized
      private static Dictionary<int, CustomizablePrefab> _customizablePrefabs = new Dictionary<int, CustomizablePrefab>();

      // List of prefabs that can be placed in the scene
      private static List<PlaceablePrefabData> _placeablePrefabData = new List<PlaceablePrefabData>();

      // Currently selected prefab
      private static CustomizablePrefab _selectedPrefab;

      // Are we waiting for the server to respond
      private static bool _waitingServerResponse;

      // State that was approved by a server as valid
      private static Dictionary<int, PrefabState> _serverApprovedState;

      #endregion
   }
}
