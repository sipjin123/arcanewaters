using UnityEngine;
using System.Collections.Generic;
using Mirror;
using System.Linq;
using MapCreationTool;

public class PlantableTreeManager : MonoBehaviour
{
   #region Public Variables

   // Maximum distance for player to tether a tree
   public const float TETHER_DISTANCE = 0.64f;

   // Singleton instance
   public static PlantableTreeManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   [Client]
   public void applyTreeDefinitions (PlantableTreeDefinition[] def) {
      _treeDefinitions = def.ToDictionary(d => d.id, d => d);
   }

   [Server]
   public void cachePlantableTreeDefinitions () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         _treeDefinitions = DB_Main.exec(cmd => DB_Main.getPlantableTreeDefinitions(cmd)).ToDictionary(d => d.id, d => d);
      });
   }

   [Server]
   public void playerEnteredFarm (NetEntity player, string areaKey) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         PlantableTreeInstanceData[] instances = DB_Main.exec(cmd => DB_Main.getPlantableTreeInstances(cmd, areaKey)).ToArray();

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            player.rpc.Target_UpdatePlantableTrees(player.connectionToClient, areaKey, instances, _treeDefinitions.Values.ToArray());
            updatePlantableTrees(areaKey, instances, false);
         });
      });
   }

   public void areaCreationFinished (string areaKey) {
      if (_queuedTreeData.Count == 0) {
         return;
      }

      Area area = AreaManager.self.getArea(areaKey);
      if (area == null) {
         D.warning("Area that was created doesn't exist.");
         return;
      }

      // Iterate manually so we can remove nodes without breaking the iterator
      LinkedListNode<PlantableTreeInstanceData> node = _queuedTreeData.First;
      while (node != null) {
         LinkedListNode<PlantableTreeInstanceData> next = node.Next;

         // If this change matches the areaKey, apply it
         if (node.Value.areaKey.Equals(areaKey)) {
            _queuedTreeData.Remove(node);
            updatePlantableTrees(node.Value.id, area, node.Value, !NetworkServer.active);
         }
         node = next;
      }
   }

   public void updatePlantableTrees (int id, Area area, PlantableTreeInstanceData data, bool client) {
      // Can be called on both client and server
      if (data != null) {
         if (_treeDefinitions.TryGetValue(data.treeDefinitionId, out PlantableTreeDefinition def)) {
            MapManager.self.updatePlantableTree(id, area, data, def, client);
         } else {
            D.error("Missing tree definition " + data.treeDefinitionId);
         }
      } else {
         // Tree deleted
         MapManager.self.updatePlantableTree(id, area, null, null, client);
      }
   }

   public void updatePlantableTrees (string areaKey, PlantableTreeInstanceData[] data, bool client) {
      Area area = AreaManager.self.getArea(areaKey);

      if (area != null) {
         foreach (PlantableTreeInstanceData d in data) {
            updatePlantableTrees(d.id, area, d, client);
         }
      } else {
         // It's possible the player received plantable trees before the area was created
         // In that case, we'll cache them and apply once the area is created
         foreach (PlantableTreeInstanceData d in data) {
            _queuedTreeData.AddLast(d);
         }
      }
   }

   [Client]
   public bool playerTriesPlanting (BodyEntity player, Vector2 worldPos) {
      Area area = AreaManager.self.getArea(player.areaKey);

      if (area != null) {
         Vector2 at = area.transform.InverseTransformPoint(worldPos);
         if (canPlayerPlant(player, player.areaKey, at, out string message)) {
            player.rpc.Cmd_PlantTree(at);
            return true;
         } else {
            if (!string.IsNullOrWhiteSpace(message)) {
               FloatingCanvas.instantiateAt(worldPos).asCustomMessage(message);
            }
         }
      }
      return false;
   }

   public bool canPlayerPlant (BodyEntity player, string areaKey, Vector2 localPosition, out string message) =>
      canPlayerPlant(player, areaKey, localPosition, out PlantableTreeDefinition _, out PlantableTree _, out message);

   public bool canPlayerPlant (BodyEntity player, string areaKey, Vector2 localPosition, out PlantableTree treePrefab) =>
      canPlayerPlant(player, areaKey, localPosition, out PlantableTreeDefinition _, out treePrefab, out string _);

   public bool canPlayerPlant (BodyEntity player, string areaKey, Vector2 localPosition, out PlantableTreeDefinition targetTreeDefinition) =>
      canPlayerPlant(player, areaKey, localPosition, out targetTreeDefinition, out PlantableTree _, out string _);

   public bool canPlayerPlant (BodyEntity player, string areaKey, Vector2 localPosition, out PlantableTreeDefinition targetTreeDefinition, out PlantableTree targetPrefab, out string message) {
      targetTreeDefinition = null;
      targetPrefab = null;

      // Check that we have information about this area
      Area area = AreaManager.self.getArea(areaKey);
      if (area == null) {
         message = null;
         return false;
      }

      // Check if player has a seedbag equiped
      WeaponStatData equipedData = EquipmentXMLManager.self.getWeaponData(player.weaponManager.equipmentDataId);
      if (equipedData == null || equipedData.actionType != Weapon.ActionType.PlantTree) {
         message = null;
         return false;
      }

      // Check that we can assosiate a tree definition with this seedbag
      int seedBagId = equipedData.sqlId;
      foreach (PlantableTreeDefinition def in _treeDefinitions.Values) {
         if (def.seedBagId == seedBagId) {
            targetTreeDefinition = def;
            break;
         }
      }
      if (targetTreeDefinition == null) {
         message = null;
         return false;
      }

      // Check that this is a farm
      if (!AreaManager.isFarmingAllowed(areaKey)) {
         message = null;
         return false;
      }

      // Check that farm belongs to this user
      if (!AreaManager.self.isFarmOfUser(areaKey, player.userId) && !CustomGuildMapManager.canUserFarm(areaKey, player)) {
         message = "Not your farm";
         return false;
      }

      // Check that we are not processing another plant at the moment
      if (_plantingIn.Contains(areaKey)) {
         message = "Please wait...";
         return false;
      }

      // Get the prefab
      SpaceRequirer req = null;
      GameObject prefGO = AssetSerializationMaps.getPrefab(targetTreeDefinition.prefabId, area.biome, false);

      if (prefGO != null) {
         if (prefGO.TryGetComponent(out PlantableTree tree)) {
            targetPrefab = tree;
            req = tree.spaceRequirer;
         }
      }

      if (req == null) {
         message = "Error - missing tree config";
         return false;
      }

      // Make sure exact spot is not occupied and that there is a bit of space around it
      if (!req.wouldHaveSpace(area.transform.TransformPoint(localPosition))) {
         message = "No Space";
         return false;
      }

      message = null;
      return true;
   }

   [Server]
   public void plantTree (BodyEntity planter, string areaKey, Vector2 localPosition) {
      if (!canPlayerPlant(planter, areaKey, localPosition, out string message)) {
         return;
      }

      Area area = AreaManager.self.getArea(areaKey);

      // Get seed bag
      int seedBagId = EquipmentXMLManager.self.getWeaponData(planter.weaponManager.equipmentDataId).sqlId;
      int seedBagInstanceId = planter.weaponManager.equippedWeaponId;

      PlantableTreeDefinition treeDefinition = new PlantableTreeDefinition();
      foreach (PlantableTreeDefinition def in _treeDefinitions.Values) {
         if (def.seedBagId == seedBagId) {
            treeDefinition = def;
            break;
         }
      }

      // Create tree instance data
      PlantableTreeInstanceData data = new PlantableTreeInstanceData {
         areaKey = areaKey,
         position = localPosition,
         treeDefinitionId = treeDefinition.id,
         planterUserId = planter.userId,
         growthStagesCompleted = 0,
         lastUpdateTime = TimeManager.self.getLastServerUnixTimestamp()
      };

      if (!_plantingIn.Contains(areaKey)) {
         _plantingIn.Add(areaKey);
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Remove a seed from the bag
         bool success = DB_Main.decreaseQuantityOrDeleteItem(planter.userId, seedBagInstanceId, 1);

         // Stop the process if there were not enough seeds
         if (!success) {
            if (_plantingIn.Contains(areaKey)) {
               _plantingIn.Remove(areaKey);
            }
            return;
         }

         DB_Main.exec((cmd) => DB_Main.createPlantableTreeInstance(cmd, data));

         // If this was the last seed of an equipped bag, unequip it
         Item seedBag = DB_Main.getItem(planter.userId, seedBagInstanceId);
         if (seedBag == null || seedBag.count == 0) {
            planter.rpc.Bkg_RequestSetWeaponId(0);
         }

         // Send the updated shortcuts to the client
         planter.rpc.sendItemShortcutList();

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (_plantingIn.Contains(areaKey)) {
               _plantingIn.Remove(areaKey);
            }
            // Check that area still exists (might be destroyed, that's fine, changes are stored in the database)
            if (area != null) {
               // Notify clients to update their trees
               planter.rpc.Rpc_UpdatePlantableTrees(data.id, areaKey, data);
               updatePlantableTrees(data.id, area, data, false);
            }
         });
      });
   }

   [Client]
   public void playerSwungAtTree (NetEntity player, PlantableTree tree) {
      // Called when player swings any item while being close to a tree
      if (canPlayerWater(player, tree)) {
         Global.player.rpc.Cmd_WaterTree(tree.data.id);
      }
   }

   [Client]
   public void playerSwungAtTreeAxeRange (NetEntity player, PlantableTree tree) {
      // Called when player swings any item while being in chop range to a tree
      if (canPlayerChop(player, tree)) {
         Global.player.rpc.Cmd_ChopTree(tree.data.id);
         if (!Util.isHost()) {
            tree.receiveChop(player.transform.position.x < tree.transform.position.x);
         }
      }
   }

   public bool canPlayerChop (NetEntity player, PlantableTree tree) {
      if (player == null || !(player is PlayerBodyEntity)) {
         return false;
      }

      if ((player as PlayerBodyEntity).weaponManager.actionType != Weapon.ActionType.Chop) {
         return false;
      }

      if (tree.data == null) {
         return false;
      }

      // Check that we have information about this area
      Area area = AreaManager.self.getArea(tree.data.areaKey);
      if (area == null) {
         D.warning($"Can't chop tree in { tree.data.areaKey} because missing area data!");
         return false;
      }

      // Check that it is a farm and it belongs to this user
      if (!AreaManager.self.isFarmOfUser(tree.data.areaKey, player.userId) && !CustomGuildMapManager.canUserFarm(tree.data.areaKey, player)) {
         return false;
      }

      if (!_treeDefinitions.TryGetValue(tree.data.treeDefinitionId, out PlantableTreeDefinition def)) {
         return false;
      }

      if (!def.isFullyGrown(tree.data, TimeManager.self.getLastServerUnixTimestamp())) {
         return false;
      }

      return true;
   }

   [Server]
   public void chopTree (BodyEntity player, int treeId) {
      if (!MapManager.self.tryGetPlantableTree(treeId, out PlantableTree tree)) {
         return;
      }
      if (!canPlayerChop(player, tree)) {
         return;
      }

      MapManager.self.tryGetPlantableTree(treeId, out PlantableTreeInstanceData treeData);
      _treeDefinitions.TryGetValue(treeData.treeDefinitionId, out PlantableTreeDefinition treeDef);
      Area area = AreaManager.self.getArea(treeData.areaKey);

      // Apply a chop to the tree
      tree.receiveChop(player.transform.position.x < tree.transform.position.x);
      player.rpc.Rpc_ReceiveChopTreeVisual(treeId, player.userId, player.transform.position.x < tree.transform.position.x);

      // Check if the tree has been chopped
      if (tree.currentChopCount >= 3) {
         tree.currentChopCount = 0;
         // Now the tree can either be completely chopped, or leave a stump, or a stump can be chopped
         List<Item> loot = treeDef.isStump(treeData) ? treeDef.getStumpHarvestLoot() : treeDef.getMainHarvestLoot();

         if (!treeDef.isStump(treeData) && treeDef.leavesStump) {
            // Turn tree to stump
            treeDef.turnTreeToStump(treeData, TimeManager.self.getLastServerUnixTimestamp());

            // Shove the update to the database in the background
            Util.dbBackgroundExec((cmd) => DB_Main.updatePlantableTreeInstance(cmd, treeData));

            // Update the tree and notify user of it
            player.rpc.Rpc_UpdatePlantableTrees(treeId, treeData.areaKey, treeData);
            updatePlantableTrees(treeId, area, treeData, false);
         } else {
            // Delete the tree from db
            Util.dbBackgroundExec((cmd) => DB_Main.deletePlantableTreeInstance(cmd, treeId));

            // Destroy the tree in server and client
            player.rpc.Rpc_UpdatePlantableTrees(treeId, treeData.areaKey, null);
            updatePlantableTrees(treeId, area, null, false);
         }

         // Drop loot for harvest
         if (InstanceManager.self.tryGetInstance(player.instanceId, out Instance instance)) {
            // Drop resources
            foreach (Item item in loot) {
               dropItem(instance, item, treeData.position, player);
            }
         }
      }
   }

   private void dropItem (Instance instance, Item item, Vector2 treePos, NetEntity forPlayer) {
      instance.dropNewItem(
         item,
         treePos + Vector2.up * 0.32f,
         (droppedItem) => {
            droppedItem.limitToUserId = forPlayer.userId;
            droppedItem.itemSpriteOverride = "Assets/Resources/Sprites/Icons/LogPickup.png";
            droppedItem.appearLifeTime = 0.75f;
            droppedItem.appearArchHeight = 0.48f;
         });
   }

   private bool canPlayerWater (NetEntity player, PlantableTree tree) {
      if (player == null || !(player is PlayerBodyEntity)) {
         return false;
      }

      if ((player as PlayerBodyEntity).weaponManager.actionType != Weapon.ActionType.WaterCrop) {
         return false;
      }

      if (tree.data == null) {
         return false;
      }

      if (!_treeDefinitions.TryGetValue(tree.data.treeDefinitionId, out PlantableTreeDefinition def)) {
         return false;
      }

      if (!def.needsWatering(tree.data, TimeManager.self.getLastServerUnixTimestamp())) {
         return false;
      }

      return true;
   }

   [Server]
   public void waterTree (BodyEntity player, int treeId) {
      if (!MapManager.self.tryGetPlantableTree(treeId, out PlantableTree tree)) {
         return;
      }
      if (!canPlayerWater(player, tree)) {
         return;
      }

      MapManager.self.tryGetPlantableTree(treeId, out PlantableTreeInstanceData treeData);
      _treeDefinitions.TryGetValue(treeData.treeDefinitionId, out PlantableTreeDefinition treeDef);
      Area area = AreaManager.self.getArea(treeData.areaKey);

      // Water tree
      treeDef.waterTree(treeData, TimeManager.self.getLastServerUnixTimestamp());

      // Shove the update to the database in the background
      Util.dbBackgroundExec((cmd) => DB_Main.updatePlantableTreeInstance(cmd, treeData));

      // Update the tree and notify user of it
      player.rpc.Rpc_UpdatePlantableTrees(treeId, treeData.areaKey, treeData);
      updatePlantableTrees(treeId, area, treeData, false);
   }

   #region Private Variables

   // The types of trees we can plant
   private Dictionary<int, PlantableTreeDefinition> _treeDefinitions = new Dictionary<int, PlantableTreeDefinition>();

   // In case we receive tree data before area is created, store it here and wait
   private LinkedList<PlantableTreeInstanceData> _queuedTreeData = new LinkedList<PlantableTreeInstanceData>();

   // Stores which areas are processing a planting action right now
   private HashSet<string> _plantingIn = new HashSet<string>();

   #endregion
}
