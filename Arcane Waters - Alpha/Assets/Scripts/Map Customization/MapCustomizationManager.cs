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

      // Called when selected prefab changes
      public static event Action<CustomizablePrefab> SelectedPrefabChanged;

      // How many pixels are in one length unit of a tile
      const int PIXELS_PER_TILE = 16;

      // When user creats new prefabs, from where to start their IDS
      const int NEW_PREFAB_ID_START = 100000000;

      // How much should distance should be between player and prefab when changing prefab position
      const float MIN_PREFAB_DISTANCE_FROM_PLAYER_CLIENT = 0.24f;
      const float MIN_PREFAB_DISTANCE_FROM_PLAYER_SERVER = 0.12f;

      // Singleton instance
      public static MapCustomizationManager self;

      // Custom camera for rendering images at runtime
      public Camera utilCam;

      // Current area that is being customized, null if customization is not active currently
      public static Area currentArea { get; private set; }

      // Owner userId of the current area
      public static int areaOwnerId { get; private set; }

      [Space(5), Tooltip("Color for outline that is used when prefab isnt selected but is ready for input")]
      public Color prefabReadyColor;

      [Tooltip("Color for outline that is used when user is hovering a prefab")]
      public Color prefabHoveredColor;

      [Tooltip("Color for outline that is used when user is modifying a prefab and the current changes are valid")]
      public Color prefabValidColor;

      [Tooltip("Color for outline that is used when user is modifying a prefab and the current changes are invalid")]
      public Color prefabInvalidColor;

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
         yield return new WaitForEndOfFrame();

         // Gather prefabs from the scene that can be customized
         CustomizablePrefab[] prefabs = currentArea.GetComponentsInChildren<CustomizablePrefab>().Where(cp => !cp.isPermanent).ToArray();
         _customizablePrefabs = prefabs.ToDictionary(p => p.customizedState.id, p => p);
         _serverApprovedState = prefabs.ToDictionary(p => p.customizedState.id, p => p.customizedState);
         yield return new WaitForEndOfFrame();

         yield return new WaitWhile(() => _waitingServerResponse);

         // Enable prefab outlines
         updatePrefabOutlines(null);

         CustomizationUI.setLoading(false);
      }

      public static void exitCustomization () {
         Global.player.rpc.Cmd_ExitMapCustomization(Global.player.userId, currentArea.areaKey);

         currentArea = null;
         _selectedPrefab = null;
         PanelManager.self.popPanel();

         self.StopAllCoroutines();

         // Disable prefab outlines
         foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
            prefab.setOutline(false, false, false, false);
         }
      }

      /// <summary>
      /// Called when pointer position changes and pointer is hovering the screen
      /// </summary>
      /// <param name="worldPosition"></param>
      public static void pointerHover (Vector2 worldPosition) {
         updatePrefabOutlines(worldPosition);
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

            updatePrefabOutlines(null);
         }
      }

      public static void pointerDown (Vector2 worldPosition) {
         selectPrefab(getPrefabAtPosition(worldPosition));
         updatePrefabOutlines(null);
      }

      public static void pointerUp (Vector2 worldPosition, bool deleteHovered) {
         if (_selectedPrefab == null) return;

         // If delete button is hovered, cancel creation or mark as deleted
         if (deleteHovered) {
            if (_selectedPrefab.unappliedChanges.created) {
               _selectedPrefab.revertUnappliedChanges();
            } else {
               _selectedPrefab.unappliedChanges.deleted = true;
               _selectedPrefab.unappliedChanges.clearLocalPosition();
            }
         }

         if (_selectedPrefab.anyUnappliedState()) {
            if (validatePrefabChanges(currentArea, _selectedPrefab.unappliedChanges, false, out string errorMessage)) {
               Global.player.rpc.Cmd_AddPrefabCustomization(areaOwnerId, currentArea.areaKey, _selectedPrefab.unappliedChanges);
               _selectedPrefab.submitUnappliedChanges();
            } else {
               _selectedPrefab.revertUnappliedChanges();
            }

         }

         selectPrefab(null);
         updatePrefabOutlines(worldPosition);
      }

      public static void newPrefabClick (PlaceablePrefabData newPrefabData, Vector3 globalPosition) {
         // Calculate local position for the prefab
         Vector3 localPosition = currentArea.prefabParent.transform.InverseTransformPoint(globalPosition);

         // Calculate the state of the new prefab
         int maxId = _customizablePrefabs.Keys.Count == 0 ? 0 : _customizablePrefabs.Keys.Max();
         PrefabState state = new PrefabState {
            id = Math.Max(NEW_PREFAB_ID_START, maxId + 1),
            created = true,
            localPosition = localPosition,
            serializationId = newPrefabData.serializationId
         };

         CustomizablePrefab newPref = MapManager.self.createPrefab(currentArea, state, false);
         _customizablePrefabs.Add(newPref.unappliedChanges.id, newPref);
         selectPrefab(newPref);
         updatePrefabOutlines(globalPosition);
      }

      private static void updatePrefabOutlines (Vector2? hoveredPosition) {
         CustomizablePrefab hoveredPrefab = hoveredPosition == null ? null : getPrefabAtPosition(hoveredPosition.Value);

         foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
            bool valid = true;
            if (_selectedPrefab != null) {
               valid = validatePrefabChanges(currentArea, _selectedPrefab.unappliedChanges, false, out string errorMessage);
            }

            prefab.setOutline(true, prefab == hoveredPrefab, prefab == _selectedPrefab, valid);
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
         if (_selectedPrefab != null) {
            _selectedPrefab.setGameInteractionsActive(true);
         }

         _selectedPrefab = prefab;

         if (_selectedPrefab != null) {
            _selectedPrefab.setGameInteractionsActive(false);
         }

         SelectedPrefabChanged?.Invoke(_selectedPrefab);
      }

      public static void serverAllowedEnterCustomization () {
         _waitingServerResponse = false;
      }

      public static void serverDeniedEnterCustomization (string message) {
         _waitingServerResponse = false;
         ChatPanel.self.addChatInfo(new ChatInfo(0, message, DateTime.Now, ChatInfo.Type.System));
         exitCustomization();
      }

      public static bool validatePrefabChanges (Area area, PrefabState changes, bool isServer, out string errorMessage) {
         // Check if prefab is not too close to a player
         if (changes.isLocalPositionSet()) {
            int? prefabSerializationId = changes.created
               ? changes.serializationId
               : getPrefab(area, changes.id)?.customizedState.serializationId;

            CustomizablePrefab prefab = AssetSerializationMaps.tryGetPrefabGame(prefabSerializationId ?? -1, area.biome)?.GetComponent<CustomizablePrefab>();

            // Check that the prefab area type matches area's type
            EditorType? type = AreaManager.self.getAreaEditorType(area.baseAreaKey);
            if (type == null || type.Value != prefab.editorType) {
               errorMessage = "Target object does not belong to this type of area";
               return false;
            }

            if (prefab == null) {
               errorMessage = "Could not find target object";
               return false;
            }

            float minDist = isServer ? MIN_PREFAB_DISTANCE_FROM_PLAYER_SERVER : MIN_PREFAB_DISTANCE_FROM_PLAYER_CLIENT;

            bool overlapsAny = EntityManager.self.getAllEntities()
               .Where(e => e.areaKey.Equals(area.areaKey))
               .Any(e => prefab.interactionOverlaps(area.prefabParent.transform.TransformPoint(changes.localPosition), e.sortPoint.transform.position, minDist));

            if (overlapsAny) {
               errorMessage = "Object is too close to a player";
               return false;
            }
         }

         errorMessage = null;
         return true;
      }

      public static CustomizablePrefab getPrefab (Area area, int prefabId) {
         if (area == currentArea) {
            if (_customizablePrefabs.TryGetValue(prefabId, out CustomizablePrefab prefab)) {
               return prefab;
            }
         }

         return area.prefabParent.GetComponentsInChildren<CustomizablePrefab>().FirstOrDefault(cp => cp.customizedState.id == prefabId);
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
            updatePrefabOutlines(null);
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
         } else if (changes.deleted && currentArea != null && _serverApprovedState.TryGetValue(changes.id, out PrefabState approvedState)) {
            CustomizablePrefab previouslyDeletedPrefab = MapManager.self.createPrefab(currentArea, approvedState, true);
            _customizablePrefabs.Add(previouslyDeletedPrefab.customizedState.id, previouslyDeletedPrefab);
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

         // Get the type of the current area
         EditorType? areaType = AreaManager.self.getAreaEditorType(currentArea.baseAreaKey);
         if (areaType == null) {
            D.error($"Area { currentArea.areaKey }-{ currentArea.baseAreaKey } does not have a type stored in AreaManager");
            return;
         }

         // Get new entries from asset serialization maps
         foreach (KeyValuePair<int, GameObject> indexPref in AssetSerializationMaps.allBiomes.indexToPrefab) {
            // Only prefabs with CustomizablePrefab component can be placed
            CustomizablePrefab cPref = indexPref.Value.GetComponent<CustomizablePrefab>();
            if (cPref != null && areaType.Value == cPref.editorType) {
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
