﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class Layer
   {
      public const string PATH_KEY = "path";
      public const string WATER_KEY = "water";
      public const string MOUNTAIN_KEY = "mountain";
      public const string BUILDING_KEY = "bulding";
      public const string GRASS_KEY = "grass";
      public const string VINE_KEY = "vine";
      public const string STAIR_KEY = "stair";
      public const string RUG_KEY = "rug";
      public const string DOCK_KEY = "dock";
      public const string RIVER_KEY = "river";
      public const string PATHWAY_KEY = "pathway";
      public const string CEILING_KEY = "ceiling";
      public const string WALL_KEY = "wall";
      public const string DOORFRAME_KEY = "door-frame";
      public const string ABSOLUTE_TOP_KEY = "absolute-top";

      public Layer[] subLayers { get; private set; }

      private Tilemap tilemap;
      public Vector3Int origin { get; private set; }
      public Vector3Int size { get; private set; }

      // Matrixes for specifying what is in every cell
      private TileBase[,] tileMatrix = new TileBase[0, 0];
      private TileBase[,] previewTileMatrix = new TileBase[0, 0];
      private bool[,] previewTileMatrixOccupied = new bool[0, 0];

      public Layer (Tilemap tilemap) {
         this.tilemap = tilemap;
         subLayers = new Layer[0];
      }
      public Layer (Tilemap[] subLayers) {
         tilemap = null;
         this.subLayers = subLayers.Select(tl => new Layer(tl)).ToArray();
      }

      public void Destroy () {
         if (tilemap != null)
            Object.Destroy(tilemap.gameObject);
         foreach (Layer layer in subLayers)
            layer.Destroy();
      }

      /// <summary>
      /// Checks whether the layer has a tilemap. Otherwise, the layer is only a container for sublayers
      /// </summary>
      public bool hasTilemap
      {
         get { return tilemap != null; }
      }

      public string tilemapGameObjectName
      {
         get { return tilemap.gameObject.name; }
      }

      public int tileCount
      {
         get
         {
            if (tilemap == null)
               return 0;

            int count = 0;
            for (int i = 0; i < size.x; i++) {
               for (int j = 0; j < size.y; j++) {
                  if (tileMatrix[i, j] != null) {
                     count++;
                  }

               }
            }
            return count;
         }
      }

      public TileBase getTile (Vector3Int position) {
         Vector3Int index = position - origin;
         if (index.x < 0 || index.x >= size.x || index.y < 0 || index.y >= size.y)
            return null;

         return tileMatrix[index.x, index.y];
      }

      public void setTile (Vector3Int position, TileBase tile) {
         if (tilemap == null)
            throw new System.InvalidOperationException("Layer is defined as a container, but is used as a tilemap!");

         encapsulate(position);

         Vector3Int index = position - origin;
         tileMatrix[index.x, index.y] = tile;
         tilemap.SetTile(position, tile);
         if (tile == null) {
            tilemap.SetTile(position, previewTileMatrix[index.x, index.y]);
         }
      }

      /// <summary>
      /// Given an array of tiles, calculates a change, that would revert it the array if it was applied
      /// </summary>
      /// <returns></returns>
      public BoardChange calculateUndoChange (Vector3Int[] positions, TileBase[] tiles) {
         encapsulate(positions);

         BoardChange result = new BoardChange();

         for (int i = 0; i < positions.Length; i++) {
            Vector3Int p = positions[i] - origin;
            TileBase curTile = tileMatrix[p.x, p.y];

            if (curTile != tiles[i])
               result.tileChanges.Add(new TileChange(curTile, positions[i], this));
         }

         return result;
      }

      /// <summary>
      /// Sets an array of tiles. 
      /// THE PROVIDED TILES ARRAY IS MODIFIED AND SHOULD NOT BE USED!
      /// </summary>
      /// <param name="positions"></param>
      /// <param name="tiles"></param>
      public void setTiles (Vector3Int[] positions, TileBase[] tiles) {
         if (positions.Length == 0) {
            return;
         }

         encapsulate(positions);

         for (int i = 0; i < positions.Length; i++) {
            int x = positions[i].x - origin.x;
            int y = positions[i].y - origin.y;

            tileMatrix[x, y] = tiles[i];

            if (tiles[i] == null)
               tiles[i] = previewTileMatrix[x, y];
         }

         tilemap.SetTiles(positions, tiles);
      }

      public bool hasTile (Vector3Int position) {
         int x = position.x - origin.x;
         int y = position.y - origin.y;

         if (x < 0 || x >= size.x || y < 0 || y >= size.y)
            return false;

         return tileMatrix[x, y];
      }

      public bool hasTile (Vector2Int position) {
         int x = position.x - origin.x;
         int y = position.y - origin.y;

         if (x < 0 || x >= size.x || y < 0 || y >= size.y)
            return false;

         return tileMatrix[x, y];
      }

      public bool hasTile (int x, int y) {
         x -= origin.x;
         y -= origin.y;

         if (x < 0 || x >= size.x || y < 0 || y >= size.y)
            return false;

         return tileMatrix[x, y] != null;
      }

      public void clearAllPreviewTiles () {
         (int x, int y) size = (this.size.x, this.size.y);
         for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
               if (previewTileMatrixOccupied[i, j]) {
                  setTilemapTile(new Vector3Int(i, j, 0) + origin, getTile(new Vector3Int(i, j, 0) + origin));
                  previewTileMatrix[i, j] = null;
                  previewTileMatrixOccupied[i, j] = false;
               }
            }
         }
      }

      public void setPreviewTile (Vector3Int position, TileBase tile) {
         if (tilemap == null)
            throw new System.InvalidOperationException("Layer is defined as a container, but is used as a tilemap!");

         encapsulate(position);
         Vector3Int index = position - origin;
         previewTileMatrix[index.x, index.y] = tile;
         if (tile != null) {
            setTilemapTile(position, tile);
            previewTileMatrixOccupied[index.x, index.y] = true;
         } else {
            setTilemapTile(position, tileMatrix[index.x, index.y]);
            previewTileMatrixOccupied[index.x, index.y] = false;
         }
      }

      private void setTilemapTile (Vector3Int position, TileBase tile) {
         if (tile is AnimatedTile)
            tilemap.SetTile(position, null);

         tilemap.SetTile(position, tile);
      }

      public SidesInt surroundingCountMaxOne (int x, int y) {
         SidesInt result = new SidesInt {
            left = 0,
            right = 0,
            bot = 0,
            top = 0
         };

         if (x > origin.x && getTile(x - 1, y) != null) {
            result.left = 1;
         }

         if (y > origin.y && getTile(x, y - 1) != null) {
            result.bot = 1;
         }

         if (x < origin.x + size.x - 1 && getTile(x + 1, y) != null) {
            result.right = 1;
         }

         if (y < origin.y + size.y - 1 && getTile(x, y + 1) != null) {
            result.top = 1;
         }

         return result;
      }

      public Layer defaultLayer
      {
         get
         {
            if (tilemap != null)
               return this;
            else
               return subLayers[0];
         }
      }

      /// <summary>
      /// Grows matrixes to include given position
      /// </summary>
      /// <param name="position"></param>
      private void encapsulate (Vector3Int[] positions) {
         Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, 0);
         Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, 0);

         for (int i = 0; i < positions.Length; i++) {
            if (positions[i].x < min.x)
               min.x = positions[i].x;
            if (positions[i].y < min.y)
               min.y = positions[i].y;

            if (positions[i].x > max.x)
               max.x = positions[i].x;
            if (positions[i].y > max.y)
               max.y = positions[i].y;
         }

         encapsulate(min);
         encapsulate(max);
      }

      /// <summary>
      /// Grows matrixes to include given position
      /// </summary>
      /// <param name="position"></param>
      private void encapsulate (Vector3Int position) {
         int index_x = position.x - origin.x;
         int index_y = position.y - origin.y;
         int size_x = size.x;
         int size_y = size.y;

         if (index_x < 0) {
            int missing = -index_x;

            tileMatrix = expandLeft(tileMatrix, missing, size_x, size_y);
            previewTileMatrix = expandLeft(previewTileMatrix, missing, size_x, size_y);
            previewTileMatrixOccupied = expandLeft(previewTileMatrixOccupied, missing, size_x, size_y);

            size_x += missing;
            origin = new Vector3Int(origin.x - missing, origin.y, 0);
         } else if (index_x >= size_x) {
            int missing = index_x - size_x + 1;

            tileMatrix = expandRight(tileMatrix, missing, size_x, size_y);
            previewTileMatrix = expandRight(previewTileMatrix, missing, size_x, size_y);
            previewTileMatrixOccupied = expandRight(previewTileMatrixOccupied, missing, size_x, size_y);

            size_x += missing;
         }

         if (index_y < 0) {
            int missing = -index_y;

            tileMatrix = expandDown(tileMatrix, missing, size_x, size_y);
            previewTileMatrix = expandDown(previewTileMatrix, missing, size_x, size_y);
            previewTileMatrixOccupied = expandDown(previewTileMatrixOccupied, missing, size_x, size_y);

            size_y += missing;
            origin = new Vector3Int(origin.x, origin.y - missing, 0);
         } else if (index_y >= size_y) {
            int missing = index_y - size_y + 1;

            tileMatrix = expandUp(tileMatrix, missing, size_x, size_y);
            previewTileMatrix = expandUp(previewTileMatrix, missing, size_x, size_y);
            previewTileMatrixOccupied = expandUp(previewTileMatrixOccupied, missing, size_x, size_y);

            size_y += missing;
         }

         size = new Vector3Int(size_x, size_y, 0);
      }

      private T[,] expandRight<T> (T[,] target, int count, int size_x, int size_y) {
         T[,] result = new T[size_x + count, size_y];

         for (int i = 0; i < size_x; i++)
            for (int j = 0; j < size_y; j++)
               result[i, j] = target[i, j];

         return result;
      }

      private T[,] expandLeft<T> (T[,] target, int count, int size_x, int size_y) {
         T[,] result = new T[size_x + count, size_y];

         for (int i = 0; i < size_x; i++)
            for (int j = 0; j < size_y; j++)
               result[i + count, j] = target[i, j];

         return result;
      }

      private T[,] expandUp<T> (T[,] target, int count, int size_x, int size_y) {
         T[,] result = new T[size_x, size_y + count];

         for (int i = 0; i < size_x; i++)
            for (int j = 0; j < size_y; j++)
               result[i, j] = target[i, j];

         return result;
      }

      private T[,] expandDown<T> (T[,] target, int count, int size_x, int size_y) {
         T[,] result = new T[size_x, size_y + count];

         for (int i = 0; i < size_x; i++)
            for (int j = 0; j < size_y; j++)
               result[i, j + count] = target[i, j];

         return result;
      }

      public TileBase getTile (int x, int y) {
         (int x, int y) index = (x - origin.x, y - origin.y);
         if (index.x < 0 || index.x >= size.x || index.y < 0 || index.y >= size.y)
            return null;

         return tileMatrix[index.x, index.y];
      }
      public void setTile (int x, int y, TileBase tile) {
         setTile(new Vector3Int(x, y, 0), tile);
      }

      public static bool isVine (string layer) {
         return layer.CompareTo(VINE_KEY) == 0;
      }

      public static bool isMountain (string layer) {
         return layer.CompareTo(MOUNTAIN_KEY) == 0;
      }

      public static bool isWater (string layer) {
         return layer.CompareTo(WATER_KEY) == 0;
      }

      public static bool isDock (string layer) {
         return layer.CompareTo(DOCK_KEY) == 0;
      }

      public static bool isCeiling (string layer) {
         return layer.CompareTo(CEILING_KEY) == 0;
      }

      public static bool isWall (string layer) {
         return layer.CompareTo(WALL_KEY) == 0;
      }

      public static bool isDoorframe (string layer) {
         return layer.CompareTo(DOORFRAME_KEY) == 0;
      }

      public static bool isPath (string layer) {
         return layer.CompareTo(PATH_KEY) == 0;
      }

      public static bool isStair (string layer) {
         return layer.CompareTo(STAIR_KEY) == 0;
      }

      public static bool isRug (string layer) {
         return layer.CompareTo(RUG_KEY) == 0;
      }
   }
}