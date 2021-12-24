using UnityEngine;
using System.Collections.Generic;
using Mirror;
using System.Linq;

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
      D.log("Using database to fetch tree definition! Refactor to XML workflow later.");
      _treeDefinitions = def.ToDictionary(d => d.id, d => d);
   }

   [Server]
   public void playerEnteredFarm (NetEntity player, string areaKey) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         D.log("Using database to fetch tree definition! Refactor to XML workflow later.");
         _treeDefinitions = DB_Main.exec(cmd => DB_Main.getPlantableTreeDefinitions(cmd)).ToDictionary(d => d.id, d => d);

         PlantableTreeInstanceData[] instances = DB_Main.exec(cmd => DB_Main.getPlantableTreeInstances(cmd, areaKey)).ToArray();

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            player.rpc.Target_UpdatePlantableTrees(player.connectionToClient, areaKey, instances, _treeDefinitions.Values.ToArray());
            updatePlantableTrees(areaKey, instances);
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
            updatePlantableTrees(node.Value.id, area, node.Value);
         }
         node = next;
      }
   }

   public void updatePlantableTrees (int id, Area area, PlantableTreeInstanceData data) {
      // Can be called on both client and server
      if (data != null) {
         if (_treeDefinitions.TryGetValue(data.treeDefinitionId, out PlantableTreeDefinition def)) {
            MapManager.self.updatePlantableTree(id, area, data, def);
         } else {
            D.error("Missing tree definition " + data.treeDefinitionId);
         }
      } else {
         // Tree deleted
         MapManager.self.updatePlantableTree(id, area, null, null);
      }
   }

   public void updatePlantableTrees (string areaKey, PlantableTreeInstanceData[] data) {
      Area area = AreaManager.self.getArea(areaKey);

      if (area != null) {
         foreach (PlantableTreeInstanceData d in data) {
            updatePlantableTrees(d.id, area, d);
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
   public void playerTriesPlanting (BodyEntity player, Vector2 worldPos) {
      Area area = AreaManager.self.getArea(player.areaKey);

      if (area != null) {
         Vector2 at = area.transform.InverseTransformPoint(worldPos);
         if (canPlayerPlant(player, player.areaKey, at)) {
            player.rpc.Cmd_PlantTree(at);
         }
      }
   }

   private bool canPlayerPlant (BodyEntity player, string areaKey, Vector2 position) {
      // Check that we have information about this area
      Area area = AreaManager.self.getArea(areaKey);
      if (area == null) {
         D.warning($"Can't plant tree in { areaKey} because missing area data!");
         return false;
      }

      // Check that it is a farm and it belongs to this user
      if (!AreaManager.self.isFarmOfUser(areaKey, player.userId)) {
         return false;
      }

      // Check if player has a seedbag equiped
      WeaponStatData equipedData = EquipmentXMLManager.self.getWeaponData(player.weaponManager.equipmentDataId);
      if (equipedData == null || equipedData.actionType != Weapon.ActionType.PlantCrop) {
         return false;
      }

      // Check that we can assosiate a tree definition with this seedbag
      int seedBagId = equipedData.sqlId;
      PlantableTreeDefinition treeDefinition = null;
      foreach (PlantableTreeDefinition def in _treeDefinitions.Values) {
         if (def.seedBagId == seedBagId) {
            treeDefinition = def;
            break;
         }
      }
      if (treeDefinition == null) {
         return false;
      }

      // Make sure that it is a farm map and this particular farm belongs to the user
      if (!AreaManager.self.isFarmOfUser(areaKey, player.userId)) {
         return false;
      }

      // Make sure exact spot is not occupied and that there is a bit of space around it
      int count = Physics2D.OverlapCircle(
         area.transform.TransformPoint(position),
         0.16f,
         new ContactFilter2D { layerMask = LayerMask.NameToLayer("PlayerBipeds") },
         _colliderBuffer);

      for (int i = 0; i < count; i++) {
         if (!_colliderBuffer[i].isTrigger) {
            return false;
         }
      }

      return true;
   }

   [Server]
   public void plantTree (BodyEntity planter, string areaKey, Vector2 position) {
      if (!canPlayerPlant(planter, areaKey, position)) {
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
         position = position,
         treeDefinitionId = treeDefinition.id,
         planterUserId = planter.userId,
         state = PlantableTreeInstanceData.StateType.Planted,
         lastUpdateTime = TimeManager.self.getLastServerUnixTimestamp()
      };

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Remove a seed from the bag
         bool success = DB_Main.decreaseQuantityOrDeleteItem(planter.userId, seedBagInstanceId, 1);

         // Stop the process if there were not enough seeds
         if (!success) {
            return;
         }

         DB_Main.exec((cmd) => DB_Main.createPlantableTreeInstance(cmd, data));

         // If this was the last seed of an equipped bag, unequip it
         Item seedBag = DB_Main.getItem(planter.userId, seedBagInstanceId);
         if (seedBag == null || seedBag.count == 0) {
            planter.rpc.Bkg_RequestSetWeaponId(0);
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Check that area still exists (might be destroyed, that's fine, changes are stored in the database)
            if (area != null) {
               // Notify clients to update their trees
               planter.rpc.Rpc_UpdatePlantableTrees(data.id, areaKey, data);
               updatePlantableTrees(data.id, area, data);
            }
         });
      });
   }

   [Client]
   public void playerClickedTree (int treeId) {
      if (canPlayerTetherUntether(Global.player, treeId, true)) {
         Global.player.rpc.Cmd_TetherUntetherTree(treeId);
      }
   }

   private bool canPlayerTetherUntether (NetEntity player, int treeId, bool showCantEffects) {
      // Get the tree
      if (!MapManager.self.tryGetPlantableTree(treeId, out PlantableTree tree)) {
         return false;
      }
      if (!_treeDefinitions.TryGetValue(tree.data.treeDefinitionId, out PlantableTreeDefinition treeDef)) {
         return false;
      }

      // Get the area
      Area area = AreaManager.self.getArea(tree.data.areaKey);
      if (area == null) {
         return false;
      }

      // Make sure tree is in a state where it can be tethered/untethered
      if (!treeDef.canTetherUntether(tree.data, TimeManager.self.getLastServerUnixTimestamp())) {
         return false;
      }

      // Make sure player is in range
      if (!Util.distanceLessThan2D(player.sortPoint.transform.position, tree.transform.position, TETHER_DISTANCE)) {
         if (showCantEffects) {
            FloatingCanvas.instantiateAt(tree.transform.position).asTooFar();
         }
         return false;
      }

      return true;
   }

   [Server]
   public void tetherUntetherTree (BodyEntity player, int treeId) {
      if (!canPlayerTetherUntether(player, treeId, false)) {
         return;
      }

      MapManager.self.tryGetPlantableTree(treeId, out PlantableTreeInstanceData treeData);
      _treeDefinitions.TryGetValue(treeData.treeDefinitionId, out PlantableTreeDefinition treeDef);
      Area area = AreaManager.self.getArea(treeData.areaKey);

      // Tether untether tree
      treeDef.tetherUntetherTree(treeData, TimeManager.self.getLastServerUnixTimestamp());

      // Shove the update to the database in the background
      Util.dbBackgroundExec((cmd) => DB_Main.updatePlantableTreeInstance(cmd, treeData));

      // Update the tree and notify user of it
      player.rpc.Rpc_UpdatePlantableTrees(treeId, treeData.areaKey, treeData);
      updatePlantableTrees(treeId, area, treeData);
   }


   [Client]
   public void playerSwungAtTree (NetEntity player, PlantableTree tree) {
      // Called when player wings any item while being close to a tree
      if (canPlayerWater(player, tree)) {
         Global.player.rpc.Cmd_WaterTree(tree.data.id);
      }

      if (canPlayerChop(player, tree)) {
         Global.player.rpc.Cmd_ChopTree(tree.data.id);
         if (!Util.isHost()) {
            tree.receiveChop();
         }
      }
   }

   private bool canPlayerChop (NetEntity player, PlantableTree tree) {
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
      if (!AreaManager.self.isFarmOfUser(tree.data.areaKey, player.userId)) {
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
      tree.receiveChop();

      // Check if the tree has been chopped, destroy if so
      if (tree.currentChopCount >= 3) {
         // Delete the tree from db
         Util.dbBackgroundExec((cmd) => DB_Main.deletePlantableTreeInstance(cmd, treeId));

         // Destroy the tree in server and client
         player.rpc.Rpc_UpdatePlantableTrees(treeId, treeData.areaKey, null);
         updatePlantableTrees(treeId, area, null);

         // Drop resources
         // TODO
      } else {
         player.rpc.Rpc_ReceiveChopTreeVisual(treeId);
      }
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
      updatePlantableTrees(treeId, area, treeData);
   }

   #region Private Variables

   // The types of trees we can plant
   private Dictionary<int, PlantableTreeDefinition> _treeDefinitions = new Dictionary<int, PlantableTreeDefinition>();

   // In case we receive tree data before area is created, store it here and wait
   private LinkedList<PlantableTreeInstanceData> _queuedTreeData = new LinkedList<PlantableTreeInstanceData>();

   // Buffer for checking collider overlap
   private Collider2D[] _colliderBuffer = new Collider2D[20];

   #endregion
}
