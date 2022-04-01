using System.Collections;
using System.Collections.Generic;
using MapCreationTool;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Tilemaps;

namespace MapCustomization
{
   public class MapCustomizationManager : ClientMonoBehaviour
   {
      #region Public Variables
      // When user creats new prefabs, from where to start their IDS
      const int NEW_PREFAB_ID_START = 100000000;

      // How much should distance should be between player and prefab when changing prefab position
      const float MIN_PREFAB_DISTANCE_FROM_PLAYER_CLIENT = 0.24f;
      const float MIN_PREFAB_DISTANCE_FROM_PLAYER_SERVER = 0.12f;

      // Singleton instance
      public static MapCustomizationManager self;

      // A reference to the Customization UI
      public CustomizationUI customizationUI;

      // Is local player in customization mode right now
      public static bool isLocalPlayerCustomizing { get; private set; }

      // Current area that is being customized, null if customization is not active currently
      public static Area currentArea { get; private set; }

      // Biome of the current instance that is being customized
      public static Biome.Type currentBiome { get; private set; }

      // Owner userId of the current area
      public static int areaOwnerId { get; private set; }

      // The guild id of the guild that owns the current area, if it's a guild area
      public static int areaGuildId { get; private set; }

      // Remaining props for the user to place
      public static List<Item> remainingProps { get; private set; }

      [Space(5), Tooltip("Color for outline that is used when prefab isnt selected but is ready for input")]
      public Color prefabReadyColor;

      [Tooltip("Color for outline that is used when user is hovering a prefab")]
      public Color prefabHoveredColor;

      [Tooltip("Color for outline that is used when user is modifying a prefab and the current changes are valid")]
      public Color prefabValidColor;

      [Tooltip("Color for outline that is used when user is modifying a prefab and the current changes are invalid")]
      public Color prefabInvalidColor;

      // Reference to the selection arrows
      public GameObject selectionArrows;

      // Keep track of sound state in update loop
      public static bool soundHasBeenPlayed = false;

      // Keep track of scroll wheel direction
      public static bool scrollWheelUp;

      // Store the position of the prefab when dragging first starts
      public static Vector2 dragStartPosition;

      // Flag to keep track of when the dragging of a prefab is starting
      public static bool isBeginningOfDrag = true;

      #endregion

      private void OnEnable () {
         self = this;
         _propIconCamera = GetComponentInChildren<PropIconCamera>();
         self.selectionArrows.SetActive(false);
      }

      public static bool isCustomizing => currentArea != null;

      private void Update () {
         if (Global.player == null) return;

         // Make sure that the user can only edit on a map where they have permissions
         bool hasAreaPermissions = (CustomMapManager.isUserSpecificAreaKey(Global.player.areaKey) && CustomMapManager.getUserId(Global.player.areaKey) == Global.player.userId) || 
         (CustomMapManager.isGuildSpecificAreaKey(Global.player.areaKey) && CustomMapManager.getGuildId(Global.player.areaKey) == Global.player.guildId) || 
         (CustomMapManager.isGuildHouseAreaKey(Global.player.areaKey) && CustomMapManager.getGuildId(Global.player.areaKey) == Global.player.guildId);

         // Check if player should be customizing
         bool shouldBeCustomizing = AreaManager.self.tryGetCustomMapManager(Global.player.areaKey, out CustomMapManager cmm) && // Check that this is a customizable area
            AreaManager.self.getArea(Global.player.areaKey) != null && // Check that the area is already created
            (currentArea == null || currentArea.areaKey == Global.player.areaKey) && // Check that the area hasn't changed to a different customizable area
            (Global.player as PlayerBodyEntity).weaponManager.actionType == Weapon.ActionType.CustomizeMap && // Check that customization action weapon is equipped
            (Global.player as PlayerBodyEntity).weaponManager.weaponType > 0 &&
            hasAreaPermissions; 

         if (!isCustomizing && shouldBeCustomizing) {
            enterCustomization(Global.player.areaKey);
         } else if (isCustomizing && !shouldBeCustomizing) {
            exitCustomization();
         }
      }

      public static void enterCustomization (string areaName) {
         if (currentArea != null) {
            D.warning("Trying to open map customization even though it is currently active");
            return;
         }

         if (!AreaManager.self.tryGetCustomMapManager(areaName, out CustomMapManager customMapManager)) {
            D.error("Trying to customize a map that is not an owned map: " + areaName);
            return;
         }

         if (!CustomMapManager.isUserSpecificAreaKey(areaName) && !CustomMapManager.isGuildSpecificAreaKey(areaName) && !CustomMapManager.isGuildHouseAreaKey(areaName)) {
            D.error("Trying to customize a map by a key that is not user-specific and is not a guild map: " + areaName);
            return;
         }

         currentArea = AreaManager.self.getArea(areaName);
         int userId = CustomMapManager.isUserSpecificAreaKey(areaName) ? CustomMapManager.getUserId(areaName) : Global.player.userId;
         NetEntity entity = EntityManager.self.getEntity(userId);
         isLocalPlayerCustomizing = true;

         // Owner of the target map has to be in the server
         // TODO: remove this constraint
         if (entity == null) {
            D.log("Owner of the map is not currently in the server.");
            return;
         }

         currentBiome = entity.getInstance().biome;
         areaOwnerId = CustomMapManager.getUserId(areaName);
         areaGuildId = CustomMapManager.getGuildId(areaName);

         // Make sure the customization UI is active
         self.customizationUI.gameObject.SetActive(true);

         CustomizationUI.ensureShowing();
         CustomizationUI.setLoading(true);

         self.StartCoroutine(enterCustomizationRoutine());
      }

      private static IEnumerator enterCustomizationRoutine () {
         // Fetch customization data that is saved for this map
         _waitingServerResponse = true;
         Global.player.rpc.Cmd_RequestEnterMapCustomization(currentArea.areaKey);

         // Gather data about prefabs that can be placed by the user
         updatePlaceablePrefabData();
         yield return new WaitForEndOfFrame();

         // Gather prefabs from the scene that can be customized
         CustomizablePrefab[] prefabs = currentArea.GetComponentsInChildren<CustomizablePrefab>().Where(cp => !cp.isPermanent).ToArray();
         _customizablePrefabs = prefabs.ToDictionary(p => p.customizedState.id, p => p);
         _serverApprovedState = prefabs.ToDictionary(p => p.customizedState.id, p => p.customizedState);
         yield return new WaitForEndOfFrame();

         yield return new WaitWhile(() => _waitingServerResponse);

         CustomizationUI.setPlaceablePrefabData(_placeablePrefabData);

         // Enable prefab outlines
         updatePrefabOutlines(null);

         CustomizationUI.setLoading(false);
      }

      public static void exitCustomization () {
         Global.player.rpc.Cmd_ExitMapCustomization(currentArea.areaKey);

         isLocalPlayerCustomizing = false;
         currentArea = null;
         currentBiome = Biome.Type.None;
         selectPrefab(null);
         hideSelectionArrows();
         CustomizationUI.ensureHidden();

         self.StopAllCoroutines();

         // Disable prefab outlines
         foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
            prefab.setOutline(false, false, false, false);
         }
      }

      public static void keyDeleteAt (Vector2 worldPos) {
         CustomizablePrefab hoveredPrefab = getPrefabAtPosition(worldPos);

         if (hoveredPrefab == null) return;

         if (hoveredPrefab.unappliedChanges.created) {
            hoveredPrefab.revertUnappliedChanges();
         } else {
            hoveredPrefab.unappliedChanges.deleted = true;
            hoveredPrefab.unappliedChanges.clearLocalPosition();
         }

         if (hoveredPrefab.anyUnappliedState()) {
            if (validatePrefabChanges(currentArea, currentBiome, remainingProps, hoveredPrefab.unappliedChanges, false, out string errorMessage)) {
               Global.player.rpc.Cmd_AddPrefabCustomization(areaOwnerId, currentArea.areaKey, hoveredPrefab.unappliedChanges, areaGuildId);

               // Increase remaining prop item that corresponds to this prefab if it was not placed in editor
               if (!hoveredPrefab.mapEditorState.created) {
                  incrementPropCount(hoveredPrefab.propDefinitionId);
               }
               hoveredPrefab.submitUnappliedChanges();
            } else {
               hoveredPrefab.revertUnappliedChanges();
            }
         }

         _newPrefab = null;
         selectPrefab(null);
         updatePrefabOutlines(null);
      }

      public static void pointerEnter (Vector2 worldPosition) {
         // If we have a prefab to place, show it in the map
         if (CustomizationUI.getSelectedPrefabData() != null) {
            updateToBePlacedPrefab(worldPosition, CustomizationUI.getSelectedPrefabData().Value.serializationId);
            updatePrefabOutlines(null);
         }
      }

      public static void pointerExit (Vector2 worldPosition) {
         // Destroy prefab that is being placed
         if (_newPrefab != null) {
            _newPrefab.revertUnappliedChanges();
            _newPrefab = null;
         }
      }

      /// <summary>
      /// Called when pointer position changes and pointer is hovering the screen
      /// </summary>
      /// <param name="worldPosition"></param>
      public static void pointerHover (Vector2 worldPosition) {
         // Check if we have a prefab that can be placed right now
         if (CustomizationUI.getSelectedPrefabData() != null) {
            updateToBePlacedPrefab(worldPosition, CustomizationUI.getSelectedPrefabData().Value.serializationId);
         } else if (_newPrefab != null) {
            _newPrefab.revertUnappliedChanges();
            _newPrefab = null;
         }
         updatePrefabOutlines(worldPosition);
      }

      /// <summary>
      /// Called when pointer position changes and pointer is dragging
      /// </summary>
      /// <param name="worldPosition"></param>
      public static void pointerDrag (Vector2 delta) {
         // Save starting position of drag operation
         if (_selectedPrefab != null && _draggedPrefab != null) {
            if (isBeginningOfDrag) {
               dragStartPosition = _selectedPrefab.unappliedChanges.localPosition;
               isBeginningOfDrag = false;
            }

            if (!_selectedPrefab.unappliedChanges.isLocalPositionSet()) {
               _selectedPrefab.unappliedChanges.localPosition = _selectedPrefab.customizedState.localPosition + delta;
            } else {
               _selectedPrefab.unappliedChanges.localPosition += delta;
            }

            _selectedPrefab.transform.localPosition = _selectedPrefab.unappliedChanges.localPosition;
            _selectedPrefab.GetComponent<ZSnap>()?.snapZ();

            // Move selection arrows with prefab
            self.selectionArrows.transform.position = _selectedPrefab.transform.position;
            self.selectionArrows.SetActive(true);
            updatePrefabOutlines(null);
         }
      }

      // Right click will cancel the process of placing a new prefab
      public static void rightClick () {
         if (_selectedPrefab != null) {
            _selectedPrefab.revertUnappliedChanges();
         }

         selectPrefab(null);

         if (_newPrefab != null) {
            _newPrefab.revertUnappliedChanges();
            _newPrefab = null;
         }

         hideSelectionArrows();
      }

      public static void pointerDown (Vector2 worldPosition) {
         if (CustomizationUI.getSelectedPrefabData() != null) {
            updateToBePlacedPrefab(worldPosition, CustomizationUI.getSelectedPrefabData().Value.serializationId);
            _customizablePrefabs.Add(_newPrefab.unappliedChanges.id, _newPrefab);
            if (validatePrefabChanges(currentArea, currentBiome, remainingProps, _newPrefab.unappliedChanges, false, out string errorMessage)) {
               _newPrefab.setGameInteractionsActive(true);
               Global.player.rpc.Cmd_AddPrefabCustomization(areaOwnerId, currentArea.areaKey, _newPrefab.unappliedChanges, areaGuildId);
               SoundEffectManager.self.playFmodSfx(SoundEffectManager.PLACE_EDITABLE_OBJECT, position: worldPosition);
               selectPrefab(_newPrefab);

               // Decrease remaining prop item that corresponds to this prefab
               foreach (Item item in remainingProps) {
                  if (item.itemTypeId == _newPrefab.propDefinitionId) {
                     item.count--;
                     CustomizationUI.updatePropCount(item);
                     if (item.count <= 0) {
                        CustomizationUI.selectEntry(null);
                     }
                     break;
                  }
               }

               _newPrefab.submitUnappliedChanges();
            } else {
               _newPrefab.revertUnappliedChanges();
            }

            // Turn on colliders for the prefab that was just placed
            if (_newPrefab.GetComponent<Collider2D>() != null) {
               _newPrefab.GetComponent<Collider2D>().enabled = true;
            } else if (_newPrefab.GetComponentsInChildren<Collider2D>() != null) {
               foreach (Collider2D col in _newPrefab.GetComponentsInChildren<Collider2D>()) {
                  col.enabled = true;
               }
            }

            _newPrefab = null;
         } else {
            // If something is hovered, select it and procceed to change it's position
            CustomizablePrefab hoveredPrefab = getPrefabAtPosition(worldPosition);
            if (hoveredPrefab != null) {
               selectPrefab(hoveredPrefab);
               _draggedPrefab = hoveredPrefab;
               updatePrefabOutlines(worldPosition);
               SoundEffectManager.self.playFmodSfx(SoundEffectManager.PICKUP_EDITABLE_OBJECT, position: worldPosition);
               //SoundEffectManager.self.playSoundEffect(SoundEffectManager.PICKUP_EDIT_OBJ, SoundEffectManager.self.transform);
            } else {
               selectPrefab(null);
            }
         }

         updatePrefabOutlines(worldPosition);
      }

      public static void pointerUp (Vector2 worldPosition) {
         // If dragging a prefab before mouse pointer up, validate the prefab now that the dragging is over
         if (_draggedPrefab != null) {
            // Do not allow placing of a prefab in an invalid location
            if (!validatePrefabChanges(currentArea, currentBiome, remainingProps, _draggedPrefab.unappliedChanges, false, out string errorMessage)) {
               selectPrefab(_draggedPrefab);
               _selectedPrefab.unappliedChanges.localPosition = dragStartPosition;
               _selectedPrefab.submitUnappliedChanges();
            } else {
               Global.player.rpc.Cmd_AddPrefabCustomization(areaOwnerId, currentArea.areaKey, _draggedPrefab.unappliedChanges, areaGuildId);
               _draggedPrefab.submitUnappliedChanges();
            }
            _draggedPrefab = null;
            isBeginningOfDrag = true;
         }

         updatePrefabOutlines(worldPosition);
      }

      private static void updateToBePlacedPrefab (Vector3 worldPosition, int serializationId) {
         // If the prefab has changed, destroy the current prefab before creating a new one
         if ((_newPrefab != null) && (_newPrefab.unappliedChanges.serializationId != serializationId)) {
            _newPrefab.revertUnappliedChanges();
            _newPrefab = null;
            Destroy(_newPrefab);
            _newPrefab = MapManager.self.createPrefab(currentArea, currentBiome, newPrefabState(worldPosition, serializationId), false);
            _newPrefab.setGameInteractionsActive(false);
            soundHasBeenPlayed = false;
         }

         // If the prefab is missing, create a new one and initialize it
         if (_newPrefab == null) {
            _newPrefab = MapManager.self.createPrefab(currentArea, currentBiome, newPrefabState(worldPosition, serializationId), false);
            _newPrefab.name += " (being placed...)";
            _newPrefab.setGameInteractionsActive(false);
            soundHasBeenPlayed = false;
         }

         _newPrefab.unappliedChanges.localPosition = currentArea.prefabParent.transform.InverseTransformPoint(worldPosition);
         _newPrefab.transform.localPosition = _newPrefab.unappliedChanges.localPosition;
         _newPrefab.GetComponent<ZSnap>()?.snapZ();

         if (!soundHasBeenPlayed) {
            SoundEffectManager.self.playFmodSfx(SoundEffectManager.PLACE_EDITABLE_OBJECT, position: worldPosition);
            soundHasBeenPlayed = true;
         }
      }

      private static PrefabState newPrefabState (Vector3 worldPosition, int serializationId) {
         // Calculate the state of the new prefab
         int maxId = _customizablePrefabs.Keys.Count == 0 ? 0 : _customizablePrefabs.Keys.Max();
         return new PrefabState {
            id = Math.Max(NEW_PREFAB_ID_START, maxId + 1),
            created = true,
            localPosition = currentArea.prefabParent.transform.InverseTransformPoint(worldPosition),
            serializationId = serializationId
         };
      }

      private static void updatePrefabOutlines (Vector2? hoveredPosition) {
         CustomizablePrefab hoveredPrefab = hoveredPosition == null ? null : getPrefabAtPosition(hoveredPosition.Value);

         foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
            prefab.setOutline(true, prefab == hoveredPrefab, false, false);
         }

         if (_newPrefab != null) {
            bool valid = validatePrefabChanges(currentArea, currentBiome, remainingProps, _newPrefab.unappliedChanges, false, out string errorMessage);
            _newPrefab.setOutline(true, true, true, valid);

            showSelectionArrows();
            self.selectionArrows.transform.position = _newPrefab.transform.position;
            SelectionSpriteBuildMode.self.setDistances(_newPrefab);
            SelectionSpriteBuildMode.self.setColors(CustomizablePrefab.getIndicatorColor(true, true, true, valid));

            return;
         }

         if (_selectedPrefab != null) {
            bool valid = validatePrefabChanges(currentArea, currentBiome, remainingProps, _selectedPrefab.unappliedChanges, false, out string errorMessage);
            _selectedPrefab.setOutline(true, _selectedPrefab == hoveredPrefab, true, valid);

            showSelectionArrows();
            self.selectionArrows.transform.position = _selectedPrefab.transform.position;
            SelectionSpriteBuildMode.self.setDistances(_selectedPrefab);
            SelectionSpriteBuildMode.self.setColors(CustomizablePrefab.getIndicatorColor(true, true, _selectedPrefab == _draggedPrefab, valid));

            return;
         }

         if (hoveredPrefab != null) {
            showSelectionArrows();
            self.selectionArrows.transform.position = hoveredPrefab.transform.position;
            SelectionSpriteBuildMode.self.setDistances(hoveredPrefab);
            SelectionSpriteBuildMode.self.setColors(CustomizablePrefab.getIndicatorColor(true, true, false, false));

            return;
         }

         hideSelectionArrows();
      }

      private static CustomizablePrefab getPrefabAtPosition (Vector2 position) {
         CustomizablePrefab result = null;
         float minZ = float.MaxValue;

         // Iterate over all prefabs that overlap given point
         foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
            bool enabled = prefab.interactionCollider.enabled;
            prefab.interactionCollider.enabled = true;
            if (prefab.interactionCollider.OverlapPoint(position)) {
               if (prefab.transform.position.z < minZ) {
                  minZ = prefab.transform.position.z;
                  result = prefab;
               }
            }

            prefab.interactionCollider.enabled = enabled;

         }

         return result;
      }

      public static void selectPrefab (CustomizablePrefab prefab) {
         if (_selectedPrefab != null) {
            _selectedPrefab.setGameInteractionsActive(true);
            _selectedPrefab.setOutline(true, false, false, false);
         }

         _selectedPrefab = prefab;

         if (_selectedPrefab != null) {
            _selectedPrefab.setGameInteractionsActive(false);
            CustomizationUI.selectEntry(null);
            self.selectionArrows.transform.position = _selectedPrefab.transform.position;
            SelectionSpriteBuildMode.self.setDistances(_selectedPrefab);
            showSelectionArrows();
         }
      }

      public static void showSelectionArrows () {
         self.selectionArrows.SetActive(true);
      }

      public static void hideSelectionArrows () {
         self.selectionArrows.SetActive(false);
      }

      public static void serverAllowedEnterCustomization (Item[] remainingProps) {
         MapCustomizationManager.remainingProps = remainingProps.ToList();
         _waitingServerResponse = false;
      }

      public static void serverDeniedEnterCustomization (string message) {
         _waitingServerResponse = false;
         ChatPanel.self.addChatInfo(new ChatInfo(0, message, DateTime.Now, ChatInfo.Type.System));
         exitCustomization();
      }

      public static bool validatePrefabChanges (Area area, Biome.Type biome, List<Item> remainingItems, PrefabState changes, bool isServer, out string errorMessage) {
         // If we are host, and this is was validated on the 'client' side, lets just assume it's valid
         if (Util.isHost() && isServer) {
            errorMessage = null;
            return true;
         }

         if (changes.isLocalPositionSet()) {
            CustomizablePrefab prefab = AssetSerializationMaps.tryGetPrefabGame(changes.serializationId, biome)?.GetComponent<CustomizablePrefab>();

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

            // Check if prefab is not too close to a player
            float minDist = isServer ? MIN_PREFAB_DISTANCE_FROM_PLAYER_SERVER : MIN_PREFAB_DISTANCE_FROM_PLAYER_CLIENT;

            bool overlapsAny = false;
            foreach (NetEntity entity in EntityManager.self.getAllEntities()) {
               if (entity == null) {
                  D.error("Entity in entity manager is null!");
                  continue;
               }
               if (entity.areaKey == area.areaKey) {
                  Vector2 prefWorldPos = area.prefabParent.transform.TransformPoint(changes.localPosition);
                  if (prefab.interactionOverlaps(prefWorldPos, entity.sortPoint.transform.position, minDist)) {
                     overlapsAny = true;
                     break;
                  }
               }
            }

            if (overlapsAny) {
               errorMessage = "Object is too close to a player";
               return false;
            }

            // Check that the player has enough of prop remaining
            if (changes.created) {
               if (amountOfPropLeft(remainingItems, prefab) <= 0) {
                  errorMessage = "Not enough of items for this object";
                  return false;
               }
            }

            if (!changes.deleted) {
               // Get world position of prefab
               Vector3 worldPos = area.prefabParent.transform.TransformPoint(changes.localPosition);

               // Check if prefab is over illegal tiles (ex. ceiling)
               // Note: in these calculations bellow, we have to factor in that a tile's width is 0.16 units
               foreach (Tilemap tilemap in area.getTilemapLayers().Where(l => l.coversAllObjects()).Select(l => l.tilemap)) {
                  // Check multiple points along theprefab width
                  for (float offsetX = -prefab.size.x * 0.5f * 0.16f; offsetX <= prefab.size.x * 0.5f * 0.16f; offsetX += 0.16f * 0.5f) {
                     // We care about top of the prefab not being covered up, bottom is fine in most cases
                     Vector3 positionToCheck = worldPos + Vector3.right * offsetX + Vector3.up * (prefab.size.y - 0.25f) * 0.5f * 0.16f;

                     if (tilemap.GetTile(tilemap.WorldToCell(positionToCheck)) != null) {
                        errorMessage = "Object cannot be placed here.";
                        return false;
                     }
                  }
               }

               // Check that prefab is within the bounds of the map
               // We are only checking the center of it to allow some of it to be sticking out
               Vector2 areaHalfSize = area.getAreaHalfSizeWorld();
               if (changes.localPosition.x < -areaHalfSize.x || changes.localPosition.x > areaHalfSize.x ||
                     changes.localPosition.y < -areaHalfSize.y || changes.localPosition.y > areaHalfSize.y) {
                  errorMessage = "Object is outside of the bounds of the map.";
                  return false;
               }

               // Check if prefab requires space
               SpaceRequirer req = prefab.GetComponentInChildren<SpaceRequirer>();
               if (req != null && !req.wouldHaveSpace(worldPos)) {
                  errorMessage = "No Space.";
                  //FloatingCanvas.instantiateAt(worldPos).asCustomMessage("No Space.");
                  return false;
               }
            }
         }

         errorMessage = null;
         return true;
      }

      public static int amountOfPropLeft (List<Item> items, int propDefinitionId) {
         if (items == null) return 0;

         foreach (Item item in items) {
            if (item.itemTypeId == propDefinitionId) {
               return item.count;
            }
         }

         return 0;
      }

      public static int amountOfPropLeft (List<Item> items, CustomizablePrefab propPrefab) {
         return amountOfPropLeft(items, propPrefab.propDefinitionId);
      }

      private static void incrementPropCount (int itemDefinitionId) {
         bool found = false;
         foreach (Item item in remainingProps) {
            if (item.itemTypeId == itemDefinitionId) {
               item.count++;
               CustomizationUI.updatePropCount(item);
               found = true;
               break;
            }
         }

         if (!found) {
            Item item = new Item(-1, Item.Category.Prop, itemDefinitionId, -1, "", "", 100);
            remainingProps.Add(item);
            CustomizationUI.updatePropCount(item);
         }
      }

      public static void serverAddPrefabChangeSuccess (string areaKey, Biome.Type biome, PrefabState changes) {
         // If we are customizing this map, the customization process will handle the change
         // Just keep track of changes that were approved by the server
         if (currentArea != null && currentArea.areaKey.CompareTo(areaKey) == 0) {
            if (changes.created) {
               TutorialManager3.self.tryCompletingStep(TutorialTrigger.PlaceObject);
            } else if (changes.deleted) {
               TutorialManager3.self.tryCompletingStep(TutorialTrigger.DeleteObject);
            } else {
               TutorialManager3.self.tryCompletingStep(TutorialTrigger.MoveObject);
            }

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
            MapManager.self.addCustomizations(area, biome, changes);
         } else {
            D.debug("Missing area! " + area);
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
               // Readd this prefab to remaining prop items
               foreach (Item item in remainingProps) {
                  if (item.itemTypeId == changedPrefab.propDefinitionId) {
                     item.count++;
                  }
               }

               removeTracked(changedPrefab);
               Destroy(changedPrefab.gameObject);
            } else if (_serverApprovedState.TryGetValue(changes.id, out PrefabState approvedState)) {
               if (approvedState.serializationId != changedPrefab.unappliedChanges.serializationId) {
                  Debug.LogWarning("Different serialization id for prefab state " + approvedState);
               } else {
                  changedPrefab.unappliedChanges = approvedState;
                  changedPrefab.submitUnappliedChanges();
               }
            }
         } else if (changes.deleted && currentArea != null && _serverApprovedState.TryGetValue(changes.id, out PrefabState approvedState)) {
            CustomizablePrefab previouslyDeletedPrefab = MapManager.self.createPrefab(currentArea, currentBiome, approvedState, true);
            _customizablePrefabs.Add(previouslyDeletedPrefab.customizedState.id, previouslyDeletedPrefab);
         }
      }

      private static void updatePlaceablePrefabData () {
         // Delete old entries
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
            if (cPref != null && areaType.Value == cPref.editorType && cPref.availableForPlacing) {
               PlaceablePrefabData d = new PlaceablePrefabData {
                  serializationId = indexPref.Key,
                  prefab = cPref,
                  displaySprite = cPref.propIcon
               };
               if (d.displaySprite == null) {
                  d.displaySprite = self._propIconCamera.getIcon(indexPref.Key);
               }
               _placeablePrefabData.Add(d);
            }
         }
      }

      #region Private Variables

      // Prefabs in the scene that can be customized
      private static Dictionary<int, CustomizablePrefab> _customizablePrefabs = new Dictionary<int, CustomizablePrefab>();

      // List of prefabs that can be placed in the scene
      private static List<PlaceablePrefabData> _placeablePrefabData = new List<PlaceablePrefabData>();

      // Currently selected prefab
      private static CustomizablePrefab _selectedPrefab;

      // Prefab that is currently being dragged
      private static CustomizablePrefab _draggedPrefab;

      // Prefab that is being placed as a new prefab
      private static CustomizablePrefab _newPrefab;

      // Are we waiting for the server to respond
      private static bool _waitingServerResponse;

      // State that was approved by a server as valid
      private static Dictionary<int, PrefabState> _serverApprovedState;

      // Custom camera for rendering images at runtime
      private PropIconCamera _propIconCamera;

      #endregion
   }
}
