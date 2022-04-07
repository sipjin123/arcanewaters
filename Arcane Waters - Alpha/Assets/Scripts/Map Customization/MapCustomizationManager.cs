using System.Collections;
using System.Collections.Generic;
using MapCreationTool;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Tilemaps;
using Mirror;
using MapCustomization;

public class MapCustomizationManager : NetworkBehaviour, IObserver
{
   #region Public Variables

   // When user creats new prefabs, from where to start their IDS
   const int NEW_PREFAB_ID_START = 100000000;

   // How much should distance should be between player and prefab when changing prefab position
   const float MIN_PREFAB_DISTANCE_FROM_PLAYER_CLIENT = 0.24f;
   const float MIN_PREFAB_DISTANCE_FROM_PLAYER_SERVER = 0.12f;

   // Color for outline that is used when prefab isnt selected but is ready for input
   public static Color prefabReadyColor = new Color32(125, 212, 255, 255);

   // Color for outline that is used when user is hovering a prefab
   public static Color prefabHoveredColor = new Color32(0, 109, 255, 255);

   // Color for outline that is used when user is modifying a prefab and the current changes are valid
   public static Color prefabValidColor = new Color32(3, 183, 29, 255);

   // Color for outline that is used when user is modifying a prefab and the current changes are invalid
   public static Color prefabInvalidColor = new Color32(184, 15, 15, 255);

   // --------------------------------------------------------------------

   // List of all managers that exist currenly
   public static readonly Dictionary<int, MapCustomizationManager> allManagers = new Dictionary<int, MapCustomizationManager>();

   // Target instance id of this customization manager
   [SyncVar] public int instanceId;

   // Area key of the area this manager is responsible for
   [SyncVar] public string areaKey;

   // Biome of the target area
   [SyncVar] public Biome.Type areaBiome;

   // UserId of the player that owns the area
   [SyncVar] public int areaOwnerId;

   // The GuildId of the guild that owns the area
   [SyncVar(hook = nameof(guildIdUpdated))] public int areaGuildId;

   // Inventory, from which to take items when customizing:
   // UserId for private maps, -GuildInventoryId for guilds
   [SyncVar] public int itemSourceUserId;

   // Amount of items left in the inventory
   public readonly SyncList<ItemTypeCount> itemSource = new SyncList<ItemTypeCount>();

   // Is local player in customization mode right now
   public bool isLocalPlayerCustomizing = false;

   // Reference to the selection arrows
   public GameObject selectionArrows;

   #endregion

   private void OnDestroy () {
      if (allManagers.ContainsKey(instanceId)) {
         allManagers.Remove(instanceId);
      }

      if (isLocalPlayerCustomizing && tryGetCurentLocalManager(out MapCustomizationManager manager)) {
         if (manager == this) {
            exitCustomization();
         }
      }
   }

   public override void OnStartServer () {
      if (!allManagers.ContainsKey(instanceId)) {
         allManagers.Add(instanceId, this);
      }
   }

   public override void OnStartClient () {
      itemSource.Callback += itemSourceUpdated;
      guildIdUpdated(areaGuildId, areaGuildId);

      if (!allManagers.ContainsKey(instanceId)) {
         allManagers.Add(instanceId, this);
      }
   }

   private void itemSourceUpdated (SyncList<ItemTypeCount>.Operation op, int index, ItemTypeCount oldItem, ItemTypeCount newItem) {
      _waitingForItems = false;
      CustomizationUI.updatePropCount(itemSource);
   }

   private void guildIdUpdated (int oldId, int newId) {
      CustomizationUI.self.guildInventoryText.SetActive(newId > 0);
   }

   private void OnEnable () {
      _propIconCamera = GetComponentInChildren<PropIconCamera>();
      selectionArrows.SetActive(false);
   }

   private void Update () {
      if (NetworkClient.active && Global.player != null) {
         // Check if player should be customizing
         bool shouldBeCustomizing = AreaManager.self.tryGetCustomMapManager(Global.player.areaKey, out CustomMapManager cmm) && // Check that this is a customizable area
            AreaManager.self.getArea(Global.player.areaKey) != null && // Check that the area is already created
            (areaKey == Global.player.areaKey) && // Check that the area hasn't changed to a different customizable area
            instanceId == Global.player.instanceId &&
            (Global.player as PlayerBodyEntity).weaponManager.actionType == Weapon.ActionType.CustomizeMap && // Check that customization action weapon is equipped
            (Global.player as PlayerBodyEntity).weaponManager.weaponType > 0 &&
            hasPermissionToCustomize(Global.player, areaKey); // Make sure that the user can only edit on a map where they have permissions

         if (!isLocalPlayerCustomizing && shouldBeCustomizing) {
            enterCustomization(Global.player.areaKey);
         } else if (isLocalPlayerCustomizing && !shouldBeCustomizing) {
            exitCustomization();
         }
      }

      if (NetworkServer.active) {
         // If it's been a long time since last change process, something went wrong
         if (_processingChanges && Time.time - _lastChangeProcess > 20f) {
            _processingChanges = false;
            D.error("Something went wrong - map customization stuck");
         }

         if (!_processingChanges && _scheduledChanges.Count > 0) {
            var next = _scheduledChanges.Dequeue();
            if (next.changer == null) {
               // This is probably ok - user changed something and immediately disconnected, disregard it
               return;
            }

            _processingChanges = true;
            _lastChangeProcess = Time.time;
            processPrefabCustomization(next.change, next.changer);
         }
      }
   }

   public void enterCustomization (string areaKey) {
      if (isLocalPlayerCustomizing) {
         D.warning("Trying to open map customization even though it is currently active");
         return;
      }

      if (!AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         D.error("Trying to customize a map that is not an owned map: " + areaKey);
         return;
      }

      if (!CustomMapManager.isUserSpecificAreaKey(areaKey) && !CustomMapManager.isGuildSpecificAreaKey(areaKey) && !CustomMapManager.isGuildHouseAreaKey(areaKey)) {
         D.error("Trying to customize a map by a key that is not user-specific and is not a guild map: " + areaKey);
         return;
      }

      if (!AreaManager.self.tryGetArea(areaKey, out Area area)) {
         D.error("Area does not exist");
         return;
      }

      isLocalPlayerCustomizing = true;

      StartCoroutine(enterCustomizationRoutine(area));
   }

   private IEnumerator enterCustomizationRoutine (Area area) {
      // Gather data about prefabs that can be placed by the user
      updatePlaceablePrefabData();
      yield return new WaitForEndOfFrame();

      CustomizationUI.ensureShowing();
      CustomizationUI.setLoading(true);

      // Fetch customization data that is saved for this map
      CustomizationUI.setPlaceablePrefabData(this, _placeablePrefabData, new List<ItemTypeCount>());
      Cmd_PlayerIsStartingToCustomize();

      _waitingForItems = true;

      // Gather prefabs from the scene that can be customized
      CustomizablePrefab[] prefabs = area.GetComponentsInChildren<CustomizablePrefab>().Where(cp => !cp.isPermanent).ToArray();
      _customizablePrefabs = prefabs.ToDictionary(p => p.customizedState.id, p => p);
      _serverApprovedState = prefabs.ToDictionary(p => p.customizedState.id, p => p.customizedState);

      yield return new WaitForEndOfFrame();

      // Enable prefab outlines
      updatePrefabOutlines(null);
   }

   public void exitCustomization () {
      isLocalPlayerCustomizing = false;
      selectPrefab(null);
      hideSelectionArrows();
      CustomizationUI.ensureHidden();

      StopAllCoroutines();

      // Disable prefab outlines
      foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
         prefab.setOutline(false, false, false, false);
      }
   }

   public void keyDeleteAt (Vector2 worldPos) {
      CustomizablePrefab hoveredPrefab = getPrefabAtPosition(worldPos);

      if (hoveredPrefab == null) return;

      if (hoveredPrefab.unappliedChanges.created) {
         hoveredPrefab.revertUnappliedChanges();
      } else {
         hoveredPrefab.unappliedChanges.deleted = true;
         hoveredPrefab.unappliedChanges.clearLocalPosition();
      }

      if (hoveredPrefab.anyUnappliedState()) {
         if (validatePrefabChanges(areaKey, Global.player, areaBiome, itemSource, hoveredPrefab.unappliedChanges, false, out string errorMessage)) {
            Cmd_AddPrefabCustomization(hoveredPrefab.unappliedChanges);

            hoveredPrefab.submitUnappliedChanges();
         } else {
            hoveredPrefab.revertUnappliedChanges();
         }
      }

      cancelCurrentAction();
   }

   public void pointerEnter (Vector2 worldPosition) {
      // If we have a prefab to place, show it in the map
      if (CustomizationUI.getSelectedPrefabData() != null) {
         updateToBePlacedPrefab(worldPosition, CustomizationUI.getSelectedPrefabData().Value.serializationId);
         updatePrefabOutlines(null);
      }
   }

   public void pointerExit (Vector2 worldPosition) {
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
   public void pointerHover (Vector2 worldPosition) {
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
   [Client]
   public void pointerDrag (Vector2 delta) {
      // Save starting position of drag operation
      if (_selectedPrefab != null && _draggedPrefab != null) {
         if (_isBeginningOfDrag) {
            _dragStartPosition = _selectedPrefab.unappliedChanges.localPosition;
            _isBeginningOfDrag = false;
         }

         if (!_selectedPrefab.unappliedChanges.isLocalPositionSet()) {
            _selectedPrefab.unappliedChanges.localPosition = _selectedPrefab.customizedState.localPosition + delta;
         } else {
            _selectedPrefab.unappliedChanges.localPosition += delta;
         }

         _selectedPrefab.transform.localPosition = _selectedPrefab.unappliedChanges.localPosition;
         _selectedPrefab.GetComponent<ZSnap>()?.snapZ();

         // Move selection arrows with prefab
         selectionArrows.transform.position = _selectedPrefab.transform.position;
         selectionArrows.SetActive(true);
         updatePrefabOutlines(null);
      }
   }

   // Right click will cancel the process of placing a new prefab
   public void rightClick () {
      cancelCurrentAction();
   }

   public void cancelCurrentAction () {
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

   public void pointerDown (Vector2 worldPosition) {
      if (CustomizationUI.getSelectedPrefabData() != null) {
         updateToBePlacedPrefab(worldPosition, CustomizationUI.getSelectedPrefabData().Value.serializationId);
         _customizablePrefabs.Add(_newPrefab.unappliedChanges.id, _newPrefab);
         if (validatePrefabChanges(areaKey, Global.player, areaBiome, itemSource, _newPrefab.unappliedChanges, false, out string errorMessage)) {
            _newPrefab.setGameInteractionsActive(true);
            Cmd_AddPrefabCustomization(_newPrefab.unappliedChanges);
            SoundEffectManager.self.playFmodSfx(SoundEffectManager.PLACE_EDITABLE_OBJECT, position: worldPosition);
            selectPrefab(_newPrefab);

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

   public void pointerUp (Vector2 worldPosition) {
      // If dragging a prefab before mouse pointer up, validate the prefab now that the dragging is over
      if (_draggedPrefab != null) {
         // Do not allow placing of a prefab in an invalid location
         if (!validatePrefabChanges(areaKey, Global.player, areaBiome, itemSource, _draggedPrefab.unappliedChanges, false, out string errorMessage)) {
            selectPrefab(_draggedPrefab);
            _selectedPrefab.unappliedChanges.localPosition = _dragStartPosition;
            _selectedPrefab.submitUnappliedChanges();
         } else {
            Cmd_AddPrefabCustomization(_draggedPrefab.unappliedChanges);
            _draggedPrefab.submitUnappliedChanges();
         }
         _draggedPrefab = null;
         _isBeginningOfDrag = true;
      }

      updatePrefabOutlines(worldPosition);
   }

   private void updateToBePlacedPrefab (Vector3 worldPosition, int serializationId) {
      if (!AreaManager.self.tryGetArea(areaKey, out Area area)) {
         return;
      }

      // If the prefab has changed, destroy the current prefab before creating a new one
      if ((_newPrefab != null) && (_newPrefab.unappliedChanges.serializationId != serializationId)) {
         _newPrefab.revertUnappliedChanges();
         _newPrefab = null;
         Destroy(_newPrefab);
         _newPrefab = MapManager.self.createPrefab(area, areaBiome, newPrefabState(worldPosition, serializationId), false);
         _newPrefab.setGameInteractionsActive(false);
         _soundHasBeenPlayed = false;
      }

      // If the prefab is missing, create a new one and initialize it
      if (_newPrefab == null) {
         _newPrefab = MapManager.self.createPrefab(area, areaBiome, newPrefabState(worldPosition, serializationId), false);
         _newPrefab.name += " (being placed...)";
         _newPrefab.setGameInteractionsActive(false);
         _soundHasBeenPlayed = false;
      }

      _newPrefab.unappliedChanges.localPosition = area.prefabParent.transform.InverseTransformPoint(worldPosition);
      _newPrefab.transform.localPosition = _newPrefab.unappliedChanges.localPosition;
      _newPrefab.GetComponent<ZSnap>()?.snapZ();

      if (!_soundHasBeenPlayed) {
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.PLACE_EDITABLE_OBJECT, position: worldPosition);
         _soundHasBeenPlayed = true;
      }
   }

   private PrefabState newPrefabState (Vector3 worldPosition, int serializationId) {
      if (!AreaManager.self.tryGetArea(areaKey, out Area area)) {
         throw new System.Exception("Could not find area " + areaKey);
      }

      // Calculate the state of the new prefab
      return new PrefabState {
         id = newPrefabId(),
         created = true,
         localPosition = area.prefabParent.transform.InverseTransformPoint(worldPosition),
         serializationId = serializationId
      };
   }

   private int newPrefabId () {
      // Get a random ID to avoid collisions
      HashSet<int> existing = new HashSet<int>(_customizablePrefabs.Keys);

      int id = 0;
      for (int i = 0; i < 1000; i++) {
         id = UnityEngine.Random.Range(NEW_PREFAB_ID_START, int.MaxValue);
         if (!existing.Contains(id)) {
            break;
         }
      }

      return id;
   }

   private void updatePrefabOutlines (Vector2? hoveredPosition) {
      CustomizablePrefab hoveredPrefab = hoveredPosition == null ? null : getPrefabAtPosition(hoveredPosition.Value);

      foreach (CustomizablePrefab prefab in _customizablePrefabs.Values) {
         prefab.setOutline(true, prefab == hoveredPrefab, false, false);
      }

      if (_newPrefab != null) {
         bool valid = validatePrefabChanges(areaKey, Global.player, areaBiome, itemSource, _newPrefab.unappliedChanges, false, out string errorMessage);
         _newPrefab.setOutline(true, true, true, valid);

         showSelectionArrows();
         selectionArrows.transform.position = _newPrefab.transform.position;
         SelectionSpriteBuildMode.self.setDistances(_newPrefab);
         SelectionSpriteBuildMode.self.setColors(CustomizablePrefab.getIndicatorColor(true, true, true, valid));

         return;
      }

      if (_selectedPrefab != null) {
         bool valid = validatePrefabChanges(areaKey, Global.player, areaBiome, itemSource, _selectedPrefab.unappliedChanges, false, out string errorMessage);
         _selectedPrefab.setOutline(true, _selectedPrefab == hoveredPrefab, true, valid);

         showSelectionArrows();
         selectionArrows.transform.position = _selectedPrefab.transform.position;
         SelectionSpriteBuildMode.self.setDistances(_selectedPrefab);
         SelectionSpriteBuildMode.self.setColors(CustomizablePrefab.getIndicatorColor(true, true, _selectedPrefab == _draggedPrefab, valid));

         return;
      }

      if (hoveredPrefab != null) {
         showSelectionArrows();
         selectionArrows.transform.position = hoveredPrefab.transform.position;
         SelectionSpriteBuildMode.self.setDistances(hoveredPrefab);
         SelectionSpriteBuildMode.self.setColors(CustomizablePrefab.getIndicatorColor(true, true, false, false));

         return;
      }

      hideSelectionArrows();
   }

   public static bool tryGetCurentLocalManager (out MapCustomizationManager manager) {
      manager = null;
      if (Global.player == null) {
         return false;
      }

      if (allManagers.TryGetValue(Global.player.instanceId, out manager)) {
         return true;
      }

      return false;
   }

   private CustomizablePrefab getPrefabAtPosition (Vector2 position) {
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

   public static bool hasPermissionToCustomize (NetEntity entity, string areaKey) {
      return CustomMapManager.isUserSpecificAreaKey(areaKey) && CustomMapManager.getUserId(areaKey) == entity.userId ||
         (CustomMapManager.isGuildSpecificAreaKey(areaKey) && CustomMapManager.getGuildId(areaKey) == entity.guildId) ||
         (CustomMapManager.isGuildHouseAreaKey(areaKey) && CustomMapManager.getGuildId(areaKey) == entity.guildId);
   }

   public void selectPrefab (CustomizablePrefab prefab) {
      if (_selectedPrefab != null) {
         _selectedPrefab.setGameInteractionsActive(true);
         _selectedPrefab.setOutline(true, false, false, false);
      }

      _selectedPrefab = prefab;

      if (_selectedPrefab != null) {
         _selectedPrefab.setGameInteractionsActive(false);
         CustomizationUI.selectEntry(null);
         selectionArrows.transform.position = _selectedPrefab.transform.position;
         SelectionSpriteBuildMode.self.setDistances(_selectedPrefab);
         showSelectionArrows();
      }
   }

   [Client]
   private void showSelectionArrows () {
      selectionArrows.SetActive(true);
   }

   [Client]
   private void hideSelectionArrows () {
      selectionArrows.SetActive(false);
   }

   public bool validatePrefabChanges (string areaKey, NetEntity madeBy, Biome.Type biome, IList<ItemTypeCount> remainingItems, PrefabState changes, bool isServer, out string errorMessage) {
      // If we are host, and this is was validated on the 'client' side, lets just assume it's valid
      if (Util.isHost() && isServer) {
         errorMessage = null;
         return true;
      }

      if (!AreaManager.self.tryGetArea(areaKey, out Area area)) {
         errorMessage = "Area does not exist";
         return false;
      }

      if (madeBy == null || !madeBy.areaKey.Equals(areaKey) || instanceId != madeBy.instanceId) {
         errorMessage = "Player is not in the area";
         return false;
      }

      if (!hasPermissionToCustomize(madeBy, areaKey)) {
         errorMessage = "Not allowed";
         return false;
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

   public int amountOfPropLeft (IList<ItemTypeCount> items, CustomizablePrefab propPrefab) {
      return amountOfPropLeft(items, propPrefab.propItemCategory, propPrefab.propDefinitionId);
   }

   public int amountOfPropLeft (IList<ItemTypeCount> items, Item.Category category, int itemTypeId) {
      int c = 0;

      foreach (ItemTypeCount item in items) {
         if (item.itemTypeId == itemTypeId && item.category == category) {
            return c += item.count;
         }
      }

      return c;
   }

   [Client]
   public void serverAddPrefabChangeSuccess (PrefabState changes, int changedByUserId) {
      if (Global.player == null) {
         return;
      }

      // If we are customizing this map, the customization process will handle the change
      // Just keep track of changes that were approved by the server
      if (Global.player.userId == changedByUserId) {
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
         MapManager.self.addCustomizations(area, areaBiome, changes);
      } else {
         D.debug("Missing area! " + area);
      }
   }

   public void removeTracked (int id, CustomizablePrefab prefab) {
      if (_customizablePrefabs.ContainsKey(id)) {
         _customizablePrefabs.Remove(id);
      }
   }

   public void startTracking (CustomizablePrefab prefab) {
      if (!_customizablePrefabs.ContainsKey(prefab.customizedState.id)) {
         _customizablePrefabs.Add(prefab.customizedState.id, prefab);
      }

      if (!_serverApprovedState.ContainsKey(prefab.customizedState.id)) {
         _serverApprovedState.Add(prefab.customizedState.id, prefab.customizedState);
      }
   }

   public void serverAddPrefabChangeFail (PrefabState changes, string message) {
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
            removeTracked(changes.id, changedPrefab);
            Destroy(changedPrefab.gameObject);
         } else if (_serverApprovedState.TryGetValue(changes.id, out PrefabState approvedState)) {
            if (approvedState.serializationId != changedPrefab.unappliedChanges.serializationId) {
               Debug.LogWarning("Different serialization id for prefab state " + approvedState);
            } else {
               changedPrefab.unappliedChanges = approvedState;
               changedPrefab.submitUnappliedChanges();
            }
         }
      } else if (changes.deleted && _serverApprovedState.TryGetValue(changes.id, out PrefabState approvedState)) {
         if (AreaManager.self.tryGetArea(areaKey, out Area area)) {
            CustomizablePrefab previouslyDeletedPrefab = MapManager.self.createPrefab(area, areaBiome, approvedState, true);
            _customizablePrefabs.Add(previouslyDeletedPrefab.customizedState.id, previouslyDeletedPrefab);
         }
      }
   }

   private void updatePlaceablePrefabData () {
      // Delete old entries
      _placeablePrefabData.Clear();

      // Get the type of the current area
      if (!AreaManager.self.tryGetArea(areaKey, out Area area)) {
         D.error("Could not find area " + areaKey);
      }

      EditorType? areaType = AreaManager.self.getAreaEditorType(area.baseAreaKey);
      if (areaType == null) {
         D.error($"Area { area.areaKey }-{ area.baseAreaKey } does not have a type stored in AreaManager");
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
               d.displaySprite = _propIconCamera.getIcon(indexPref.Key);
            }
            _placeablePrefabData.Add(d);
         }
      }
   }

   [Command(ignoreAuthority = true)]
   public void Cmd_PlayerIsStartingToCustomize () {
      refreshItemSourceFromDB();
   }

   [ClientRpc]
   private void Rpc_ItemSourceRefreshed () {
      CustomizationUI.setLoading(false);
      CustomizationUI.updatePropCount(itemSource);
   }

   [Server]
   public void refreshItemSourceFromDB () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Figure out the inventory source we should use
         int sourceId = 0;

         if (CustomMapManager.isGuildSpecificAreaKey(areaKey) || CustomMapManager.isGuildHouseAreaKey(areaKey)) {
            int guildId = CustomMapManager.getGuildId(areaKey);
            if (guildId > 0) {
               // Fetch guild inventory id
               GuildInfo guild = DB_Main.getGuildInfo(guildId);
               if (guild != null) {
                  sourceId = -guild.inventoryId;
               }

            }
         } else if (CustomMapManager.isUserSpecificAreaKey(areaKey)) {
            int userId = CustomMapManager.getUserId(areaKey);
            sourceId = userId;
         }
         itemSourceUserId = sourceId;

         List<ItemTypeCount> items = DB_Main.getItemTypeCounts(sourceId);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // If we have less items than there is currently, remove last ones
            while (items.Count < itemSource.Count) {
               itemSource.RemoveAt(items.Count - 1);
            }

            // Update items with the new ones
            for (int i = 0; i < itemSource.Count; i++) {
               itemSource[i] = items[i];
            }

            // If we have more items than there is currently, add more
            for (int i = itemSource.Count; i < items.Count; i++) {
               itemSource.Add(items[i]);
            }

            Rpc_ItemSourceRefreshed();
         });
      });
   }

   [Command(ignoreAuthority = true)]
   public void Cmd_AddPrefabCustomization (PrefabState changes, NetworkConnectionToClient sender = null) {
      addPrefabCustomization(changes, sender);
   }

   [Server]
   public void addPrefabCustomization (PrefabState changes, NetworkConnectionToClient sender = null) {
      _scheduledChanges.Enqueue((sender, changes));
   }

   [Server]
   private void processPrefabCustomization (PrefabState changes, NetworkConnectionToClient sender = null) {
      Area area = AreaManager.self.getArea(areaKey);

      NetEntity modifier = sender.identity.GetComponent<NetEntity>();

      int customizationsOwnerId = CustomMapManager.getMapChangesOwnerId(areaKey);
      if (customizationsOwnerId == 0) {
         _processingChanges = false;
         Target_FailAddPrefabCustomization(sender, changes, "Could not find map's owner");
         return;
      }

      // Find the customizable prefab that is being targeted
      if (!AssetSerializationMaps.tryGetPrefabGame(changes.serializationId, areaBiome, out CustomizablePrefab prefab)) {
         _processingChanges = false;
         Target_FailAddPrefabCustomization(sender, changes, "Could not find associated object " + changes.serializationId);
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int count = DB_Main.getItemCountByType(itemSourceUserId, (int) prefab.propItemCategory, prefab.propDefinitionId);
         List<ItemTypeCount> items = new List<ItemTypeCount>() { new ItemTypeCount { itemTypeId = prefab.propDefinitionId, category = prefab.propItemCategory, count = count } };

         // Figure out the base map id of the area
         if (!AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager manager)) {
            _processingChanges = false;
            Target_FailAddPrefabCustomization(sender, changes, "Could not find custom map manager for " + areaKey);
            return;
         }

         int baseMapId = manager.Bkg_GetBaseMapIdFromDB(areaOwnerId, areaGuildId);
         if (baseMapId <= 0) {
            string errorString = "Couldn't find the base of the map we are customising. OwnerId: " + areaOwnerId + ", GuildId: " + areaGuildId;
            D.error(errorString);
            _processingChanges = false;
            Target_FailAddPrefabCustomization(sender, changes, errorString);
            return;
         }

         // Get the current state of the object
         PrefabState currentState = DB_Main.exec((cmd) => DB_Main.getMapCustomizationChanges(cmd, baseMapId, customizationsOwnerId, changes.id));

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // If user is creating a new object, check that ID does not clash with an existing object
            if (changes.created && (currentState.id != -1 && !currentState.deleted)) {
               _processingChanges = false;
               Target_FailAddPrefabCustomization(sender, changes, "Action interrupted");
               return;
            }

            // Check if changes are valid
            if (!validatePrefabChanges(areaKey, modifier, areaBiome, items, changes, true, out string errorMessage)) {
               _processingChanges = false;
               Target_FailAddPrefabCustomization(sender, changes, errorMessage);
               return;
            }

            int modifierUserId = modifier.userId;

            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               // Control item source
               ItemTypeCount itemSourceDelta = new ItemTypeCount { category = prefab.propItemCategory, itemTypeId = prefab.propDefinitionId, count = 0 };

               // If creating a new prefab, remove an item from the inventory
               if (changes.created) {
                  if (!DB_Main.decreaseQuantityOrDeleteItem(itemSourceUserId, prefab.propItemCategory, prefab.propDefinitionId, 1)) {
                     UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                        _processingChanges = false;
                        Target_FailAddPrefabCustomization(sender, changes, "Could not find associated item in database");
                     });
                     return;
                  }
                  itemSourceDelta.count = -1;
               }

               // Set changes in the database
               bool createdByUser = currentState.created;
               currentState = currentState.id == -1 ? changes : currentState.add(changes);
               DB_Main.exec((cmd) => DB_Main.setMapCustomizationChanges(cmd, baseMapId, customizationsOwnerId, currentState));

               // If deleting a prefab and it's not placed in map editor, return to inventory
               if (changes.deleted && createdByUser) {
                  Item newItem = new Item(0, prefab.propItemCategory, prefab.propDefinitionId, 1, "", "", 100);
                  DB_Main.createItemOrUpdateItemCount(itemSourceUserId, newItem);
                  itemSourceDelta.count = 1;
               }

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  // Set changes in the server
                  MapManager.self.addCustomizations(area, areaBiome, changes);

                  // Notify all clients about them
                  Rpc_AddPrefabCustomizationSuccess(changes, modifierUserId);

                  // Update items
                  if (itemSourceDelta.count != 0) {
                     addItemToItemSource(itemSourceDelta);
                  }

                  _processingChanges = false;
               });
            });
         });
      });
   }

   [TargetRpc]
   public void Target_FailAddPrefabCustomization (NetworkConnection target, PrefabState failedChanges, string message) {
      serverAddPrefabChangeFail(failedChanges, message);
   }

   [ClientRpc]
   public void Rpc_AddPrefabCustomizationSuccess (PrefabState changes, int changedByUserId) {
      serverAddPrefabChangeSuccess(changes, changedByUserId);
   }

   [Server]
   private void addItemToItemSource (ItemTypeCount item) {
      for (int i = 0; i < itemSource.Count; i++) {
         if (itemSource[i].itemTypeId == item.itemTypeId && itemSource[i].category == item.category) {
            ItemTypeCount newItem = itemSource[i];
            newItem.count += item.count;
            itemSource[i] = newItem;
            return;
         }
      }
      if (item.count > 0) {
         itemSource.Add(item);
      }
   }

   public int getInstanceId () {
      return instanceId;
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

   // State that was approved by a server as valid
   private static Dictionary<int, PrefabState> _serverApprovedState;

   // Are we waiting for the placeable items to arrive
   private bool _waitingForItems = false;

   // Custom camera for rendering images at runtime
   private PropIconCamera _propIconCamera;

   // Keep track of sound state in update loop
   private bool _soundHasBeenPlayed = false;

   // Store the position of the prefab when dragging first starts
   private Vector2 _dragStartPosition;

   // Flag to keep track of when the dragging of a prefab is starting
   private bool _isBeginningOfDrag = true;

   // Changes that are scheduled to be processed (server-only)
   private Queue<(NetworkConnectionToClient changer, PrefabState change)> _scheduledChanges = new Queue<(NetworkConnectionToClient changer, PrefabState change)>();

   // Are we processing a prefab change currently (server-only)
   private bool _processingChanges = false;

   // Last time we processed a prefab change
   private float _lastChangeProcess = 0;

   #endregion
}