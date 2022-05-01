using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class OutpostUtil
{
   #region Public Variables

   // What's the minimum amount of distance between outposts
   public const float MIN_DIST_BETWEEN_OUTPOST = 10f;

   // Distance between dock and outpost buildings
   public const float DOCK_TO_BUILDING_DISTANCE = 0.48f;

   // Reasons why player may not be able to build an outpost
   public enum CantBuildReason
   {
      None = 0,
      NotEnoughResources = 1,
      NotShore = 2,
      Obstructed = 3,
      TooCloseToOutpost = 4,
      NotPrimaryInstance = 5,
      IncorrectTilesBlocking = 6
   }

   #endregion

   public static bool canBuildOutpostAnywhere (NetEntity entity, out Vector3 buildPosition,
      out CantBuildReason cantBuildReason, out bool foundPosition, out Direction outpostDirection) {
      buildPosition = Vector3.zero;
      foundPosition = false;
      outpostDirection = Direction.North;

      if (entity == null) {
         cantBuildReason = CantBuildReason.None;
         return false;
      }

      if (entity.guildId == 0) {
         cantBuildReason = CantBuildReason.None;
         return false;
      }

      // Make sure the area exists
      if (!AreaManager.self.tryGetArea(entity.areaKey, out Area area)) {
         cantBuildReason = CantBuildReason.None;
         return false;
      }

      // Entity must be a player ship
      if (!(entity is PlayerShipEntity)) {
         cantBuildReason = CantBuildReason.None;
         return false;
      }

      // Get the instance of the entity
      if (!InstanceManager.self.tryGetInstance(entity.instanceId, out Instance instance)) {
         cantBuildReason = CantBuildReason.None;
         return false;
      }

      // Make sure instance is the primary one
      if (!InstanceManager.self.isPrimaryInstance(instance)) {
         cantBuildReason = CantBuildReason.NotPrimaryInstance;
         return false;
      }

      // Make sure instance is open world
      if (!WorldMapManager.self.isWorldMapArea(instance.areaKey)) {
         cantBuildReason = CantBuildReason.None;
         return false;
      }

      // Find closest point to which we could build
      if (!area.closestTileWithAnyOfAttribute(_outpostSnapTiles,
         entity.transform.position, new Vector2Int(12, 12), out buildPosition, out TileAttributes.Type tile)) {
         cantBuildReason = CantBuildReason.NotShore;
         return false;
      }

      // Make sure we can connect a tile attribute with a direction
      if (!_outpostDirections.TryGetValue(tile, out Direction dir)) {
         cantBuildReason = CantBuildReason.NotShore;
         return false;
      }

      outpostDirection = dir;
      foundPosition = true;
      return canBuildOutpostAt(entity, buildPosition, outpostDirection, out cantBuildReason);
   }

   public static bool canBuildOutpostAt (NetEntity entity, Vector3 buildPosition, Direction outpostDirection,
      out CantBuildReason cantBuildReason) {
      // Entity must be a player ship
      if (!(entity is PlayerShipEntity)) {
         cantBuildReason = CantBuildReason.None;
         return false;
      }

      if (entity.guildId == 0) {
         cantBuildReason = CantBuildReason.None;
         return false;
      }

      // Get the instance of the entity
      if (!InstanceManager.self.tryGetInstance(entity.instanceId, out Instance instance)) {
         cantBuildReason = CantBuildReason.None;
         return false;
      }

      // Make sure the area exists
      if (!AreaManager.self.tryGetArea(instance.areaKey, out Area area)) {
         cantBuildReason = CantBuildReason.None;
         return false;
      }

      // Make sure instance is the primary one
      if (!InstanceManager.self.isPrimaryInstance(instance)) {
         cantBuildReason = CantBuildReason.NotPrimaryInstance;
         return false;
      }

      // Make sure instance is open world
      if (!WorldMapManager.self.isWorldMapArea(instance.areaKey)) {
         cantBuildReason = CantBuildReason.None;
         return false;
      }

      // Check if there's an outpost next to the position
      List<SeaStructure> structs = NetworkServer.active
         ? instance.seaStructures
         : Outpost.outpostsClient;
      foreach (SeaStructure struc in structs) {
         if (struc != null && struc is Outpost) {
            if (Util.distanceLessThan2D(struc.transform.position, buildPosition, MIN_DIST_BETWEEN_OUTPOST)) {
               cantBuildReason = CantBuildReason.TooCloseToOutpost;
               return false;
            }
         }
      }

      // Check if there's a tile that can receive an outpost
      bool foundTile = false;
      foreach (TileAttributes.Type t in _outpostSnapTiles) {
         if (area.hasTileAttribute(t, buildPosition)) {
            foundTile = true;
            break;
         }
      }

      if (!foundTile) {
         cantBuildReason = CantBuildReason.NotShore;
         return false;
      }

      // Check that there's an open area to receive the outpost buildings
      Vector2 buildingCorner = (Vector2) buildPosition
         + -Util.getDirectionFromFacing(outpostDirection) * DOCK_TO_BUILDING_DISTANCE
         + new Vector2(-2 * 0.16f, -2 * 0.16f);

      // Check that all tiles underneath the building have an attribute
      for (int i = 0; i < 5; i++) {
         for (int j = 0; j < 5; j++) {
            // Skip corners
            if ((i == 0 || i == 4) && (j == 0 || j == 4)) {
               continue;
            }

            if (!area.hasTileAttribute(TileAttributes.Type.OutpostBaseSpot, buildingCorner + new Vector2(i * 0.16f, j * 0.16f))) {
               cantBuildReason = CantBuildReason.IncorrectTilesBlocking;
               return false;
            }
         }
      }

      // Check that no tiles underneath the building have blocking attribute
      for (int i = 0; i < 5; i++) {
         for (int j = 0; j < 5; j++) {
            // Skip corners
            if ((i == 0 || i == 4) && (j == 0 || j == 4)) {
               continue;
            }

            if (area.hasTileAttribute(TileAttributes.Type.OutpostBasePrevent, buildingCorner + new Vector2(i * 0.16f, j * 0.16f))) {
               cantBuildReason = CantBuildReason.IncorrectTilesBlocking;
               return false;
            }
         }
      }

      cantBuildReason = CantBuildReason.None;
      return true;
   }

   public static void disableTreesAroundOutpost (Area area, Outpost outpost) {
      foreach (SeaTree tree in area.GetComponentsInChildren<SeaTree>()) {
         if (Util.distanceLessThan2D(outpost.buildingsParent.transform.position, tree.transform.position, 0.48f)) {
            tree.gameObject.SetActive(false);
         }
      }
   }

   #region Private Variables

   // Outpost snap positions and their respective outpost directions
   private static Dictionary<TileAttributes.Type, Direction> _outpostDirections =
      new Dictionary<TileAttributes.Type, Direction>() {
         { TileAttributes.Type.OutpostBridgeSnap_S, Direction.South },
         { TileAttributes.Type.OutpostBridgeSnap_N, Direction.North },
         { TileAttributes.Type.OutpostBridgeSnap_W, Direction.West },
         { TileAttributes.Type.OutpostBridgeSnap_E, Direction.East },
         { TileAttributes.Type.OutpostBridgeSnap_SW, Direction.SouthWest },
         { TileAttributes.Type.OutpostBridgeSnap_SE, Direction.SouthEast },
         { TileAttributes.Type.OutpostBridgeSnap_NW, Direction.NorthWest },
         { TileAttributes.Type.OutpostBridgeSnap_NE, Direction.NorthEast }
      };

   // Tile attributes which allow for outpost docks to snap
   private static TileAttributes.Type[] _outpostSnapTiles = new TileAttributes.Type[] {
      TileAttributes.Type.OutpostBridgeSnap_S, TileAttributes.Type.OutpostBridgeSnap_N,
      TileAttributes.Type.OutpostBridgeSnap_W, TileAttributes.Type.OutpostBridgeSnap_E,
      TileAttributes.Type.OutpostBridgeSnap_SW, TileAttributes.Type.OutpostBridgeSnap_SE,
      TileAttributes.Type.OutpostBridgeSnap_NE, TileAttributes.Type.OutpostBridgeSnap_NW
   };

   #endregion
}
