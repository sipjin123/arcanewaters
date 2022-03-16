using UnityEngine;
using System.Collections.Generic;
using System;
public class WorldMapPanelCloudsContainer : MonoBehaviour
{
   #region Public Variables

   // Cloud Prefab
   public GameObject cloudPrefab;

   // Should the cloud around the edge the container be flushed with the border?
   public bool flushCloudsWithEdge = true;

   // Default cloud sprite
   public int defaultCloudSpriteNumber = 0;

   #endregion

   public void fill () {
      clearClouds();

      for (int row = 0; row < WorldMapPanel.self.mapDimensions.y; row++) {
         for (int col = 0; col < WorldMapPanel.self.mapDimensions.x; col++) {
            createCloud(new WorldMapPanelAreaCoords(col, row));
         }
      }

      assignCloudConfigurations();
      textureAllClouds();
   }

   public void hideClouds (List<WorldMapPanelAreaCoords> coordsList) {
      foreach (WorldMapPanelAreaCoords coords in coordsList) {
         if (findCloud(coords, out WorldMapPanelCloud cloud)) {
            cloud.toggle(false);
         }
      }

      assignCloudConfigurations();
      textureAllClouds();
   }

   public void clearClouds () {
      foreach (KeyValuePair<WorldMapPanelAreaCoords, WorldMapPanelCloud> pair in _clouds) {
         Destroy(pair.Value.gameObject);
      }

      _clouds.Clear();
   }

   public static WorldMapPanelAreaCoords computeNextCoords (WorldMapPanelAreaCoords coords, Direction direction) {
      WorldMapPanelAreaCoords computedCoords = new WorldMapPanelAreaCoords();

      switch (direction) {
         case Direction.North:
            computedCoords = new WorldMapPanelAreaCoords(coords.x, coords.y - 1);
            break;
         case Direction.NorthEast:
            computedCoords = new WorldMapPanelAreaCoords(coords.x + 1, coords.y - 1);
            break;
         case Direction.East:
            computedCoords = new WorldMapPanelAreaCoords(coords.x + 1, coords.y);
            break;
         case Direction.SouthEast:
            computedCoords = new WorldMapPanelAreaCoords(coords.x + 1, coords.y + 1);
            break;
         case Direction.South:
            computedCoords = new WorldMapPanelAreaCoords(coords.x, coords.y + 1);
            break;
         case Direction.SouthWest:
            computedCoords = new WorldMapPanelAreaCoords(coords.x - 1, coords.y + 1);
            break;
         case Direction.West:
            computedCoords = new WorldMapPanelAreaCoords(coords.x - 1, coords.y);
            break;
         case Direction.NorthWest:
            computedCoords = new WorldMapPanelAreaCoords(coords.x - 1, coords.y - 1);
            break;
      }

      return computedCoords;
   }

   public WorldMapPanelCloud findCloudInDirection (WorldMapPanelCloud cloud, Direction direction) {
      WorldMapPanelAreaCoords computedCoords = computeNextCoords(cloud.coords, direction);
      if (_clouds.TryGetValue(computedCoords, out WorldMapPanelCloud foundCloud)) {
         return foundCloud;
      }

      return null;
   }

   public void findAdjacentClouds (WorldMapPanelCloud cloud, ref Dictionary<Direction, WorldMapPanelCloud> adjacentClouds) {
      adjacentClouds.Clear();
      var allDirections = Enum.GetValues(typeof(Direction));

      foreach (Direction direction in allDirections) {
         WorldMapPanelCloud foundCloud = findCloudInDirection(cloud, direction);

         if (foundCloud != null) {
            adjacentClouds.Add(direction, foundCloud);
         }
      }
   }

   private WorldMapPanelCloud createCloud (WorldMapPanelAreaCoords coords) {
      GameObject cloudGo = Instantiate(cloudPrefab);
      WorldMapPanelCloud cloud = cloudGo.GetComponent<WorldMapPanelCloud>();
      cloud.coords = coords;
      cloud.toggle(true);
      parentCloud(cloud);
      positionCloud(cloud);
      resizeCloud(cloud);
      registerCloud(cloud);

      return cloud;
   }

   private bool findCloud (WorldMapPanelAreaCoords coords, out WorldMapPanelCloud cloud) {
      if (_clouds.TryGetValue(coords, out WorldMapPanelCloud c)) {
         cloud = c;
         return true;
      }

      cloud = null;
      return false;
   }

   private bool registerCloud (WorldMapPanelCloud cloud) {
      if (_clouds.ContainsKey(cloud.coords)) {
         return false;
      }

      _clouds.Add(cloud.coords, cloud);
      cloud.gameObject.name = "WorldMapPanelCloud_" + cloud.coords.ToString();
      return true;
   }

   private bool positionCloud (WorldMapPanelCloud cloud) {
      RectTransform rectTransform = cloud.transform.GetComponent<RectTransform>();

      if (rectTransform == null) {
         return false;
      }

      rectTransform.localPosition = new Vector3(cloud.coords.x * WorldMapPanel.self.cellSize.x, -cloud.coords.y * WorldMapPanel.self.cellSize.y, 0);
      return true;
   }

   private void parentCloud (WorldMapPanelCloud cloud) {
      cloud.transform.SetParent(transform);
   }

   private void textureAllClouds () {
      foreach (WorldMapPanelCloud cloud in _clouds.Values) {
         textureCloud(cloud, computeSpriteNumberForCloud(cloud));
      }
   }

   private void textureCloud (WorldMapPanelCloud cloud, int spriteNumber) {
      Sprite cloudSprite = WorldMapPanel.self.cloudSpriteResolver.getCloudSprite(spriteNumber);
      Sprite cloudShadowSprite = WorldMapPanel.self.cloudSpriteResolver.getCloudShadowSprite(spriteNumber);

      cloud.setCloudSprite(cloudSprite);
      cloud.setShadowSprite(cloudShadowSprite);
   }

   private bool resizeCloud (WorldMapPanelCloud cloud) {
      RectTransform rectTransform = cloud.GetComponent<RectTransform>();

      if (rectTransform == null) {
         return false;
      }

      rectTransform.sizeDelta = new Vector2(WorldMapPanel.self.cellSize.x, WorldMapPanel.self.cellSize.y);
      rectTransform.localScale = Vector3.one;
      return true;
   }

   private int computeSpriteNumberForCloud (WorldMapPanelCloud cloud) {
      return WorldMapPanel.self.cloudSpriteResolver.getCloudSpriteNumberByConfiguration(cloud.configuration);
   }

   private WorldMapPanelCloud.CloudConfiguration computeCloudConfiguration (Dictionary<Direction, WorldMapPanelCloud> adjacentClouds) {
      WorldMapPanelCloud.CloudConfiguration cloudConfiguration = WorldMapPanelCloud.CloudConfiguration.None;

      foreach (KeyValuePair<Direction, WorldMapPanelCloud> cloudPair in adjacentClouds) {
         // Ignore hidden clouds
         if (!cloudPair.Value.isVisible()) {
            continue;
         }

         switch (cloudPair.Key) {
            case Direction.North:
               cloudConfiguration |= WorldMapPanelCloud.CloudConfiguration.North;
               break;
            case Direction.NorthEast:
               cloudConfiguration |= WorldMapPanelCloud.CloudConfiguration.NorthEast;
               break;
            case Direction.East:
               cloudConfiguration |= WorldMapPanelCloud.CloudConfiguration.East;
               break;
            case Direction.SouthEast:
               cloudConfiguration |= WorldMapPanelCloud.CloudConfiguration.SouthEast;
               break;
            case Direction.South:
               cloudConfiguration |= WorldMapPanelCloud.CloudConfiguration.South;
               break;
            case Direction.SouthWest:
               cloudConfiguration |= WorldMapPanelCloud.CloudConfiguration.SouthWest;
               break;
            case Direction.West:
               cloudConfiguration |= WorldMapPanelCloud.CloudConfiguration.West;
               break;
            case Direction.NorthWest:
               cloudConfiguration |= WorldMapPanelCloud.CloudConfiguration.NorthWest;
               break;
         }
      }

      return cloudConfiguration;
   }

   private void assignCloudConfigurations () {
      Dictionary<Direction, WorldMapPanelCloud> adjacentClouds = new Dictionary<Direction, WorldMapPanelCloud>();

      foreach (WorldMapPanelCloud cloud in _clouds.Values) {
         findAdjacentClouds(cloud, ref adjacentClouds);
         cloud.configuration = computeCloudConfiguration(adjacentClouds);

         // Additional adjustments for the clouds on the edge
         if (flushCloudsWithEdge && isCloudOnEdge(cloud)) {
            if (cloud.coords.x == 0) {
               cloud.configuration |= WorldMapPanelCloud.CloudConfiguration.West | WorldMapPanelCloud.CloudConfiguration.SouthWest | WorldMapPanelCloud.CloudConfiguration.NorthWest;
            } else if (cloud.coords.x == WorldMapPanel.self.mapDimensions.x - 1) {
               cloud.configuration |= WorldMapPanelCloud.CloudConfiguration.East | WorldMapPanelCloud.CloudConfiguration.NorthEast | WorldMapPanelCloud.CloudConfiguration.SouthEast;
            }

            if (cloud.coords.y == 0) {
               cloud.configuration |= WorldMapPanelCloud.CloudConfiguration.North | WorldMapPanelCloud.CloudConfiguration.NorthEast | WorldMapPanelCloud.CloudConfiguration.NorthWest;
            } else if (cloud.coords.y == WorldMapPanel.self.mapDimensions.y - 1) {
               cloud.configuration |= WorldMapPanelCloud.CloudConfiguration.South | WorldMapPanelCloud.CloudConfiguration.SouthEast | WorldMapPanelCloud.CloudConfiguration.SouthWest;
            }
         }
      }
   }

   private bool isCloudOnEdge (WorldMapPanelCloud cloud) {
      return (cloud.coords.x == 0 || cloud.coords.x + 1 == WorldMapPanel.self.mapDimensions.x || cloud.coords.y == 0 || cloud.coords.y + 1 == WorldMapPanel.self.mapDimensions.y);
   }

   #region Private Variables

   // Registry of clouds
   private Dictionary<WorldMapPanelAreaCoords, WorldMapPanelCloud> _clouds = new Dictionary<WorldMapPanelAreaCoords, WorldMapPanelCloud>();

   #endregion
}
