using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MinimapCreator : EditorWindow {
   #region Public Variables

   #endregion

   [MenuItem("Util/Create Minimaps")]
   public static void createMinimaps () {
      // Loop through all of our assets
      foreach (string assetPath in AssetDatabase.GetAllAssetPaths()) {
         // We only care about our map assets
         if (!assetPath.StartsWith("Assets/Prefabs/Maps/")) {
            continue;
         }

         // TESTING: just look at the farm map for now
         if (!assetPath.Contains("Farm")) {
            continue;
         }

         // Get the Area associated with the Map
         Area area = AssetDatabase.LoadAssetAtPath<Area>(assetPath);

         // Create a blank texture we can write to
         Texture2D map = new Texture2D(64, 64);

         // Get the prefab
         /*GameObject mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
         GameObject mapInstance = (GameObject) PrefabUtility.InstantiatePrefab(mapPrefab);

         // Temporarily switch from outline to polygon collider mode
         CompositeCollider2D compositeCollider = mapInstance.GetComponentInChildren<CompositeCollider2D>();
         compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
         compositeCollider.GenerateGeometry();

         // Note any spots that are blocked by collision
         for (int y = 0; y < 64; y++) {
            for (int x = 0; x < 64; x++) {
               Vector3 pos = mapPrefab.transform.position + new Vector3(x * .16f, y * -.16f);
               if (compositeCollider.OverlapPoint(pos)) {
                  map.SetPixel(x, 64 - y, Color.green);
               }
            }
         }

         // Now we can get rid of the prefab instance
         Object.Destroy(mapInstance);*/

         // Locate the tilemaps within the area
         foreach (Tilemap tilemap in area.GetComponentsInChildren<Tilemap>()) {
            // Cycle over all the Tile positions in this Tilemap layer
            for (int y = 0; y < 64; y++) {
               for (int x = 0; x < 64; x++) {
                  // Check which Tile is at the cell position
                  Vector3Int cellPos = new Vector3Int(x, -y, 0);
                  TileBase tile = tilemap.GetTile(cellPos);

                  if (tile != null) {
                     // Depending on which layer the tile is in, color the minimap differently
                     if (tilemap.name.EndsWith("Base Ground Layer")) {
                        // map.SetPixel(x, 64 - y, Color.grey);
                     } else if (tilemap.name.Contains("Mountains")) {
                        map.SetPixel(x, 64 - y, Util.getColor(114, 74, 10));
                     } else if (tilemap.name.Contains("Water")) {
                        map.SetPixel(x, 64 - y, Color.blue);
                     } else if (tilemap.name.Contains("Shrubs")) {
                        map.SetPixel(x, 64 - y, Color.green);
                     } else if (tilemap.name.Contains("Props")) {
                        // map.SetPixel(x, 64 - y, Color.white);
                     } else if (tilemap.name.Contains("Stairs") || tilemap.name.Contains("Bridge")) {
                        map.SetPixel(x, 64 - y, Color.black);
                     }

                     // Check the collider type for this tile
                     Tile.ColliderType colliderType = tilemap.GetColliderType(cellPos);

                     // Debug.Log("Tile name: " + tile.name);
                     if (WaterChecker.getAllWaterTiles().Contains(tile.name)) {
                        // map.SetPixel(x, 64 - y, Color.blue);
                     } else if (colliderType != Tile.ColliderType.None) {
                        // map.SetPixel(x, 64 - y, Color.black);
                     }
                  }
               }
            }

            // Apply the texture changes
            map.Apply();

            // Write the Texture into a PNG file in the project assets
            byte[] mapPng = map.EncodeToPNG();
            string path = Application.dataPath + "/Sprites/Minimaps/" + area.areaKey + ".png";
            File.WriteAllBytes(path, mapPng);
            AssetDatabase.Refresh();
         }
      }
   }

   #region Private Variables

   #endregion
}
