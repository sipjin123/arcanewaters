using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;
using MapCreationTool.PaletteTilesData;
using System.Linq;
using System;

namespace MapCreationTool
{
   public class BrushStroke
   {
      public const int MATRIX_PADDING = 16;

      public TileGroup tileGroup { get; private set; }
      public Type type { get; set; }

      private (int x, int y) matrixFrom;
      private (int x, int y) matrixSize;
      private bool[,] adj;
      private TileBase[,] modifiedTiles;
      private TileBase[,] modifiedTiles2;
      private Dictionary<Layer, TileBase>[,] modifiedTilesDict;
      private Dictionary<Vector3, StrokePrefab> placedPrefabs;
      private (int x, int y) rectFrom;

      public BrushStroke () {
         matrixSize = (Tools.MAX_BOARD_SIZE + MATRIX_PADDING * 2, Tools.MAX_BOARD_SIZE + MATRIX_PADDING * 2);
         matrixFrom = (-Tools.MAX_BOARD_SIZE / 2 - MATRIX_PADDING, -Tools.MAX_BOARD_SIZE / 2 - MATRIX_PADDING);
         adj = new bool[matrixSize.x, matrixSize.y];
         modifiedTiles = new TileBase[matrixSize.x, matrixSize.y];
         modifiedTiles2 = new TileBase[matrixSize.x, matrixSize.y];
         modifiedTilesDict = new Dictionary<Layer, TileBase>[matrixSize.x, matrixSize.y];
         placedPrefabs = new Dictionary<Vector3, StrokePrefab>();

         for (int i = 0; i < matrixSize.x; i++) {
            for (int j = 0; j < matrixSize.y; j++) {
               modifiedTilesDict[i, j] = new Dictionary<Layer, TileBase>();
            }
         }

         clear();
      }

      public void clear () {
         tileGroup = null;
      }

      public void newPrefabStroke (TileGroup group, Vector3 position) {
         type = Type.Prefab;
         tileGroup = group;

         placedPrefabs.Clear();
      }

      public void newTileStroke (TileGroup group, Vector2Int boardSize, Vector3Int from) {
         if (group.type == TileGroupType.NineFour) {
            type = Type.NineFourTilePlacement;
         } else if (group.type == TileGroupType.Regular) {
            type = Type.RegularTilePlacement;
         } else if (group.type == TileGroupType.Rect) {
            type = Type.TileRect;
            rectFrom = (from.x - matrixFrom.x, from.y - matrixFrom.y);
         } else {
            type = Type.TilePlacement;
         }

         matrixSize = (boardSize.x + MATRIX_PADDING * 2, boardSize.y + MATRIX_PADDING * 2);
         matrixFrom = (-boardSize.x / 2 - MATRIX_PADDING, -boardSize.y / 2 - MATRIX_PADDING);

         if (type == Type.TilePlacement || type == Type.NineFourTilePlacement || type == Type.TileRect) {
            for (int i = 0; i < matrixSize.x; i++) {
               for (int j = 0; j < matrixSize.y; j++) {
                  modifiedTiles[i, j] = null;
               }
            }
         }

         if (type == Type.NineFourTilePlacement) {
            for (int i = 0; i < matrixSize.x; i++) {
               for (int j = 0; j < matrixSize.y; j++) {
                  modifiedTiles2[i, j] = null;
               }
            }
         }

         if (type == Type.RegularTilePlacement) {
            for (int i = 0; i < matrixSize.x; i++) {
               for (int j = 0; j < matrixSize.y; j++) {
                  modifiedTilesDict[i, j].Clear();
               }
            }
         }

         fillAdjacencyMatrix(group);
         tileGroup = group;
      }

      public BoardChange calculateTileChange () {
         BoardChange result = new BoardChange();

         if (tileGroup == null) {
            return result;
         }

         if (type == Type.TilePlacement || type == Type.TileRect) {
            for (int i = 0; i < matrixSize.x; i++) {
               for (int j = 0; j < matrixSize.y; j++) {
                  if (modifiedTiles[i, j] != null) {
                     result.tileChanges.Add(new TileChange {
                        tile = modifiedTiles[i, j],
                        layer = getPrimaryTileGroupLayer(tileGroup),
                        position = new Vector3Int(i + matrixFrom.x, j + matrixFrom.y, 0)
                     });
                  }
               }
            }
         } else if (type == Type.NineFourTilePlacement) {
            Layer container = getPrimaryTileGroupLayer(tileGroup);
            NineFourGroup nfg = tileGroup as NineFourGroup;
            Layer main = container.subLayers[nfg.mainTiles[0, 0].subLayer];
            Layer corner = container.subLayers[nfg.cornerTiles[0, 0].subLayer];
            for (int i = 0; i < matrixSize.x; i++) {
               for (int j = 0; j < matrixSize.y; j++) {
                  if (modifiedTiles[i, j] != null) {
                     result.tileChanges.Add(new TileChange {
                        tile = modifiedTiles[i, j],
                        layer = main,
                        position = new Vector3Int(i + matrixFrom.x, j + matrixFrom.y, 0)
                     });
                  }
               }
            }
            if (!nfg.singleLayer) {
               for (int i = 0; i < matrixSize.x; i++) {
                  for (int j = 0; j < matrixSize.y; j++) {
                     if (modifiedTiles[i, j] != null) {
                        result.tileChanges.Add(new TileChange {
                           tile = modifiedTiles2[i, j],
                           layer = corner,
                           position = new Vector3Int(i + matrixFrom.x, j + matrixFrom.y, 0)
                        });
                     }
                  }
               }
            }
         } else if (type == Type.RegularTilePlacement) {
            for (int i = 0; i < matrixSize.x; i++) {
               for (int j = 0; j < matrixSize.y; j++) {
                  foreach (var kv in modifiedTilesDict[i, j]) {
                     result.tileChanges.Add(new TileChange {
                        tile = kv.Value,
                        layer = kv.Key,
                        position = new Vector3Int(i + matrixFrom.x, j + matrixFrom.y, 0)
                     });
                  }
               }
            }
         }

         return result;
      }

      public BoardChange calculatePrefabChange () {
         if (tileGroup == null) {
            return new BoardChange();
         }

         if (type == Type.Prefab) {
            return new BoardChange {
               prefabChanges = placedPrefabs.Select(pp => new PrefabChange {
                  positionToPlace = pp.Value.position,
                  prefabToPlace = pp.Value.original,
                  dataToSet = pp.Value.data,
               }).ToList()
            };
         }
         return new BoardChange();
      }

      public void paintPosition (Vector3 position) {
         if (tileGroup == null) {
            return;
         }

         if (type == Type.Prefab) {
            paintPrefab(position, tileGroup as PrefabGroup);
            return;
         }

         Vector3Int cellPosition = DrawBoard.worldToCell(position);
         (int x, int y) index = (cellPosition.x - matrixFrom.x, cellPosition.y - matrixFrom.y);

         switch (tileGroup.type) {
            case TileGroupType.Regular:
               paintRegularTiles(tileGroup, index);
               break;
            case TileGroupType.NineFour:
               NineFourGroup nfg = tileGroup as NineFourGroup;
               HashSet<(int x, int y)> nfTiles = getTilesToAdd(nfg, index);
               setAdjacencyPositions(nfTiles);
               addTilesToRemoveThinParts(nfTiles, (2, 2));
               paintNineFourTiles(nfg, nfTiles);
               break;
            case TileGroupType.Nine:
               NineGroup ng = tileGroup as NineGroup;
               paintTileGroup(ng, ng.pickTile, index, (1, 1));
               break;
            case TileGroupType.NineSliceInOut:
               NineSliceInOutGroup nsg = tileGroup as NineSliceInOutGroup;
               paintTileGroup(nsg, nsg.pickTile, index, (1, 1));
               break;
            case TileGroupType.Mountain:
               MountainGroup mg = tileGroup as MountainGroup;
               HashSet<(int x, int y)> mTiles = getMountainTilesToAdd(mg, index);
               setAdjacencyPositions(mTiles);
               addTilesForMountainEdgeCases(mTiles);
               paintMountainTiles(Tools.tileGroup as MountainGroup, mTiles);
               break;
            case TileGroupType.Dock:
               DockGroup dg = tileGroup as DockGroup;
               HashSet<(int x, int y)> dTiles = getTilesToAdd(dg, index);
               setAdjacencyPositions(dTiles);
               addTilesToRemoveThinParts(dTiles, 2);
               paintTiles(dg.pickTile, dTiles, (1, 1));
               break;
            case TileGroupType.Wall:
               WallGroup wg = tileGroup as WallGroup;
               HashSet<(int x, int y)> wTiles = getTilesToAdd(wg, index);
               setAdjacencyPositions(wTiles);
               addTilesToRemoveThinParts(wTiles, (1, 2));
               paintTiles(wg.pickTile, wTiles, (1, 2));
               break;
            case TileGroupType.SeaMountain:
               SeaMountainGroup smg = tileGroup as SeaMountainGroup;
               HashSet<(int x, int y)> sTiles = getTilesToAdd(smg, index);
               HashSet<(int x, int y)> brushTiles = new HashSet<(int x, int y)>(sTiles);
               setAdjacencyPositions(sTiles);
               addTilesToRemoveThinParts(sTiles, (5, 5), false);
               setAdjacencyPositions(sTiles);
               addTilesToRoundOutCorners(sTiles, false);
               setAdjacencyPositions(sTiles);
               paintSeaMountainTiles(smg, sTiles, brushTiles);
               break;
            case TileGroupType.River:
               RiverGroup rg = tileGroup as RiverGroup;
               paintTileGroup(rg, (a, x, y) => rg.pickTile(a, surroundingCount(a, (x, y), SidesInt.uniform(1)), x, y), index, (1, 1));
               break;
            case TileGroupType.InteriorWall:
               InteriorWallGroup iwg = tileGroup as InteriorWallGroup;
               HashSet<(int x, int y)> iwTiles = getTilesToAdd(iwg, index);
               setAdjacencyPositions(iwTiles);
               addTilesToRemoveThinParts(iwTiles, (iwg.brushSize.x, iwg.brushSize.y), false);
               setAdjacencyPositions(iwTiles);
               addTilesToRoundOutCorners(iwTiles, false);
               setAdjacencyPositions(iwTiles);
               addTilesForWallEdgeCases(iwTiles, false);
               setAdjacencyPositions(iwTiles);
               paintTiles((a, x, y) => iwg.pickTile(a, surroundingCount(adj, (x, y), SidesInt.uniform(4)), x, y), iwTiles, (4, 4));
               break;
            case TileGroupType.Rect:
               paintRect(tileGroup as RectTileGroup, rectFrom, index);
               break;
         }
      }

      private void paintPrefab (Vector3 position, PrefabGroup group) {
         position = DrawBoard.calculatePrefabPosition(group, group.getPrefab(), position);

         Bounds bounds = DrawBoard.getPrefabBounds();
         if (position.x < bounds.min.x || position.x > bounds.max.x || position.y < bounds.min.y || position.y > bounds.max.y)
            return;

         if (placedPrefabs.ContainsKey(position)) {
            placedPrefabs.Add(position, new StrokePrefab { original = group.getPrefab(), position = position, data = Tools.getDefaultData(group) });
         } else {
            placedPrefabs[position] = new StrokePrefab { original = group.getPrefab(), position = position, data = Tools.getDefaultData(group) };
         }
      }

      private void paintRect (RectTileGroup group, (int x, int y) p1, (int x, int y) p2) {
         for (int i = MATRIX_PADDING; i < matrixSize.x - MATRIX_PADDING; i++) {
            for (int j = MATRIX_PADDING; j < matrixSize.y - MATRIX_PADDING; j++) {
               modifiedTiles[i, j] = null;
            }
         }

         // Clamp points inside board bounds
         p1 = (Math.Max(p1.x, MATRIX_PADDING), Math.Max(p1.y, MATRIX_PADDING));
         p1 = (Math.Min(p1.x, matrixSize.x - MATRIX_PADDING - 1), Math.Min(p1.y, matrixSize.y - MATRIX_PADDING - 1));
         p2 = (Math.Max(p2.x, MATRIX_PADDING), Math.Max(p2.y, MATRIX_PADDING));
         p2 = (Math.Min(p2.x, matrixSize.x - MATRIX_PADDING - 1), Math.Min(p2.y, matrixSize.y - MATRIX_PADDING - 1));

         // Find min and max corners of the bounding box
         (int x, int y) min = (Math.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y));
         (int x, int y) max = (Math.Max(p1.x, p2.x), Mathf.Max(p1.y, p2.y));
         (int x, int y) size = (max.x - min.x + 1, max.y - min.y + 1);


         // Add left corners
         modifiedTiles[min.x, min.y] = group.tiles[0, 0].tile;
         modifiedTiles[min.x, max.y] = group.tiles[0, 2].tile;

         // Add rest of left column
         for (int i = 1; i < size.y - 1; i++) {
            modifiedTiles[min.x, min.y + i] = group.tiles[0, 1].tile;
         }

         // Add middle rows
         for (int i = 1; i < size.x - 1; i++) {
            // Add top and bot tiles
            modifiedTiles[min.x + i, min.y] = group.tiles[1, 0].tile;
            modifiedTiles[min.x + i, max.y] = group.tiles[1, 2].tile;

            // Add rest of tiles
            for (int j = 1; j < size.y - 1; j++) {
               modifiedTiles[min.x + i, min.y + j] = group.tiles[1, 1].tile;
            }
         }

         // Add right corners
         modifiedTiles[max.x, min.y] = group.tiles[2, 0].tile;
         modifiedTiles[max.x, max.y] = group.tiles[2, 2].tile;

         // Add rest of right tiles
         for (int i = 1; i < size.y - 1; i++) {
            modifiedTiles[max.x, max.y - i] = group.tiles[2, 1].tile;
         }
      }

      private void paintTileGroup (TileGroup group, Func<bool[,], int, int, TileBase> pickTile, (int x, int y) index, (int x, int y) checkAround) {
         HashSet<(int x, int y)> addedTiles = getTilesToAdd(group, index);
         setAdjacencyPositions(addedTiles);

         var bounds = getTilePickBounds(addedTiles, checkAround);

         for (int i = bounds.from.x; i < bounds.to.x; i++) {
            for (int j = bounds.from.y; j < bounds.to.y; j++) {
               if (adj[i, j]) {
                  modifiedTiles[i, j] = pickTile(adj, i, j);
               }
            }
         }
      }

      private void paintSeaMountainTiles (SeaMountainGroup group, HashSet<(int x, int y)> tilesToPaint, HashSet<(int x, int y)> brushTilesToPaint) {
         (int x, int y) margin = (3, 3);
         var bounds = getTilePickBounds(tilesToPaint, margin);
         Layer l = layer(Layer.MOUNTAIN_KEY, Tools.mountainLayer);


         for (int i = bounds.from.x; i < bounds.to.x; i++) {
            for (int j = bounds.from.y; j < bounds.to.y; j++) {
               if (adj[i, j]) {
                  // Pick out the target tile
                  TileBase tile = group.pickTile(adj, surroundingCount(adj, (i, j), SidesInt.uniform(3)), i, j);

                  // Get the existing tile
                  TileBase existing = modifiedTiles[i, j];
                  if (existing == null) {
                     existing = l.getTile(i + matrixFrom.x, j + matrixFrom.y);
                  }
                  // If this tile is not part of the core brush size and is the same as the tile in an alternative group, don't paint
                  if (brushTilesToPaint.Contains((i, j)) || !group.areAlternativeTiles(tile, existing)) {
                     modifiedTiles[i, j] = tile;
                  }
               }
            }
         }
      }

      private void paintTiles (Func<bool[,], int, int, TileBase> pickTile, HashSet<(int x, int y)> tilesToPaint, (int x, int y) checkAround) {
         setAdjacencyPositions(tilesToPaint);

         var bounds = getTilePickBounds(tilesToPaint, checkAround);

         for (int i = bounds.from.x; i < bounds.to.x; i++) {
            for (int j = bounds.from.y; j < bounds.to.y; j++) {
               if (adj[i, j]) {
                  modifiedTiles[i, j] = pickTile(adj, i, j);
               }
            }
         }
      }

      private void paintMountainTiles (MountainGroup group, HashSet<(int x, int y)> tilesToPaint) {
         var bounds = getTilePickBounds(tilesToPaint, 5);

         for (int i = bounds.from.x; i < bounds.to.x; i++) {
            for (int j = bounds.from.y; j < bounds.to.y; j++) {
               if (adj[i, j]) {
                  SidesInt sur = surroundingCount(adj, (i, j), new SidesInt {
                     left = 5,
                     right = 5,
                     top = 5,
                     bot = 5
                  });

                  modifiedTiles[i, j] = group.pickTile(adj, sur, i, j);
               }
            }
         }
      }

      private void paintNineFourTiles (NineFourGroup group, HashSet<(int x, int y)> tilesToPaint) {
         setAdjacencyPositions(tilesToPaint);

         var bounds = getTilePickBounds(tilesToPaint, 2);

         for (int i = bounds.from.x; i < bounds.to.x; i++) {
            for (int j = bounds.from.y; j < bounds.to.y; j++) {
               if (adj[i, j]) {
                  if (group.singleLayer) {
                     modifiedTiles[i, j] = group.pickCornerTile(adj, i, j);
                     if (modifiedTiles[i, j] == null) {
                        modifiedTiles[i, j] = group.pickMainTile(adj, i, j);
                     }
                  } else {
                     modifiedTiles[i, j] = group.pickMainTile(adj, i, j);
                     modifiedTiles2[i, j] = group.pickCornerTile(adj, i, j);
                  }
               }
            }
         }
      }

      private void paintRegularTiles (TileGroup group, (int x, int y) index) {
         HashSet<(int x, int y)> tilesToAdd = getTilesToAdd(group, index);

         foreach (var p in tilesToAdd) {
            (int x, int y) i = (p.x - index.x + group.size.x / 2, p.y - index.y + group.size.y / 2);

            if (group.tiles[i.x, i.y] != null) {
               Layer layer = null;

               if (Layer.isMountain(group.tiles[i.x, i.y].layer))
                  layer = layers[group.tiles[i.x, i.y].layer].subLayers[Tools.mountainLayer];
               else if (layers.TryGetValue(group.tiles[i.x, i.y].layer, out Layer outlayer))
                  layer = outlayer.subLayers[group.tiles[i.x, i.y].subLayer];
               else
                  Debug.Log($"Layer {group.tiles[i.x, i.y].layer} not set up. Tile = {group.tiles[i.x, i.y].tile}.");

               if (!modifiedTilesDict[p.x, p.y].ContainsKey(layer)) {
                  modifiedTilesDict[p.x, p.y].Add(layer, group.tiles[i.x, i.y].tile);
               } else {
                  modifiedTilesDict[p.x, p.y][layer] = group.tiles[i.x, i.y].tile;
               }
            }
         }
      }

      private HashSet<(int x, int y)> getTilesToAdd (TileGroup group, (int x, int y) index) {
         HashSet<(int x, int y)> tiles = new HashSet<(int x, int y)>();

         (int x, int y) targetCenter = (index.x - group.brushSize.x / 2, index.y - group.brushSize.y / 2);

         for (int i = 0; i < group.brushSize.x; i++) {
            for (int j = 0; j < group.brushSize.y; j++) {
               (int x, int y) p = (i + targetCenter.x, j + targetCenter.y);
               if (p.x >= MATRIX_PADDING && p.y >= MATRIX_PADDING && p.x < matrixSize.x - MATRIX_PADDING && p.y < matrixSize.y - MATRIX_PADDING) {
                  tiles.Add(p);
               }
            }
         }

         return tiles;
      }

      private HashSet<(int x, int y)> getMountainTilesToAdd (MountainGroup group, (int x, int y) index) {
         HashSet<(int x, int y)> tiles = new HashSet<(int x, int y)>();

         (int x, int y) targetCenter = (index.x - group.brushSize.x / 2, index.y - group.brushSize.y / 2);

         for (int i = 0; i < group.brushSize.x; i++) {
            for (int j = 0; j < group.brushSize.y; j++) {
               (int x, int y) p = (i + targetCenter.x, j + targetCenter.y);

               if (group.outerTiles[i, j] != null) {
                  if (p.x >= MATRIX_PADDING && p.y >= MATRIX_PADDING && p.x < matrixSize.x - MATRIX_PADDING && p.y < matrixSize.y - MATRIX_PADDING) {
                     tiles.Add(p);
                  }
               }
            }
         }

         return tiles;
      }

      private ((int x, int y) from, (int x, int y) to) getTilePickBounds (HashSet<(int x, int y)> positions, int margin) {
         return getTilePickBounds(positions, (margin, margin));
      }

      private ((int x, int y) from, (int x, int y) to) getTilePickBounds (HashSet<(int x, int y)> positions, (int x, int y) margin) {
         if (positions.Count == 0) {
            return ((0, 0), (0, 0));
         }

         var from = (
            Math.Max(positions.Min(t => t.x) - margin.x, MATRIX_PADDING),
            Math.Max(positions.Min(t => t.y) - margin.y, MATRIX_PADDING)
            );
         var to = (
            Math.Min(positions.Max(t => t.x) + margin.x + 1, matrixSize.x - MATRIX_PADDING),
            Math.Min(positions.Max(t => t.y) + margin.y + 1, matrixSize.y - MATRIX_PADDING)
            );

         return (from, to);
      }

      public void fillAdjacencyMatrix (TileGroup group) {
         Layer layer = getPrimaryTileGroupLayer(group);
         if (layer == null) {
            return;
         }

         if (group is NineFourGroup) {
            layer = layer.subLayers[(group as NineFourGroup).mainTiles[0, 0].subLayer];
         }

         // Fill out-of-bounds positions with 'true'
         for (int i = 0; i < MATRIX_PADDING; i++) {
            for (int j = 0; j < matrixSize.y; j++) {
               adj[i, j] = true;
            }
         }

         for (int i = matrixSize.x - MATRIX_PADDING; i < matrixSize.x; i++) {
            for (int j = 0; j < matrixSize.y; j++) {
               adj[i, j] = true;
            }
         }

         for (int i = MATRIX_PADDING; i < matrixSize.x - MATRIX_PADDING; i++) {
            for (int j = 0; j < MATRIX_PADDING; j++) {
               adj[i, j] = true;
            }
         }

         for (int i = MATRIX_PADDING; i < matrixSize.x - MATRIX_PADDING; i++) {
            for (int j = matrixSize.y - MATRIX_PADDING; j < matrixSize.y; j++) {
               adj[i, j] = true;
            }
         }

         //Fill adjacency matrix according to relavent tile info
         for (int i = MATRIX_PADDING; i < matrixSize.x - MATRIX_PADDING; i++) {
            for (int j = MATRIX_PADDING; j < matrixSize.y - MATRIX_PADDING; j++) {
               TileBase tile = layer.getTile(i + matrixFrom.x, j + matrixFrom.y);
               adj[i, j] = tile != null && (group.contains(tile) || group.alternativeGroupsContain(tile));
            }
         }
      }

      public void setAdjacencyPositions (IEnumerable<(int x, int y)> positions) {
         foreach (var p in positions) {
            adj[p.x, p.y] = true;
         }
      }

      private Layer getPrimaryTileGroupLayer (TileGroup group) {
         switch (Tools.tileGroup.type) {
            case TileGroupType.Rect:
               RectTileGroup rcg = group as RectTileGroup;
               return layer(rcg.layer, rcg.subLayer);
            case TileGroupType.NineFour:
               NineFourGroup nfg = group as NineFourGroup;
               return layers[nfg.layer];
            case TileGroupType.Nine:
               NineGroup ng = group as NineGroup;
               return layers[ng.layer].subLayers[ng.subLayer];
            case TileGroupType.NineSliceInOut:
               NineSliceInOutGroup nsg = group as NineSliceInOutGroup;
               return layer(nsg.tiles[0, 0].layer, nsg.tiles[0, 0].subLayer);
            case TileGroupType.Mountain:
               return layer(Layer.MOUNTAIN_KEY, Tools.mountainLayer);
            case TileGroupType.Dock:
               DockGroup dg = group as DockGroup;
               return layer(dg.layer, dg.subLayer);
            case TileGroupType.Wall:
               WallGroup wg = group as WallGroup;
               return layer(wg.layer, 0);
            case TileGroupType.SeaMountain:
               return layer(Layer.MOUNTAIN_KEY, Tools.mountainLayer);
            case TileGroupType.River:
               RiverGroup rg = group as RiverGroup;
               return layer(rg.layer, rg.subLayer);
            case TileGroupType.InteriorWall:
               InteriorWallGroup iwg = group as InteriorWallGroup;
               return layer(iwg.layer, iwg.subLayer);
            default:
               return null;
         }
      }

      private void addTilesForMountainEdgeCases (HashSet<(int x, int y)> placed) {
         List<(int x, int y)> added = new List<(int x, int y)>();
         int lastAddedCount = -1;

         for (int k = 0; k < 10000; k++) {
            // Break when no changes were made
            if (lastAddedCount == added.Count)
               break;

            lastAddedCount = added.Count;

            for (int i = MATRIX_PADDING; i < matrixSize.x - MATRIX_PADDING; i++) {
               for (int j = MATRIX_PADDING; j < matrixSize.y - MATRIX_PADDING; j++) {
                  // Some special cases around the edges of the map cause issues, skip them
                  if (j == MATRIX_PADDING) {
                     if (!adj[i, j] && !adj[i - 1, j] && !adj[i - 1, j + 1] && !adj[i, j + 1] && adj[i + 1, j] && adj[i + 1, j + 1])
                        continue;

                     if (!adj[i, j] && !adj[i + 1, j] && !adj[i + 1, j + 1] && !adj[i, j + 1] && adj[i - 1, j] && adj[i - 1, j + 1])
                        continue;
                  }

                  if (j == matrixSize.y - MATRIX_PADDING - 1) {
                     if (!adj[i, j] && !adj[i - 1, j] && !adj[i - 1, j - 1] && !adj[i, j - 1] && adj[i + 1, j] && adj[i + 1, j - 1])
                        continue;

                     if (!adj[i, j] && !adj[i + 1, j] && !adj[i + 1, j - 1] && !adj[i, j - 1] && adj[i - 1, j] && adj[i - 1, j - 1])
                        continue;
                  }

                  //2x2 corners
                  //bottom left
                  if (!adj[i, j] && !adj[i - 1, j] && !adj[i, j - 1] &&
                      adj[i - 1, j + 1] && adj[i, j + 1] && adj[i + 1, j + 1] && adj[i + 1, j] && adj[i + 1, j - 1])
                     added.Add((i, j));

                  //bottom right
                  if (!adj[i, j] && !adj[i + 1, j] && !adj[i, j - 1] &&
                      adj[i + 1, j + 1] && adj[i, j + 1] && adj[i - 1, j + 1] && adj[i - 1, j] && adj[i - 1, j - 1])
                     added.Add((i, j));

                  //top right
                  if (!adj[i, j] && !adj[i + 1, j] && !adj[i, j + 1] &&
                      adj[i + 1, j - 1] && adj[i, j - 1] && adj[i - 1, j - 1] && adj[i - 1, j] && adj[i - 1, j + 1])
                     added.Add((i, j));

                  //top left
                  if (!adj[i, j] && !adj[i - 1, j] && !adj[i, j + 1] &&
                      adj[i - 1, j - 1] && adj[i, j - 1] && adj[i + 1, j - 1] && adj[i + 1, j] && adj[i + 1, j + 1])
                     added.Add((i, j));

                  //short diagonals
                  if (!adj[i, j] && adj[i + 1, j + 1] && adj[i, j + 1] && adj[i + 1, j] &&
                         (!adj[i + 2, j + 2] || !adj[i + 3, j + 3] || !adj[i + 4, j + 4]))
                     added.Add((i, j));
                  if (!adj[i, j] && adj[i - 1, j - 1] && adj[i, j - 1] && adj[i - 1, j] &&
                         (!adj[i - 2, j - 2] || !adj[i - 3, j - 3] || !adj[i - 4, j - 4]))
                     added.Add((i, j));
                  if (!adj[i, j] && adj[i + 1, j - 1] && adj[i, j - 1] && adj[i + 1, j] &&
                         (!adj[i + 2, j - 2] || !adj[i + 3, j - 3] || !adj[i + 4, j - 4]))
                     added.Add((i, j));
                  if (!adj[i, j] && adj[i - 1, j + 1] && adj[i, j + 1] && adj[i - 1, j] &&
                         (!adj[i - 2, j + 2] || !adj[i - 3, j + 3] || !adj[i - 4, j + 4]))
                     added.Add((i, j));
                  //up-diagonals
                  if (!adj[i, j] && adj[i - 1, j + 1] && adj[i, j + 1] && adj[i - 1, j] &&
                         (!adj[i - 1, j + 2] || !adj[i - 1, j + 3] || !adj[i - 1, j + 4] || !adj[i - 1, j + 5]))
                     added.Add((i, j));
                  if (!adj[i, j] && adj[i + 1, j + 1] && adj[i, j + 1] && adj[i + 1, j] &&
                         (!adj[i + 1, j + 2] || !adj[i + 1, j + 3] || !adj[i + 1, j + 4] || !adj[i + 1, j + 5]))
                     added.Add((i, j));
                  if (!adj[i, j] && adj[i - 1, j + 1] && adj[i, j + 1] && adj[i - 1, j] &&
                         (!adj[i - 2, j + 2] || !adj[i - 2, j + 3] || !adj[i - 2, j + 4] || !adj[i - 2, j + 5]))
                     added.Add((i, j));
                  if (!adj[i, j] && adj[i + 1, j + 1] && adj[i, j + 1] && adj[i + 1, j] &&
                         (!adj[i + 2, j + 2] || !adj[i + 2, j + 3] || !adj[i + 2, j + 4] || !adj[i + 2, j + 5]))
                     added.Add((i, j));
                  if (!adj[i, j] && adj[i - 1, j + 1] && adj[i, j + 1] && adj[i - 1, j] &&
                         (!adj[i - 2, j + 2] || !adj[i - 3, j + 3] || !adj[i - 3, j + 4] || !adj[i - 3, j + 5]))
                     added.Add((i, j));
                  if (!adj[i, j] && adj[i + 1, j + 1] && adj[i, j + 1] && adj[i + 1, j] &&
                         (!adj[i + 2, j + 2] || !adj[i + 3, j + 3] || !adj[i + 3, j + 4] || !adj[i + 3, j + 5]))
                     added.Add((i, j));

                  //single tile, bound by 3 sides
                  //bound from bottom
                  if (!adj[i, j] && adj[i - 1, j] && adj[i + 1, j] && adj[i, j - 1] && (adj[i - 1, j + 1] || adj[i + 1, j + 1]))
                     added.Add((i, j));
                  //bound from bottom left
                  if (!adj[i, j] && adj[i - 1, j] && adj[i, j + 1] && adj[i, j - 1] && adj[i + 1, j - 1])
                     added.Add((i, j));
                  //bound from right
                  if (!adj[i, j] && adj[i + 1, j] && adj[i, j + 1] && adj[i, j - 1] && adj[i - 1, j - 1])
                     added.Add((i, j));

                  //width
                  if (!adj[i, j] && adj[i, j - 1] && adj[i + 1, j] && (!adj[i + 2, j] || !adj[i + 3, j] || !adj[i + 4, j]))
                     added.Add((i, j));
                  if (!adj[i, j] && adj[i, j - 1] && adj[i - 1, j] && (!adj[i - 2, j] || !adj[i - 3, j] || !adj[i - 4, j]))
                     added.Add((i, j));
               }
            }

            for (int i = lastAddedCount; i < added.Count; i++) {
               adj[added[i].x, added[i].y] = true;
            }

            if (k == 9999)
               Debug.LogError("Potential infinite cycle!");
         }

         foreach (var p in added) {
            placed.Add(p);
         }
      }

      private void addTilesForWallEdgeCases (HashSet<(int x, int y)> placed, bool ofBoundsIsFilled = true) {
         Func<(int x, int y), bool> has = (p) => {
            return placed.Contains(p) || adj[p.x, p.y];
         };

         if (ofBoundsIsFilled) {
            has = (p) => {
               return placed.Contains(p) || adj[p.x, p.y] ||
                  p.x < MATRIX_PADDING || p.x > matrixSize.x - MATRIX_PADDING || p.y < MATRIX_PADDING || p.y > matrixSize.y - MATRIX_PADDING;
            };
         }

         bool[,] aj = new bool[3, 3];

         Queue<(int x, int y)> queue = new Queue<(int x, int y)>(placed.SelectMany(p => get8SideNeighbours(p)));

         for (int i = 0; i < 100000; i++) {
            if (queue.Count == 0) {
               return;
            }

            (int x, int y) p = queue.Dequeue();

            //Check if already added
            if (placed.Contains(p))
               continue;

            //Check if not empty
            if (has(p))
               continue;

            bool add = false;

            for (int ii = 0; ii < 3; ii++)
               for (int jj = 0; jj < 3; jj++)
                  aj[ii, jj] = has((p.x + ii - 1, p.y + jj - 1));

            if (aj[1, 2] && aj[2, 2] && aj[2, 1] && aj[2, 0] && !aj[1, 1] && !aj[1, 0])
               add = true;

            else if (aj[1, 2] && aj[0, 2] && aj[0, 1] && aj[0, 0] && !aj[1, 1] && !aj[1, 0])
               add = true;

            else if (aj[1, 2] && aj[1, 0] && aj[2, 1] && aj[0, 1])
               add = true;

            if (add) {
               placed.Add(p);
               foreach (var n in get8SideNeighbours(p))
                  if (!has(n))
                     queue.Enqueue(n);
            }
         }

         Debug.LogError("Exceded maximum iteration count. Potential infinite cycle.");
      }

      private void addTilesToRoundOutCorners (HashSet<(int x, int y)> placed, bool ofBoundsIsFilled = true) {
         Func<(int x, int y), bool> has = (p) => {
            return placed.Contains(p) || adj[p.x, p.y];
         };

         if (ofBoundsIsFilled) {
            has = (p) => {
               return placed.Contains(p) || adj[p.x, p.y] ||
                  p.x < MATRIX_PADDING || p.x > matrixSize.x - MATRIX_PADDING || p.y < MATRIX_PADDING || p.y > matrixSize.y - MATRIX_PADDING;
            };
         }

         bool[,] aj = new bool[3, 3];

         Queue<(int x, int y)> queue = new Queue<(int x, int y)>(placed.SelectMany(p => get8SideNeighbours(p)));

         for (int i = 0; i < 100000; i++) {
            if (queue.Count == 0) {
               return;
            }

            (int x, int y) p = queue.Dequeue();

            //Check if already added
            if (placed.Contains(p))
               continue;

            //Check if not empty
            if (has(p))
               continue;

            bool add = false;

            for (int ii = 0; ii < 3; ii++)
               for (int jj = 0; jj < 3; jj++)
                  aj[ii, jj] = has((p.x + ii - 1, p.y + jj - 1));

            if (!aj[0, 1] && !aj[1, 0] &&
             aj[0, 2] && aj[1, 2] && aj[2, 2] && aj[2, 1] && aj[2, 0])
               add = true;

            //bottom right
            else if (!aj[2, 1] && !aj[1, 0] &&
                aj[2, 2] && aj[1, 2] && aj[0, 2] && aj[0, 1] && aj[0, 0])
               add = true;

            //top right
            else if (!aj[2, 1] && !aj[1, 2] &&
                aj[2, 0] && aj[1, 0] && aj[0, 0] && aj[0, 1] && aj[0, 2])
               add = true;

            //top left
            else if (!aj[0, 1] && !aj[0, 1] &&
                aj[0, 0] && aj[1, 0] && aj[2, 0] && aj[2, 1] && aj[2, 2])
               add = true;

            //single tile, bound by 3 sides
            //bound from bottom
            else if (aj[0, 1] && aj[2, 1] && aj[1, 0])
               add = true;
            //bound from bottom left
            else if (aj[0, 1] && aj[1, 2] && aj[1, 0] && aj[2, 0])
               add = true;

            //bound from right
            else if (aj[2, 1] && aj[1, 2] && aj[1, 0] && aj[0, 0])
               add = true;

            //bound from top
            else if (aj[0, 1] && aj[2, 1] && aj[1, 2])
               add = true;

            if (add) {
               placed.Add(p);
               foreach (var n in get8SideNeighbours(p))
                  if (!has(n))
                     queue.Enqueue(n);
            }
         }

         Debug.LogError("Exceded maximum iteration count. Potential infinite cycle.");
      }

      /// <summary>
      /// Finds the aditional positions that must be placed to make sure no 'thin' parts are present
      /// </summary>
      /// <param name="layer">The layer which to check</param>
      /// <param name="placed">Aditional tiles that will be placed inside the layer</param>
      /// <returns>The given 'placed' positions, and new positions to remove thin parts </returns>
      private void addTilesToRemoveThinParts (HashSet<(int x, int y)> placed, int minThiccness, bool ofBoundsIsFilled = true) {
         addTilesToRemoveThinParts(placed, (minThiccness, minThiccness), ofBoundsIsFilled);
      }

      /// <summary>
      /// Finds the aditional positions that must be placed to make sure no 'thin' parts are present
      /// </summary>
      /// <param name="layer">The layer which to check</param>
      /// <param name="placed">Aditional tiles that will be placed inside the layer</param>
      /// <returns>The given 'placed' positions, and new positions to remove thin parts </returns>
      private void addTilesToRemoveThinParts (HashSet<(int x, int y)> placed, (int x, int y) minThiccness, bool ofBoundsIsFilled = true) {
         (int x, int y) minDistance = (minThiccness.x + 1, minThiccness.y + 1);

         Func<(int x, int y), bool> has = (p) => {
            return placed.Contains(p) || adj[p.x, p.y];
         };

         if (ofBoundsIsFilled) {
            has = (p) => {
               return placed.Contains(p) || adj[p.x, p.y] ||
                  p.x < MATRIX_PADDING || p.x > matrixSize.x - MATRIX_PADDING || p.y < MATRIX_PADDING || p.y > matrixSize.y - MATRIX_PADDING;
            };
         }

         Queue<(int x, int y)> queue = new Queue<(int x, int y)>(placed.SelectMany(p => get4SideNeighbours(p)));

         for (int j = 0; j < 100000; j++) {
            if (queue.Count == 0)
               return;

            (int x, int y) p = queue.Dequeue();

            //Check if already added
            if (placed.Contains(p))
               continue;

            //Check if not empty
            if (has(p))
               continue;

            //Check straight to all 4 sides
            int dst = distanceToEmpty(has, p, (-1, 0), minDistance.x);
            if (dst > 1 && dst < minDistance.x) {
               placed.Add(p);
               foreach (var n in get4SideNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }

            dst = distanceToEmpty(has, p, (1, 0), minDistance.x);
            if (dst > 1 && dst < minDistance.x) {
               placed.Add(p);
               foreach (var n in get4SideNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }

            dst = distanceToEmpty(has, p, (0, 1), minDistance.y);
            if (dst > 1 && dst < minDistance.y) {
               placed.Add(p);
               foreach (var n in get4SideNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }

            dst = distanceToEmpty(has, p, (0, -1), minDistance.y);
            if (dst > 1 && dst < minDistance.y) {
               placed.Add(p);
               foreach (var n in get4SideNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }

            //Check diagonals
            //top right
            if (has((p.x, p.y + 1)) && has((p.x + 1, p.y)) && !distanceToEmptyAtLeast(has, p, (1, 1), minDistance)) {

               placed.Add(p);
               foreach (var n in get4SideNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }
            //top left
            if (has((p.x, p.y + 1)) && has((p.x - 1, p.y)) && !distanceToEmptyAtLeast(has, p, (-1, 1), minDistance)) {
               placed.Add(p);
               foreach (var n in get4SideNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }
            //bot right
            if (has((p.x, p.y - 1)) && has((p.x + 1, p.y)) && !distanceToEmptyAtLeast(has, p, (1, -1), minDistance)) {
               placed.Add(p);
               foreach (var n in get4SideNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }
            //bot left
            if (has((p.x, p.y - 1)) && has((p.x - 1, p.y)) && !distanceToEmptyAtLeast(has, p, (-1, -1), minDistance)) {
               placed.Add(p);
               foreach (var n in get4SideNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }
         }

         Debug.LogError("Exceded maximum iteration count. Potential infinite cycle.");
      }

      /// <summary>
      /// Searches for a path to an empty cell, calculating the distance.
      /// </summary>
      /// <param name="hasTile"></param>
      /// <param name="from"></param>
      /// <param name="direction">Direction to seatch</param>
      /// <param name="maxDepth"></param>
      /// <returns>Returns true, if the distance to an empty cell is at least maxDepth</returns>
      private static bool distanceToEmptyAtLeast (Func<(int x, int y), bool> hasTile, (int x, int y) from, (int x, int y) direction, (int x, int y) maxDepth) {
         return
             distanceToEmpty(hasTile, from, (direction.x, 0), direction, maxDepth) == maxDepth.x &&
             distanceToEmpty(hasTile, from, (0, direction.y), direction, maxDepth) == maxDepth.y;
      }

      /// <summary>
      /// Checks the distance to an empty cell. 
      /// </summary>
      /// <param name="hasTile">Funtion to check if a tile is present in a given place</param>
      /// <param name="from">From where to look</param>
      /// <param name="direction"></param>
      /// <param name="maxDepth">Maximum length of path to check</param>
      /// <returns>Maximum length of path, but no bigger than maxDepth</returns>
      private static int distanceToEmpty (Func<(int x, int y), bool> hasTile, (int x, int y) from, (int x, int y) direction, int maxDepth) {
         (int x, int y) next = (from.x + direction.x, from.y + direction.y);
         if (!hasTile(next) || maxDepth == 1)
            return 1;
         else
            return 1 + distanceToEmpty(hasTile, next, direction, maxDepth - 1);
      }

      /// <summary>
      /// Checks the distance to an empty cell.
      /// </summary>
      /// <param name="hasTile">Funtion to check if a tile is present in a given place</param>
      /// <param name="from">From where to look</param>
      /// <param name="maxDepth">Maximum length of path to check</param>
      /// <returns></returns>
      private static int distanceToEmpty (Func<(int x, int y), bool> hasTile, (int x, int y) from, (int x, int y) straightDir, (int x, int y) diagonalDir, (int x, int y) maxDepth) {
         //Check if maxDepth to the direction of straightDir is 1
         if (Math.Abs(straightDir.x) * maxDepth.x == 1 || Math.Abs(straightDir.y) * maxDepth.y == 1)
            return 1;

         if (maxDepth.x > 1 && maxDepth.y > 1) {
            if (!hasTile((from.x + diagonalDir.x, from.y + diagonalDir.y)))
               return 1;
            return 1 + Mathf.Min(
                distanceToEmpty(hasTile, (from.x + straightDir.x, from.y + straightDir.y), straightDir, diagonalDir,
                (maxDepth.x - Math.Abs(straightDir.x), maxDepth.y - Math.Abs(straightDir.y))),
                distanceToEmpty(hasTile, (from.x + diagonalDir.x, from.y + diagonalDir.y), straightDir, diagonalDir,
                (maxDepth.x - 1, maxDepth.y - 1)));
         }

         if (!hasTile((from.x + straightDir.x, from.y + straightDir.y)))
            return 1;

         return
             1 + distanceToEmpty(hasTile, (from.x + straightDir.x, from.y + straightDir.y), straightDir, diagonalDir,
             (maxDepth.x - Math.Abs(straightDir.x), maxDepth.y - Math.Abs(straightDir.y)));
      }

      private IEnumerable<(int, int)> get8SideNeighbours ((int x, int y) p) {
         yield return (p.x, p.y + 1);
         yield return (p.x, p.y - 1);
         yield return (p.x - 1, p.y);
         yield return (p.x + 1, p.y);
         yield return (p.x + 1, p.y + 1);
         yield return (p.x - 1, p.y + 1);
         yield return (p.x + 1, p.y - 1);
         yield return (p.x - 1, p.y - 1);
      }

      private IEnumerable<(int, int)> get4SideNeighbours ((int x, int y) p) {
         yield return (p.x, p.y + 1);
         yield return (p.x, p.y - 1);
         yield return (p.x - 1, p.y);
         yield return (p.x + 1, p.y);
      }

      /// <summary>
      /// Given a matrix and a starting point, calculates how many cells are 'true' in a row
      /// in all four sides.
      /// </summary>
      /// <param name="matrix"></param>
      /// <param name="from"></param>
      /// <param name="maxDepth">Maximum depth to search</param>
      /// <returns></returns>
      public static SidesInt surroundingCount (bool[,] matrix, (int x, int y) from, SidesInt maxDepth) {
         maxDepth = new SidesInt {
            left = Mathf.Min(from.x, maxDepth.left),
            right = Mathf.Min(matrix.GetLength(0) - from.x - 1, maxDepth.right),
            bot = Mathf.Min(from.y, maxDepth.bot),
            top = Mathf.Min(matrix.GetLength(1) - from.y - 1, maxDepth.top)
         };

         SidesInt result = new SidesInt();

         for (int i = 1; i <= maxDepth.left; i++) {
            if (matrix[from.x - i, from.y])
               result.left++;
            else
               break;
         }

         for (int i = 1; i <= maxDepth.right; i++) {
            if (matrix[from.x + i, from.y])
               result.right++;
            else
               break;
         }

         for (int i = 1; i <= maxDepth.top; i++) {
            if (matrix[from.x, from.y + i])
               result.top++;
            else
               break;
         }

         for (int i = 1; i <= maxDepth.bot; i++) {
            if (matrix[from.x, from.y - i])
               result.bot++;
            else
               break;
         }
         return result;
      }

      protected Layer layer (string name) {
         return DrawBoard.instance.layers[name];
      }

      protected Layer layer (string name, int sublayer) {
         return DrawBoard.instance.layers[name].subLayers[sublayer];
      }

      protected Dictionary<string, Layer> layers
      {
         get { return DrawBoard.instance.layers; }
      }

      public enum Type
      {
         TilePlacement = 1,
         NineFourTilePlacement = 2,
         RegularTilePlacement = 3,
         TileRect = 4,
         Prefab = 5
      }

      public class StrokePrefab
      {
         // Position at which prefab is placed
         public Vector3 position;

         // The original prefab which is placed
         public GameObject original;

         // Data to set for the placed prefab
         public Dictionary<string, string> data;
      }
   }
}
