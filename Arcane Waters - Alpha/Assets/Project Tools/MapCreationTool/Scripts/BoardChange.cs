using MapCreationTool.PaletteTilesData;
using MapCreationTool.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
    public class BoardChange
    {
        public List<TileChange> TileChanges { get; set; }
        public List<PrefabChange> PrefabChanges { get; set; }

        public BoardChange()
        {
            TileChanges = new List<TileChange>();
            PrefabChanges = new List<PrefabChange>();
        }

        public bool Empty
        {
            get { return TileChanges.Count == 0 && PrefabChanges.Count == 0; }
        }

        public void Add(BoardChange change)
        {
            TileChanges.AddRange(change.TileChanges);
            PrefabChanges.AddRange(change.PrefabChanges);
        }

        public static BoardChange CalculateClearAllChange(Dictionary<int, Layer> layers, List<PlacedPrefab> prefabs)
        {
            BoardChange result = new BoardChange();

            foreach (Layer layer in layers.Values)
            {
                if (layer.HasTilemap)
                    result.Add(CalculateClearAllChange(layer));
                else
                {
                    foreach (Layer sublayer in layer.SubLayers)
                    {
                        if (sublayer.HasTilemap)
                        {
                            result.Add(CalculateClearAllChange(sublayer));
                        }
                    }
                }
            }

            foreach (PlacedPrefab pref in prefabs)
            {
                result.PrefabChanges.Add(new PrefabChange { PrefabToDestroy = pref.Original, PositionToDestroy = pref.PlacedInstance.transform.position });
            }

            return result;
        }

        public static BoardChange CalculateClearAllChange(Layer layer)
        {
            BoardChange result = new BoardChange();

            for (int i = 0; i < layer.Size.x; i++)
            {
                for (int j = 0; j < layer.Size.y; j++)
                {
                    Vector3Int pos = new Vector3Int(i + layer.Origin.x, j + layer.Origin.y, 0);
                    if (layer.GetTile(pos) != null)
                    {
                        result.TileChanges.Add(new TileChange(null, pos, layer));
                    }
                }
            }

            return result;
        }

        public static BoardChange CalculateDeserializedDataChange(DeserializedProject data, Dictionary<int, Layer> layers, List<PlacedPrefab> prefabs)
        {
            BoardChange result = CalculateClearAllChange(layers, prefabs);

            foreach (var tile in data.tiles)
            {
                Layer layer = layers[tile.layer];
                if (tile.sublayer == null)
                    result.TileChanges.Add(new TileChange(tile.tile, tile.position, layers[tile.layer]));
                else
                {
                    result.TileChanges.Add(new TileChange(tile.tile, tile.position, layers[tile.layer].SubLayers[tile.sublayer.Value]));
                }

            }

            foreach (var prefab in data.prefabs)
            {
                result.PrefabChanges.Add(new PrefabChange
                {
                    PositionToPlace = prefab.position,
                    PrefabToPlace = prefab.prefab
                });
            }

            return result;
        }

        public static BoardChange CalculateEraserChange(
            IEnumerable<Layer> layers,
            List<PlacedPrefab> prefabs,
            EraserLayerMode eraserMode,
            Vector3 worldPos)
        {
            BoardChange result = new BoardChange();

            List<PlacedPrefab> overlapPrefs = new List<PlacedPrefab>();
            foreach (PlacedPrefab placedPrefab in prefabs)
            {
                bool active = placedPrefab.PlacedInstance.activeSelf;
                placedPrefab.PlacedInstance.SetActive(true);
                foreach (Collider2D col in placedPrefab.PlacedInstance.GetComponentsInChildren<Collider2D>(true))
                {
                    if (col.OverlapPoint(worldPos))
                    {
                        overlapPrefs.Add(placedPrefab);
                        break;
                    }
                }
                placedPrefab.PlacedInstance.SetActive(active);
            }

            if (overlapPrefs.Count > 0)
            {
                if (eraserMode == EraserLayerMode.Top)
                {
                    float minZ = overlapPrefs.Min(pp => pp.PlacedInstance.transform.position.z);
                    var prefabToDestroy = overlapPrefs.First(p => p.PlacedInstance.transform.position.z == minZ);

                    result.PrefabChanges.Add(new PrefabChange
                    {
                        PrefabToDestroy = prefabToDestroy.Original,
                        PositionToDestroy = prefabToDestroy.PlacedInstance.transform.position
                    });
                    return result;
                }
                else if (eraserMode == EraserLayerMode.All)
                {
                    result.PrefabChanges.AddRange(overlapPrefs.Select(p => new PrefabChange
                    {
                        PrefabToDestroy = p.Original,
                        PositionToDestroy = p.PlacedInstance.transform.position
                    }));
                }
            }

            foreach (Layer layer in layers.Reverse())
            {
                if (layer.HasTile(layer.WorldToCell(worldPos)))
                {
                    result.TileChanges.Add(new TileChange(null, layer.WorldToCell(worldPos), layer));
                    if (eraserMode == EraserLayerMode.Top)
                        return result;
                }
            }

            return result;
        }

        public static BoardChange CalculateNineSliceInOutchanges(NineSliceInOutGroup group, Layer layer, Vector3 worldPos)
        {
            BoardChange change = new BoardChange();

            Vector3Int targetCenter = layer.WorldToCell(worldPos);

            //Adjacency matrix where the target brush tile is at the center,
            //Tiles of the same group are set to true, null and other tiles false
            int n = 7; //Size of adjacency matrix
            bool[,] adj = new bool[n, n];

            //Fill adjacency matrix according to relavent tile info
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    TileBase tile = layer.GetTile(
                        new Vector3Int(targetCenter.x - (n / 2) + i, targetCenter.y - (n / 2) + j, 0));

                    adj[i, j] = tile != null && group.Contains(tile);
                }
            }

            //Set the tiles that must be placed as true
            adj[3, 3] = adj[3, 4] = adj[4, 3] = adj[4, 4] = true;

            //Determine tiles based on the adjacency matrix
            for (int i = 1; i < n - 1; i++)
            {
                for (int j = 1; j < n - 1; j++)
                {
                    if (adj[i, j])
                    {
                        Vector3Int position = targetCenter + new Vector3Int(i - (n / 2), j - (n / 2), 0);

                        Vector2Int tileIndex = new Vector2Int(1, 1);
                        if (adj[i - 1, j] && !adj[i + 1, j])
                            tileIndex.x = 2;
                        else if (!adj[i - 1, j] && adj[i + 1, j])
                            tileIndex.x = 0;

                        if (adj[i, j - 1] && !adj[i, j + 1])
                            tileIndex.y = 2;
                        else if (!adj[i, j - 1] && adj[i, j + 1])
                            tileIndex.y = 0;

                        //Override rules for when they try to connect with only 1 tile
                        if (adj[i, j + 1] && !adj[i + 1, j] && !adj[i - 1, j + 1])
                            tileIndex.Set(2, 2);
                        else if (adj[i + 1, j] && !adj[i, j + 1] && !adj[i + 1, j - 1])
                            tileIndex.Set(2, 2);
                        else if (adj[i + 1, j] && !adj[i, j - 1] && !adj[i + 1, j + 1])
                            tileIndex.Set(2, 0);
                        else if (adj[i, j - 1] && !adj[i + 1, j] && !adj[i - 1, j - 1])
                            tileIndex.Set(2, 0);
                        else if (adj[i, j - 1] && !adj[i - 1, j] && !adj[i + 1, j - 1])
                            tileIndex.Set(0, 0);
                        else if (adj[i - 1, j] && !adj[i, j - 1] && !adj[i - 1, j + 1])
                            tileIndex.Set(0, 0);
                        else if (adj[i - 1, j] && !adj[i, j + 1] && !adj[i - 1, j - 1])
                            tileIndex.Set(0, 2);
                        else if (adj[i, j + 1] && !adj[i - 1, j] && !adj[i + 1, j + 1])
                            tileIndex.Set(0, 2);

                        if (tileIndex.x == 1 && tileIndex.y == 1)
                        {
                            //Check if all diagonals are occupied
                            if (adj[i - 1, j - 1] && adj[i + 1, j + 1] && adj[i - 1, j + 1] && adj[i + 1, j - 1])
                            {
                                change.TileChanges.Add(new TileChange(group.OuterTiles[tileIndex.x, tileIndex.y].Tile, position, layer));
                            }
                            else
                            {
                                if (!adj[i - 1, j - 1])
                                    change.TileChanges.Add(new TileChange(group.InnerTiles[2, 2].Tile, position, layer));
                                else if (!adj[i + 1, j - 1])
                                    change.TileChanges.Add(new TileChange(group.InnerTiles[0, 2].Tile, position, layer));
                                else if (!adj[i - 1, j + 1])
                                    change.TileChanges.Add(new TileChange(group.InnerTiles[2, 0].Tile, position, layer));
                                else if (!adj[i + 1, j + 1])
                                    change.TileChanges.Add(new TileChange(group.InnerTiles[0, 0].Tile, position, layer));
                            }
                        }
                        else
                            change.TileChanges.Add(new TileChange(group.OuterTiles[tileIndex.x, tileIndex.y].Tile, position, layer));
                    }
                }
            }
            return change;
        }

        public static BoardChange CalculateNineGroupChanges(NineGroup group, Layer layer, Vector3 worldPos)
        {
            BoardChange change = new BoardChange();

            Vector3Int targetCenter = layer.WorldToCell(worldPos);

            //Adjacency matrix where the target brush tile is at the center,
            //Tiles of the same group are set to true, null and other tiles false
            int n = 7; //Size of adjacency matrix
            bool[,] adj = new bool[n, n];

            //Fill adjacency matrix according to relavent tile info
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    TileBase tile = layer.GetTile(
                        new Vector3Int(targetCenter.x - (n / 2) + i, targetCenter.y - (n / 2) + j, 0));

                    adj[i, j] = tile != null && group.Contains(tile);
                }
            }

            //Set the tiles that must be placed as true
            adj[3, 3] = true;
            adj[3, 4] = true;
            adj[4, 3] = true;
            adj[4, 4] = true;

            //Determine tiles based on the adjacency matrix
            for (int i = 1; i < n - 1; i++)
            {
                for (int j = 1; j < n - 1; j++)
                {
                    if (adj[i, j])
                    {
                        Vector3Int position = targetCenter + new Vector3Int(i - (n / 2), j - (n / 2), 0);

                        Vector2Int tileIndex = new Vector2Int(1, 1);
                        if (adj[i - 1, j] && !adj[i + 1, j])
                            tileIndex.x = 2;
                        else if (!adj[i - 1, j] && adj[i + 1, j])
                            tileIndex.x = 0;

                        if (adj[i, j - 1] && !adj[i, j + 1])
                            tileIndex.y = 2;
                        else if (!adj[i, j - 1] && adj[i, j + 1])
                            tileIndex.y = 0;

                        change.TileChanges.Add(new TileChange(group.Tiles[tileIndex.x, tileIndex.y].Tile, position, layer));
                    }
                }
            }
            return change;
        }

        /// <summary>
        /// If a user would make input at a given position with the nine four tile group selected,
        /// what kind of changes would be made.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public static BoardChange CalculateNineFourGroupChanges(NineFourGroup group, Layer layer, Vector3 worldPos)
        {
            BoardChange change = new BoardChange();

            Layer mainLayer = layer.SubLayers[0];
            Layer cornerLayer1 = layer.SubLayers[1];
            Layer cornerLayer2 = layer.SubLayers[2];

            Vector3Int targetCenter = mainLayer.WorldToCell(worldPos);

            //Adjacency matrix where the target brush tile is at the center,
            //Tiles of the same group are set to true, null and other tiles false
            int n = 7; //Size of adjacency matrix
            bool[,] adj = new bool[n, n];

            //Fill adjacency matrix according to relavent tile info
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    TileBase tile = mainLayer.GetTile(
                        new Vector3Int(targetCenter.x - (n / 2) + i, targetCenter.y - (n / 2) + j, 0));

                    adj[i, j] = tile != null && group.Contains(tile);
                }
            }

            //Set the tiles that must be placed as true
            adj[3, 3] = true;
            adj[3, 4] = true;
            adj[4, 3] = true;
            adj[4, 4] = true;

            //Determine tiles based on the adjacency matrix
            for (int i = 1; i < n - 1; i++)
            {
                for (int j = 1; j < n - 1; j++)
                {
                    if (adj[i, j])
                    {
                        Vector3Int position = targetCenter + new Vector3Int(i - (n / 2), j - (n / 2), 0);

                        //-------------------------------------
                        //Main
                        Vector2Int tileIndex = new Vector2Int(1, 1);
                        if (adj[i - 1, j] && !adj[i + 1, j])
                            tileIndex.x = 2;
                        else if (!adj[i - 1, j] && adj[i + 1, j])
                            tileIndex.x = 0;

                        if (adj[i, j - 1] && !adj[i, j + 1])
                            tileIndex.y = 2;
                        else if (!adj[i, j - 1] && adj[i, j + 1])
                            tileIndex.y = 0;

                        change.TileChanges.Add(new TileChange(group.Tiles[tileIndex.x, tileIndex.y].Tile, position, mainLayer));

                        //-------------------------------------
                        //Path corners
                        PaletteTilesData.TileData[,] tiles = group.CornerTiles;

                        //Based on which diagonal is checked, add appropriate layer, so they don't cancel each other out
                        if (!adj[i - 1, j - 1] && adj[i, j - 1] && adj[i - 1, j])
                            change.TileChanges.Add(new TileChange(tiles[1, 1].Tile, position, cornerLayer1));
                        else if (!adj[i - 1, j + 1] && adj[i, j + 1] && adj[i - 1, j])
                            change.TileChanges.Add(new TileChange(tiles[1, 0].Tile, position, cornerLayer1));
                        else
                            change.TileChanges.Add(new TileChange(null, position, cornerLayer1));

                        if (!adj[i + 1, j + 1] && adj[i, j + 1] && adj[i + 1, j])
                            change.TileChanges.Add(new TileChange(tiles[0, 0].Tile, position, cornerLayer2));
                        else if (!adj[i + 1, j - 1] && adj[i, j - 1] && adj[i + 1, j])
                            change.TileChanges.Add(new TileChange(tiles[0, 1].Tile, position, cornerLayer2));
                        else
                            change.TileChanges.Add(new TileChange(null, position, cornerLayer2));
                    }
                }
            }
            return change;
        }

        /// <summary>
        /// If a user would make input at a given position with the REGULAR tile group selected,
        /// what kind of changes would be made.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public static BoardChange CalculateRegularTileGroupChanges(TileGroup group, Dictionary<int, Layer> layers, Vector3 worldPos)
        {
            BoardChange change = new BoardChange();

            if (Tools.TileIndex != null)
            {
                var tile = group.Tiles[Tools.TileIndex.Value.x, Tools.TileIndex.Value.y];
                if (tile != null)
                {
                    Layer layer = layers[0];

                    if (tile.Layer == PaletteData.MountainLayer)
                        layer = layers[tile.Layer].SubLayers[Tools.MountainLayer];
                    else if (layers.TryGetValue(tile.Layer, out Layer outlayer))
                    {
                        layer = outlayer;
                    }
                    else
                        Debug.Log($"Layer {tile.Layer} not set up!");

                    change.TileChanges.Add(new TileChange(tile.Tile, layer.WorldToCell(worldPos), layer.Default));
                }
            }
            else
            {
                Vector3 originWorldPos = new Vector3(worldPos.x - group.Size.x / 2, worldPos.y - group.Size.y / 2, 0);
                Vector3Int originCellPos = layers[0].WorldToCell(originWorldPos);

                for (int i = 0; i < group.Tiles.GetLength(0); i++)
                {
                    for (int j = 0; j < group.Tiles.GetLength(1); j++)
                    {
                        if (group.Tiles[i, j] != null)
                        {
                            Layer layer = layers[0];

                            if (group.Tiles[i, j].Layer == PaletteData.MountainLayer)
                                layer = layers[group.Tiles[i, j].Layer].SubLayers[Tools.MountainLayer];
                            else if (layers.TryGetValue(group.Tiles[i, j].Layer, out Layer outlayer))
                                layer = outlayer;
                            else
                                Debug.Log($"Layer {group.Tiles[i, j].Layer} not set up!");

                            change.TileChanges.Add(new TileChange(group.Tiles[i, j].Tile, originCellPos + new Vector3Int(i, j, 0), layer.Default));
                        }
                    }
                }
            }
            return change;
        }

        public static BoardChange CalculateMountainChanges(MountainGroup group, Vector3 worldPos, Layer layer)
        {
            BoardChange change = new BoardChange();

            SidesInt sides = new SidesInt { left = 2, top = 2, right = 2, bot = 3 };
            Vector2Int brush_n = new Vector2Int(sides.left + sides.right + 1, sides.top + sides.bot + 1);

            Vector3Int targetCenter = layer.WorldToCell(worldPos);
            Vector3Int targetCorner = targetCenter - new Vector3Int(sides.left * 5, sides.bot * 5, 0);

            //Adjacency matrix where the target brush tile is at the center,
            //Tiles of the same group are set to true, null and other tiles false
            //Size of adjacency matrix
            Vector3Int n = new Vector3Int(
                sides.left * 4 + brush_n.x + sides.right * 4,
                sides.bot * 4 + brush_n.y + sides.top * 4,
                0);

            List<Vector3Int> addedTiles = new List<Vector3Int>();

            for (int i = 0; i < brush_n.x; i++)
                for (int j = 0; j < brush_n.y; j++)
                    if (group.OuterTiles[i, j] != null)
                        addedTiles.Add(new Vector3Int(i + targetCorner.x + sides.left * 4, j + targetCorner.y + sides.bot * 4, 0));

            change.Add(CalculateMountainRearangementChanges(group, addedTiles, layer, targetCorner, n, sides, 0));

            return change;
        }

        public static BoardChange CalculateMountainRearangementChanges(
            MountainGroup group, List<Vector3Int> addedTiles, Layer layer, Vector3Int from, Vector3Int size, SidesInt sides, int depth)
        {
            if (depth > 1000)
                throw new System.Exception("Potential infinite recursion!");

            BoardChange change = new BoardChange();

            //Adjacency matrix where the target brush tile is at the center,
            //Tiles of the same group are set to true, null and other tiles false
            //Size of adjacency matrix
            bool[,] adj = new bool[size.x, size.y];

            //Fill adjacency matrix according to relavent tile info
            for (int i = 0; i < size.x; i++)
            {
                for (int j = 0; j < size.y; j++)
                {
                    TileBase tile = layer.GetTile(new Vector3Int(i, j, 0) + from);
                    adj[i, j] = tile != null;// && group.Contains(tile);
                }
            }

            foreach (Vector3Int pos in addedTiles)
            {
                if (pos.x >= from.x && pos.y >= from.y && pos.x < from.x + size.x && pos.y < from.y + size.y)
                    adj[pos.x - from.x, pos.y - from.y] = true;
            }

            //--------------------------------------------
            //Ensure no edge cases are present
            List<Vector3Int> added = new List<Vector3Int>();
            int lastAddedCount = -1;

            int infiniteCounter = 0;
            while (lastAddedCount != added.Count)
            {
                infiniteCounter++;
                if (infiniteCounter > 10000)
                    throw new System.Exception("Infinite cycle!");

                lastAddedCount = added.Count;
                var aj = adj.Clone() as bool[,];


                for (int i = 0; i < size.x; i++)
                {
                    for (int j = 0; j < size.y; j++)
                    {
                        //2x2 corners
                        if (i > 0 && i < size.x - 1 && j > 0 && j < size.y - 1)
                        {
                            //bottom left
                            if (!aj[i, j] && !aj[i - 1, j] && !aj[i, j - 1] &&
                                aj[i - 1, j + 1] && aj[i, j + 1] && aj[i + 1, j + 1] && aj[i + 1, j] && aj[i + 1, j - 1])
                                added.Add(new Vector3Int(i, j, 0));

                            //bottom right
                            if (!aj[i, j] && !aj[i + 1, j] && !aj[i, j - 1] &&
                                aj[i + 1, j + 1] && aj[i, j + 1] && aj[i - 1, j + 1] && aj[i - 1, j] && aj[i - 1, j - 1])
                                added.Add(new Vector3Int(i, j, 0));

                            //top right
                            if (!aj[i, j] && !aj[i + 1, j] && !aj[i, j + 1] &&
                                aj[i + 1, j - 1] && aj[i, j - 1] && aj[i - 1, j - 1] && aj[i - 1, j] && aj[i - 1, j + 1])
                                added.Add(new Vector3Int(i, j, 0));

                            //top left
                            if (!aj[i, j] && !aj[i - 1, j] && !aj[i, j + 1] &&
                                aj[i - 1, j - 1] && aj[i, j - 1] && aj[i + 1, j - 1] && aj[i + 1, j] && aj[i + 1, j + 1])
                                added.Add(new Vector3Int(i, j, 0));
                        }

                        //short diagonals
                        if (i < size.x - 4 && j < size.y - 4)
                        {
                            if (!aj[i, j] && aj[i + 1, j + 1] && aj[i, j + 1] && aj[i + 1, j] &&
                                (!aj[i + 2, j + 2] || !aj[i + 3, j + 3] || !aj[i + 4, j + 4]))
                                added.Add(new Vector3Int(i, j, 0));

                        }
                        if (i > 3 && j > 3)
                        {
                            if (!aj[i, j] && aj[i - 1, j - 1] && aj[i, j - 1] && aj[i - 1, j] &&
                                (!aj[i - 2, j - 2] || !aj[i - 3, j - 3] || !aj[i - 4, j - 4]))
                                added.Add(new Vector3Int(i, j, 0));
                        }
                        if (i < size.x - 4 && j > 3)
                        {
                            if (!aj[i, j] && aj[i + 1, j - 1] && aj[i, j - 1] && aj[i + 1, j] &&
                                (!aj[i + 2, j - 2] || !aj[i + 3, j - 3] || !aj[i + 4, j - 4]))
                                added.Add(new Vector3Int(i, j, 0));
                        }
                        if (i > 3 && j < size.y - 4)
                        {
                            if (!aj[i, j] && aj[i - 1, j + 1] && aj[i, j + 1] && aj[i - 1, j] &&
                                (!aj[i - 2, j + 2] || !aj[i - 3, j + 3] || !aj[i - 4, j + 4]))
                                added.Add(new Vector3Int(i, j, 0));

                        }
                        //up-diagonals
                        if (i > 0 && j < size.y - 5)
                        {
                            if (!aj[i, j] && aj[i - 1, j + 1] && aj[i, j + 1] && aj[i - 1, j] &&
                                (!aj[i - 1, j + 2] || !aj[i - 1, j + 3] || !aj[i - 1, j + 4] || !aj[i - 1, j + 5]))
                                added.Add(new Vector3Int(i, j, 0));

                        }
                        if (i < size.x - 1 && j < size.y - 5)
                        {
                            if (!aj[i, j] && aj[i + 1, j + 1] && aj[i, j + 1] && aj[i + 1, j] &&
                                (!aj[i + 1, j + 2] || !aj[i + 1, j + 3] || !aj[i + 1, j + 4] || !aj[i + 1, j + 5]))
                                added.Add(new Vector3Int(i, j, 0));

                        }
                        if (i > 1 && j < size.y - 5)
                        {
                            if (!aj[i, j] && aj[i - 1, j + 1] && aj[i, j + 1] && aj[i - 1, j] &&
                                (!aj[i - 2, j + 2] || !aj[i - 2, j + 3] || !aj[i - 2, j + 4] || !aj[i - 2, j + 5]))
                                added.Add(new Vector3Int(i, j, 0));

                        }
                        if (i < size.x - 2 && j < size.y - 5)
                        {
                            if (!aj[i, j] && aj[i + 1, j + 1] && aj[i, j + 1] && aj[i + 1, j] &&
                                (!aj[i + 2, j + 2] || !aj[i + 2, j + 3] || !aj[i + 2, j + 4] || !aj[i + 2, j + 5]))
                                added.Add(new Vector3Int(i, j, 0));

                        }
                        if (i > 2 && j < size.y - 5)
                        {
                            if (!aj[i, j] && aj[i - 1, j + 1] && aj[i, j + 1] && aj[i - 1, j] &&
                                (!aj[i - 2, j + 2] || !aj[i - 3, j + 3] || !aj[i - 3, j + 4] || !aj[i - 3, j + 5]))
                                added.Add(new Vector3Int(i, j, 0));

                        }
                        if (i < size.x - 3 && j < size.y - 5)
                        {
                            if (!aj[i, j] && aj[i + 1, j + 1] && aj[i, j + 1] && aj[i + 1, j] &&
                                (!aj[i + 2, j + 2] || !aj[i + 3, j + 3] || !aj[i + 3, j + 4] || !aj[i + 3, j + 5]))
                                added.Add(new Vector3Int(i, j, 0));

                        }

                        //single tile, bound by 3 sides
                        //bound from bottom
                        if (i < size.x - 1 && i > 0 && j > 0 && j < size.y - 1)
                        {
                            if (!aj[i, j] && aj[i - 1, j] && aj[i + 1, j] && aj[i, j - 1] && (aj[i - 1, j + 1] || aj[i + 1, j + 1]))
                                added.Add(new Vector3Int(i, j, 0));
                        }
                        //bound from bottom left
                        if (i > 0 && i < size.x - 1 && j > 0 && j < size.y - 1)
                        {
                            if (!aj[i, j] && aj[i - 1, j] && aj[i, j + 1] && aj[i, j - 1] && adj[i + 1, j - 1])
                                added.Add(new Vector3Int(i, j, 0));
                        }
                        //bound from right
                        if (i > 0 && i < size.x - 1 && j > 0 && j < size.y - 1)
                        {
                            if (!aj[i, j] && aj[i + 1, j] && aj[i, j + 1] && aj[i, j - 1] && adj[i - 1, j - 1])
                                added.Add(new Vector3Int(i, j, 0));
                        }


                        //width
                        if (i < size.x - 4 && j > 0)
                        {
                            if (!aj[i, j] && aj[i, j - 1] && aj[i + 1, j] && (!aj[i + 2, j] || !aj[i + 3, j] || !aj[i + 4, j]))
                                added.Add(new Vector3Int(i, j, 0));
                        }
                        if (i > 3 && j > 0)
                        {
                            if (!aj[i, j] && aj[i, j - 1] && aj[i - 1, j] && (!aj[i - 2, j] || !aj[i - 3, j] || !aj[i - 4, j]))
                                added.Add(new Vector3Int(i, j, 0));
                        }
                    }
                }

                for (int i = lastAddedCount; i < added.Count; i++)
                {
                    adj[added[i].x, added[i].y] = true;
                }
            }

            addedTiles.AddRange(added.Select(p => p + from));

            //Determine tiles based on the adjacency matrix
            for (int i = sides.left; i < size.x - sides.right; i++)
            {
                for (int j = sides.bot; j < size.y - sides.top; j++)
                {
                    if (adj[i, j])
                    {
                        Vector3Int position = from + new Vector3Int(i, j, 0);
                        SidesInt sur = SurroundingCount(adj, new Vector2Int(i, j), new SidesInt
                        {
                            left = 5,
                            right = 5,
                            top = 5,
                            bot = 5
                        });
                        //Vector2Int tileIndex = outerCenter;
                        Vector2Int outerIndex = new Vector2Int(-1, -1);
                        Vector2Int innerIndex = new Vector2Int(-1, -1);

                        //row 0
                        if (sur.left == 0 && sur.bot == 0 && !adj[i + 1, j - 1])
                            outerIndex.Set(1, 0);
                        else if (sur.left >= 1 && sur.right >= 1 && sur.bot == 0)
                            outerIndex.Set(2, 0);
                        else if (sur.right == 0 && sur.bot == 0 && !adj[i - 1, j - 1])
                            outerIndex.Set(3, 0);
                        //row1
                        else if (sur.left == 0 && sur.bot == 0 && adj[i + 1, j - 1])
                            outerIndex.Set(0, 1);
                        else if (sur.left == 1 && sur.bot == 1)
                            outerIndex.Set(1, 1);
                        else if (sur.left >= 2 && sur.right >= 2 && sur.bot == 1)
                            outerIndex.Set(2, 1);
                        else if (sur.right == 1 && sur.bot == 1)
                            outerIndex.Set(3, 1);
                        else if (sur.right == 0 && sur.bot == 0 && adj[i - 1, j - 1])
                            outerIndex.Set(4, 1);
                        //row2
                        else if (sur.left == 0 && sur.bot == 1)
                            outerIndex.Set(0, 2);
                        else if (sur.left == 1 && sur.bot == 2)
                            outerIndex.Set(1, 2);
                        else if (sur.left >= 2 && sur.right >= 2 && sur.bot == 2)
                            outerIndex.Set(2, 2);
                        else if (sur.right == 1 && sur.bot == 2)
                            outerIndex.Set(3, 2);
                        else if (sur.right == 0 && sur.bot == 1)
                            outerIndex.Set(4, 2);
                        //row3
                        else if (sur.left == 0 && sur.top >= 2 && sur.bot >= 2)
                            outerIndex.Set(0, 3);
                        else if (sur.left == 1 && sur.top >= 2 && sur.bot >= 3)
                            outerIndex.Set(1, 3);
                        else if (sur.left >= 2 && sur.right >= 2 && sur.top >= 2 && sur.bot >= 3)
                            outerIndex.Set(2, 3);
                        else if (sur.right == 1 && sur.top >= 2 && sur.bot >= 3)
                            outerIndex.Set(3, 3);
                        else if (sur.right == 0 && sur.top >= 2 && sur.bot >= 2)
                            outerIndex.Set(4, 3);
                        //row4
                        else if (sur.left == 0 && sur.top == 1)
                            outerIndex.Set(0, 4);
                        else if (sur.left == 1 && sur.top == 1)
                            outerIndex.Set(1, 4);
                        else if (sur.left >= 2 && sur.right >= 2 && sur.top == 1)
                            outerIndex.Set(2, 4);
                        else if (sur.right == 1 && sur.top == 1)
                            outerIndex.Set(3, 4);
                        else if (sur.right == 0 && sur.top == 1)
                            outerIndex.Set(4, 4);
                        //row5
                        else if (sur.left == 0 && sur.top == 0)
                            outerIndex.Set(0, 5);
                        else if (sur.left == 1 && sur.top == 0)
                            outerIndex.Set(1, 5);
                        else if (sur.left >= 2 && sur.right >= 2 && sur.top == 0)
                            outerIndex.Set(2, 5);
                        else if (sur.right == 1 && sur.top == 0)
                            outerIndex.Set(3, 5);
                        else if (sur.right == 0 && sur.top == 0)
                            outerIndex.Set(4, 5);

                        //----------------------------------------
                        //Handle corners 
                        //Mountain corners
                        if (sur.right == 1 && sur.top == 1 && !adj[i + 1, j + 1])
                            outerIndex.Set(4, 4);
                        else if (sur.left == 1 && sur.top == 1 && !adj[i - 1, j + 1])
                            outerIndex.Set(0, 4);

                        else if (sur.right >= 2 && sur.top >= 1 && !adj[i + 1, j + 1])
                            innerIndex.Set(2, 1);
                        else if (sur.left >= 2 && sur.top >= 1 && !adj[i - 1, j + 1])
                            innerIndex.Set(4, 1);
                        else if (sur.right >= 2 && sur.bot == 1 && !adj[i + 1, j - 1])
                            innerIndex.Set(2, 4);
                        else if (sur.left >= 2 && sur.bot == 1 && !adj[i - 1, j - 1])
                            innerIndex.Set(4, 4);

                        else if (sur.right == 1 && sur.bot >= 2 && !adj[i + 1, j - 1])
                            innerIndex.Set(1, 3);
                        else if (sur.left == 1 && sur.bot >= 2 && !adj[i - 1, j - 1])
                            innerIndex.Set(5, 3);

                        //Other pieces
                        else if (sur.left >= 1 && sur.bot >= 2 && !adj[i - 1, j - 1])
                            innerIndex.Set(5, 3);
                        else if (sur.right >= 1 && sur.bot >= 2 && !adj[i + 1, j - 1])
                            innerIndex.Set(1, 3);

                        else if (sur.bot >= 3 && sur.top >= 2 && sur.right == 1 && !adj[i + 1, j + 1])
                            innerIndex.Set(1, 2);
                        else if (sur.bot >= 3 && sur.top >= 2 && sur.left == 1 && !adj[i - 1, j + 1])
                            innerIndex.Set(5, 2);

                        //big grass corner pieces
                        else if (!adj[i + 1, j + 2] && !adj[i + 2, j + 1] && adj[i + 1, j + 1] && sur.top >= 1 && sur.right >= 1)
                            innerIndex.Set(1, 1);
                        else if (!adj[i - 1, j + 2] && !adj[i - 2, j + 1] && adj[i - 1, j + 1] && sur.top >= 1 && sur.left >= 1)
                            innerIndex.Set(5, 1);
                        else if (!adj[i + 1, j - 2] && adj[i + 1, j - 1] && sur.bot >= 2 && sur.right >= 1)
                            innerIndex.Set(1, 4);
                        else if (!adj[i - 1, j - 2] && adj[i - 1, j - 1] && sur.bot >= 2 && sur.left >= 1)
                            innerIndex.Set(5, 4);

                        //Straight grass pieces
                        else if (sur.top >= 2 && sur.bot >= 3 && sur.right >= 2 && (!adj[i + 2, j + 1] || !adj[i + 2, j - 1]))
                            outerIndex.Set(3, 3);
                        else if (sur.top >= 2 && sur.bot >= 3 && sur.left >= 2 && (!adj[i - 2, j + 1] || !adj[i - 2, j - 1]))
                            outerIndex.Set(1, 3);
                        else if (sur.left >= 2 && sur.right >= 2 && sur.top == 2 && (!adj[i - 1, j + 2] || !adj[i + 1, j + 2]))
                            outerIndex.Set(2, 4);
                        //else if (sur.left >= 2 && sur.right >= 2 && sur.bot == 2 && (!adj[i - 1, j - 2] || !adj[i + 1, j - 2]))
                        //    outerIndex.Set(2, 2);


                        //small grass corner pieces
                        else if (!adj[i + 2, j + 2] && adj[i + 1, j + 2] && adj[i + 2, j + 1] && sur.top >= 2 && sur.right >= 2)
                            innerIndex.Set(0, 1);
                        else if (!adj[i - 2, j + 2] && adj[i - 1, j + 2] && adj[i - 2, j + 1] && sur.top >= 2 && sur.left >= 2)
                            innerIndex.Set(5, 0);
                        else if (sur.bot >= 3 && sur.right >= 2 &&
                            ((adj[i + 2, j - 2] && !adj[i + 1, j - 3] && adj[i - 0, j - 3] && adj[i + 1, j - 2]) ||
                            (!adj[i + 2, j - 2] && adj[i + 1, j - 2] && adj[i + 2, j - 1])))
                            innerIndex.Set(0, 4);
                        else if (sur.bot >= 3 && sur.left >= 2 &&
                            ((adj[i - 2, j - 2] && !adj[i - 1, j - 3] && adj[i - 0, j - 3] && adj[i - 1, j - 2]) ||
                            (!adj[i - 2, j - 2] && adj[i - 1, j - 2] && adj[i - 2, j - 1])))
                            innerIndex.Set(6, 4);

                        if (innerIndex.x != -1)
                            change.TileChanges.Add(new TileChange(group.InnerTiles[innerIndex.x, innerIndex.y], position, layer));
                        else if (outerIndex.x != -1 && group.OuterTiles[outerIndex.x, outerIndex.y] != null)
                            change.TileChanges.Add(new TileChange(group.OuterTiles[outerIndex.x, outerIndex.y], position, layer));
                        else
                        {
                            Debug.LogError("Could not find tile index!");
                            change.TileChanges.Add(new TileChange(null, position, layer));
                        }

                    }
                }
            }

            foreach (Vector3Int pos in added)
            {
                change.Add(CalculateMountainRearangementChanges(
                    group,
                    addedTiles,
                    layer,
                    pos + from - new Vector3Int(sides.left * 3, sides.bot * 3, 0),
                    size,
                    sides,
                    depth + 1));
            }
            return change;
        }

        /// <summary>
        /// Given a matrix and a starting point, calculates how many cells are 'true' in a row
        /// in all four sides.
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="from"></param>
        /// <param name="maxDepth">Maximum depth to search</param>
        /// <returns></returns>
        public static SidesInt SurroundingCount(bool[,] matrix, Vector2Int from, SidesInt maxDepth)
        {
            maxDepth = new SidesInt
            {
                left = Mathf.Min(from.x, maxDepth.left),
                right = Mathf.Min(matrix.GetLength(0) - from.x - 1, maxDepth.right),
                bot = Mathf.Min(from.y, maxDepth.bot),
                top = Mathf.Min(matrix.GetLength(1) - from.y - 1, maxDepth.top)
            };

            SidesInt result = new SidesInt();

            for (int i = 1; i <= maxDepth.left; i++)
            {
                if (matrix[from.x - i, from.y])
                    result.left++;
                else
                    break;
            }

            for (int i = 1; i <= maxDepth.right; i++)
            {
                if (matrix[from.x + i, from.y])
                    result.right++;
                else
                    break;
            }

            for (int i = 1; i <= maxDepth.top; i++)
            {
                if (matrix[from.x, from.y + i])
                    result.top++;
                else
                    break;
            }

            for (int i = 1; i <= maxDepth.bot; i++)
            {
                if (matrix[from.x, from.y - i])
                    result.bot++;
                else
                    break;
            }
            return result;
        }

    }

    public class TileChange
    {
        public TileBase Tile { get; set; }
        public Vector3Int Position { get; set; }
        public Layer Layer { get; set; }

        public TileChange()
        {

        }
        public TileChange(TileBase tile, Vector3Int position, Layer layer)
        {
            Tile = tile;
            Position = position;
            Layer = layer;
        }
    }

    public class PrefabChange
    {
        public Vector3 PositionToPlace { get; set; }
        public GameObject PrefabToPlace { get; set; }

        public Vector3 PositionToDestroy { get; set; }
        public GameObject PrefabToDestroy { get; set; }
    }

}
