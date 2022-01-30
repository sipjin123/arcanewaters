using System;
using System.Collections.Generic;
using SubjectNerd.Utilities;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class AssetSerializationMaps : MonoBehaviour
   {
      private const int PrefabGroupSlots = 100;

      public BiomeMapsDefinition[] mapsDefinitions;
      public MapsDefinition allBiomesDefinition;
      public TileBase transparentTile;
      public float layerZMultiplier = -0.01f;
      public float sublayerZMultiplier = -0.0001f;
      public float layerZStart = 1f;
      public MapTemplate _mapTemplate;
      public Tilemap _tilemapTemplate;
      public Tilemap _collisionTilemapTemplate;
      public MapChunk _collisionTilemapChunkTemplate;
      public GameObject _stairsEffector;
      public Vines _vinesTrigger;
      public Ledge _ledgePrefab;
      public GameObject _currentEffector;
      public GameObject _deletedPrefabMarker;

      // The tile attribute matrix
      [SerializeField, HideInInspector]
      public TileAttributesMatrix _tileAttributeMatrix = new TileAttributesMatrix();

      public TileAttributesMatrix tileAttributeMatrixEditor
      {
         get { return _tileAttributeMatrix; }
      }

      [ExecuteInEditMode]
      private void OnValidate () {
         loadLocal();
      }

      private void loadLocal () {
         try {
            biomeSpecific = new Dictionary<Biome.Type, BiomeMaps>();
            tileAttributeMatrix = _tileAttributeMatrix;
            transparentTileBase = transparentTile;
            layerZMultip = layerZMultiplier;
            sublayerZMultip = sublayerZMultiplier;
            layerZFirst = layerZStart;
            tilemapTemplate = _tilemapTemplate;
            mapTemplate = _mapTemplate;
            collisionTilemapTemplate = _collisionTilemapTemplate;
            collisionTilemapChunkTemplate = _collisionTilemapChunkTemplate;
            stairsEffector = _stairsEffector;
            vinesTrigger = _vinesTrigger;
            currentEffector = _currentEffector;
            deletedPrefabMarker = _deletedPrefabMarker;
            ledgePrefab = _ledgePrefab;

            foreach (BiomeMapsDefinition definition in mapsDefinitions) {
               BiomeMaps bm = new BiomeMaps();
               biomeSpecific.Add(definition.biome, bm);

               //Map prefabs
               foreach (var group in definition.prefabGroups) {
                  for (int i = 0; i < group.toIndexPrefabs.Length; i++) {
                     if (!bm.prefabToIndex.ContainsKey(group.toIndexPrefabs[i]) || group.toIndexPrefabs[i] != deletedPrefabMarker) {
                        bm.prefabToIndex.Add(group.toIndexPrefabs[i], group.index * PrefabGroupSlots + i);
                     }
                     bm.indexToEditorPrefab.Add(group.index * PrefabGroupSlots + i, group.toIndexPrefabs[i]);
                  }

                  for (int i = 0; i < group.fromIndexPrefabs.Length; i++)
                     bm.indexToPrefab.Add(group.index * PrefabGroupSlots + i, group.fromIndexPrefabs[i]);
               }

               //Map tiles
               for (int i = 0; i < definition.tilemap.size.x; i++) {
                  for (int j = 0; j < definition.tilemap.size.y; j++) {
                     var tile = definition.tilemap.GetTile(new Vector3Int(i, j, 0) + definition.tilemap.origin);
                     if (tile != null) {
                        if (!bm.tileToIndex.ContainsKey(tile))
                           bm.tileToIndex.Add(tile, new Vector2Int(i, j));
                        if (!bm.indexToTile.ContainsKey(new Vector2Int(i, j)))
                           bm.indexToTile.Add(new Vector2Int(i, j), tile);
                     }
                  }
               }

               //Map special tiles, that are not included in biome specific definitions
               bm.tileToIndex.Add(transparentTile, new Vector2Int(1000, 1000));
               bm.indexToTile.Add(new Vector2Int(1000, 1000), transparentTile);
            }
            //Handle shared for all biomes
            allBiomes = new BiomeMaps();
            for (int i = 0; i < allBiomesDefinition.tilemap.size.x; i++) {
               for (int j = 0; j < allBiomesDefinition.tilemap.size.y; j++) {
                  var tile = allBiomesDefinition.tilemap.GetTile(new Vector3Int(i, j, 0) + allBiomesDefinition.tilemap.origin);
                  if (tile != null) {
                     if (!allBiomes.tileToIndex.ContainsKey(tile))
                        allBiomes.tileToIndex.Add(tile, new Vector2Int(i, j));
                     if (!allBiomes.indexToTile.ContainsKey(new Vector2Int(i, j)))
                        allBiomes.indexToTile.Add(new Vector2Int(i, j), tile);
                  }
               }
            }

            foreach (var group in allBiomesDefinition.prefabGroups) {
               for (int i = 0; i < group.toIndexPrefabs.Length; i++) {
                  if (!allBiomes.prefabToIndex.ContainsKey(group.toIndexPrefabs[i]) || group.toIndexPrefabs[i] != deletedPrefabMarker) {
                     allBiomes.prefabToIndex.Add(group.toIndexPrefabs[i], group.index * PrefabGroupSlots + i);
                  }
                  allBiomes.indexToEditorPrefab.Add(group.index * PrefabGroupSlots + i, group.toIndexPrefabs[i]);
               }

               for (int i = 0; i < group.fromIndexPrefabs.Length; i++)
                  allBiomes.indexToPrefab.Add(group.index * PrefabGroupSlots + i, group.fromIndexPrefabs[i]);
            }


            loaded = true;
         } catch (Exception ex) {
            biomeSpecific = null;
            Debug.LogError("Failed loading asset serialization maps");
            throw ex;
         }
      }

      public static void ensureLoaded () {
         if (!loaded) {
            load();
         }
      }

      public static void load () {
         var asm = Resources.Load<AssetSerializationMaps>("AssetSerializationMaps");
         if (asm == null)
            throw new System.MissingMemberException("Missing asset serialization maps");
         asm.loadLocal();

      }

      public static TileBase getTile (Vector2Int index, Biome.Type biome) {
         if (allBiomes.indexToTile.TryGetValue(index, out TileBase tile))
            return tile;
         return biomeSpecific[biome].indexToTile[index];
      }

      public static TileBase tryGetTile (Vector2Int index, Biome.Type biome) {
         if (allBiomes.indexToTile.TryGetValue(index, out TileBase t)) {
            return t;
         }
         if (biomeSpecific[biome].indexToTile.TryGetValue(index, out TileBase bt)) {
            return bt;
         }

         return null;
      }

      public static Vector2Int getIndex (TileBase tile, Biome.Type biome) {
         try {
            if (allBiomes.tileToIndex.TryGetValue(tile, out Vector2Int index))
               return index;
            return biomeSpecific[biome].tileToIndex[tile];
         } catch (Exception ex) {
            Debug.Log($"Unable to get index for tile {tile.name}");
            throw ex;
         }

      }

      public static GameObject getPrefab (int index, Biome.Type biome, bool editorPrefab) {
         if (editorPrefab) {
            if (allBiomes.indexToEditorPrefab.TryGetValue(index, out GameObject prefab))
               return prefab;
            return biomeSpecific[biome].indexToEditorPrefab[index];
         } else {
            if (allBiomes.indexToPrefab.TryGetValue(index, out GameObject prefab))
               return prefab;
            return biomeSpecific[biome].indexToPrefab[index];
         }
      }

      public static GameObject tryGetPrefabGame (int index, Biome.Type biome) {
         if (allBiomes.indexToPrefab.TryGetValue(index, out GameObject prefab))
            return prefab;
         if (biomeSpecific[biome].indexToPrefab.TryGetValue(index, out GameObject bPrefab)) {
            return bPrefab;
         }
         return null;
      }

      public static int getIndex (GameObject prefab, Biome.Type biome) {
         if (allBiomes.prefabToIndex.TryGetValue(prefab, out int index))
            return index;
         return biomeSpecific[biome].prefabToIndex[prefab];
      }

      public static Dictionary<Biome.Type, BiomeMaps> biomeSpecific { get; set; }
      public static BiomeMaps allBiomes { get; set; }
      public static TileAttributesMatrix tileAttributeMatrix { get; set; }
      public static TileBase transparentTileBase { get; set; }
      public static float layerZMultip { get; set; }
      public static float sublayerZMultip { get; set; }
      public static float layerZFirst { get; set; }
      public static bool loaded { get; private set; }
      public static MapTemplate mapTemplate { get; private set; }
      public static Tilemap tilemapTemplate { get; private set; }
      public static Tilemap collisionTilemapTemplate { get; private set; }
      public static MapChunk collisionTilemapChunkTemplate { get; private set; }
      public static GameObject stairsEffector { get; private set; }
      public static Vines vinesTrigger { get; private set; }
      public static GameObject currentEffector { get; private set; }
      public static GameObject deletedPrefabMarker { get; private set; }
      public static Ledge ledgePrefab { get; private set; }

      public class BiomeMaps
      {
         public Dictionary<Vector2Int, TileBase> indexToTile { get; set; }
         public Dictionary<int, GameObject> indexToPrefab { get; set; }
         public Dictionary<int, GameObject> indexToEditorPrefab { get; set; }
         public Dictionary<TileBase, Vector2Int> tileToIndex { get; set; }
         public Dictionary<GameObject, int> prefabToIndex { get; set; }

         public BiomeMaps () {
            indexToTile = new Dictionary<Vector2Int, TileBase>();
            indexToPrefab = new Dictionary<int, GameObject>();
            indexToEditorPrefab = new Dictionary<int, GameObject>();
            tileToIndex = new Dictionary<TileBase, Vector2Int>();
            prefabToIndex = new Dictionary<GameObject, int>();
         }
      }

      [Serializable]
      public class MapsDefinition
      {
         public Tilemap tilemap;
         public PrefabGroup[] prefabGroups;
      }

      [Serializable]
      public class BiomeMapsDefinition : MapsDefinition
      {
         public Biome.Type biome;
      }

      [Serializable]
      public class PrefabGroup
      {
         public string title;
         public int index;
         public GameObject[] toIndexPrefabs;
         public GameObject[] fromIndexPrefabs;
      }
   }
}
