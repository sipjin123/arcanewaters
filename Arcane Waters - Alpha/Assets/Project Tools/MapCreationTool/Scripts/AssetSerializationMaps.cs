using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
    public class AssetSerializationMaps : MonoBehaviour
    {
        public BiomeMapsDefinition[] mapsDefinitions;
        public MapsDefinition allBiomesDefinition;
        public TileBase transparentTile;
        public float layerZMultiplier = -0.01f;
        public float sublayerZMultiplier = -0.0001f;

        private void OnValidate()
        {
            LoadLocal();
        }

        private void LoadLocal()
        {
            try
            {
                BiomeSpecific = new Dictionary<BiomeType, BiomeMaps>();
                TransparentTile = transparentTile;
                LayerZMultiplier = layerZMultiplier;
                SublayerZMultiplier = sublayerZMultiplier;

                foreach (BiomeMapsDefinition definition in mapsDefinitions)
                {
                    BiomeMaps bm = new BiomeMaps();
                    BiomeSpecific.Add(definition.biome, bm);

                    //Map prefabs
                    for (int i = 0; i < definition.prefabs.Length; i++)
                    {
                        bm.PrefabToIndex.Add(definition.prefabs[i], i);
                        bm.IndexToPrefab.Add(i, definition.prefabs[i]);
                    }

                    //Map tiles
                    for (int i = 0; i < definition.tilemap.size.x; i++)
                    {
                        for (int j = 0; j < definition.tilemap.size.y; j++)
                        {
                            var tile = definition.tilemap.GetTile(new Vector3Int(i, j, 0) + definition.tilemap.origin);
                            if (tile != null)
                            {
                                bm.TileToIndex.Add(tile, new Vector2Int(i, j));
                                bm.IndexToTile.Add(new Vector2Int(i, j), tile);
                            }
                        }
                    }

                    //Map special tiles, that are not included in biome specific definitions
                    bm.TileToIndex.Add(transparentTile, new Vector2Int(1000, 1000));
                    bm.IndexToTile.Add(new Vector2Int(1000, 1000), transparentTile);
                }
                //Handle shared for all biomes
                AllBiomes = new BiomeMaps();
                for (int i = 0; i < allBiomesDefinition.tilemap.size.x; i++)
                {
                    for (int j = 0; j < allBiomesDefinition.tilemap.size.y; j++)
                    {
                        var tile = allBiomesDefinition.tilemap.GetTile(new Vector3Int(i, j, 0) + allBiomesDefinition.tilemap.origin);
                        if (tile != null)
                        {
                            AllBiomes.TileToIndex.Add(tile, new Vector2Int(i, j));
                            AllBiomes.IndexToTile.Add(new Vector2Int(i, j), tile);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                BiomeSpecific = null;
                Debug.LogError("Failed loading asset serialization maps");
                throw ex;
            }
        }

        public static void Load()
        {
            var asm = Resources.Load<AssetSerializationMaps>("AssetSerializationMaps");
            if (asm == null)
                throw new System.MissingMemberException("Missing asset serialization maps");
            asm.LoadLocal();

        }

        public static TileBase GetTile(Vector2Int index, BiomeType biome)
        {
            if (AllBiomes.IndexToTile.TryGetValue(index, out TileBase tile))
                return tile;
            return BiomeSpecific[biome].IndexToTile[index];
        }

        public static Vector2Int GetIndex(TileBase tile, BiomeType biome)
        {
            if (AllBiomes.TileToIndex.TryGetValue(tile, out Vector2Int index))
                return index;
            return BiomeSpecific[biome].TileToIndex[tile];
        }

        public static GameObject GetPrefab(int index, BiomeType biome)
        {
            if (AllBiomes.IndexToPrefab.TryGetValue(index, out GameObject prefab))
                return prefab;
            return BiomeSpecific[biome].IndexToPrefab[index];
        }

        public static int GetIndex(GameObject prefab, BiomeType biome)
        {
            if (AllBiomes.PrefabToIndex.TryGetValue(prefab, out int index))
                return index;
            return BiomeSpecific[biome].PrefabToIndex[prefab];
        }

        public static Dictionary<BiomeType, BiomeMaps> BiomeSpecific { get; set; }
        public static BiomeMaps AllBiomes { get; set; }
        public static TileBase TransparentTile { get; set; }
        public static float LayerZMultiplier { get; set; }
        public static float SublayerZMultiplier { get; set; }

        public class BiomeMaps
        {
            public Dictionary<Vector2Int, TileBase> IndexToTile { get; set; }
            public Dictionary<int, GameObject> IndexToPrefab { get; set; }
            public Dictionary<TileBase, Vector2Int> TileToIndex { get; set; }
            public Dictionary<GameObject, int> PrefabToIndex { get; set; }

            public BiomeMaps()
            {
                IndexToTile = new Dictionary<Vector2Int, TileBase>();
                IndexToPrefab = new Dictionary<int, GameObject>();
                TileToIndex = new Dictionary<TileBase, Vector2Int>();
                PrefabToIndex = new Dictionary<GameObject, int>();
            }
        }

        [Serializable]
        public class MapsDefinition
        {
            public Tilemap tilemap;
            public GameObject[] prefabs;
        }

        [Serializable]
        public class BiomeMapsDefinition : MapsDefinition
        {
            public BiomeType biome;
            
        }
    }
}
