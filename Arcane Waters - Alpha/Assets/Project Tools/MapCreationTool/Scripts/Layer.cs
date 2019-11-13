using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
    public class Layer
    {
        public Layer[] SubLayers { get; private set; }

        private Tilemap tilemap;
        public Vector3Int Origin { get; private set; }
        public Vector3Int Size { get; private set; }

        private TileBase[,] tileMatrix = new TileBase[0, 0];
        private TileBase[,] previewTileMatrix = new TileBase[0, 0];

        public Layer(Tilemap tilemap)
        {
            this.tilemap = tilemap;
            SubLayers = new Layer[0];
        }
        public Layer(Tilemap[] subLayers)
        {
            tilemap = null;
            SubLayers = subLayers.Select(tl => new Layer(tl)).ToArray();
        }

        /// <summary>
        /// Checks whether the layer has a tilemap. Otherwise, the layer is only a container for sublayers
        /// </summary>
        public bool HasTilemap
        {
            get { return tilemap != null; }
        }

        public string TilemapGameObjectName
        {
            get { return tilemap.gameObject.name; }
        }

        public int TileCount
        {
            get 
            {
                if (tilemap == null)
                    return 0;

                int count = 0;
                for(int i = 0; i < Size.x; i++)
                {
                    for(int j = 0; j < Size.y; j++)
                    {
                        if (tileMatrix[i, j] != null)
                        {
                            count++;
                        }
                            
                    }
                }
                return count;
            }
        }

        public TileBase GetTile(Vector3Int position)
        {
            Vector3Int index = position - Origin;
            if (index.x < 0 || index.x >= Size.x || index.y < 0 || index.y >= Size.y)
                return null;

            return tileMatrix[index.x, index.y];
        }

        public void SetTile(Vector3Int position, TileBase tile)
        {
            if (tilemap == null)
                throw new System.InvalidOperationException("Layer is defined as a container, but is used as a tilemap!");

            Encapsulate(position);

            Vector3Int index = position - Origin;
            tileMatrix[index.x, index.y] = tile;
            tilemap.SetTile(position, tile);
            if (tile == null)
            {
                tilemap.SetTile(position, previewTileMatrix[index.x, index.y]);
            }
        }

        /// <summary>
        /// Given an array of tiles, calculates a change, that would revert it the array if it was applied
        /// </summary>
        /// <returns></returns>
        public BoardChange CalculateUndoChange(Vector3Int[] positions, TileBase[] tiles)
        {
            Encapsulate(positions);

            BoardChange result = new BoardChange();

            for(int i = 0; i < positions.Length; i++)
            {
                Vector3Int p = positions[i] - Origin;
                TileBase curTile = tileMatrix[p.x, p.y];

                if (curTile != tiles[i])
                    result.TileChanges.Add(new TileChange(curTile, positions[i], this));
            }

            return result;
        }

        /// <summary>
        /// Sets an array of tiles. 
        /// THE PROVIDED TILES ARRAY IS MODIFIED AND SHOULD NOT BE USED!
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="tiles"></param>
        public void SetTiles(Vector3Int[] positions, TileBase[] tiles)
        {
            Encapsulate(positions);

            for(int i = 0; i < positions.Length; i++)
            {
                int x = positions[i].x - Origin.x;
                int y = positions[i].y - Origin.y;
                
                tileMatrix[x, y] = tiles[i];

                if (tiles[i] == null)
                    tiles[i] = previewTileMatrix[x, y];
            }
                
            tilemap.SetTiles(positions, tiles);
        }

        public bool HasTile(Vector3Int position)
        {
            int x = position.x - Origin.x;
            int y = position.y - Origin.y;

            if (x < 0 || x >= Size.x || y < 0 || y >= Size.y)
                return false;

            return tileMatrix[x, y];
        }

        public bool HasTile(Vector2Int position)
        {
            int x = position.x - Origin.x;
            int y = position.y - Origin.y;

            if (x < 0 || x >= Size.x || y < 0 || y >= Size.y)
                return false;

            return tileMatrix[x, y];
        }

        public bool HasTile(int x, int y)
        {
            x -= Origin.x;
            y -= Origin.y;

            if (x < 0 || x >= Size.x || y < 0 || y >= Size.y)
                return false;

            return tileMatrix[x, y] != null;
        }

        public void ClearAllPreviewTiles()
        {
            for (int i = 0; i < previewTileMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < previewTileMatrix.GetLength(1); j++)
                {
                    if (previewTileMatrix[i, j] != null)
                    {
                        SetTilemapTile(new Vector3Int(i, j, 0) + Origin, GetTile(new Vector3Int(i, j, 0) + Origin));
                    }
                    previewTileMatrix[i, j] = null;
                }
            }
        }
        public void SetPreviewTile(Vector3Int position, TileBase tile)
        {
            if (tilemap == null)
                throw new System.InvalidOperationException("Layer is defined as a container, but is used as a tilemap!");

            Encapsulate(position);
            Vector3Int index = position - Origin;
            previewTileMatrix[index.x, index.y] = tile;
            if (tile != null)
                SetTilemapTile(position, tile);
            else
                SetTilemapTile(position, tileMatrix[index.x, index.y]);  
        }

        private void SetTilemapTile(Vector3Int position, TileBase tile)
        {
            if(tile is AnimatedTile)
                tilemap.SetTile(position, null);
            
            tilemap.SetTile(position, tile);
        }

        public Layer Default
        {
            get
            {
                if (tilemap != null)
                    return this;
                else
                    return SubLayers[0];
            }
        }

        /// <summary>
        /// Grows matrixes to include given position
        /// </summary>
        /// <param name="position"></param>
        private void Encapsulate(Vector3Int[] positions)
        {
            Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, 0);
            Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, 0);

            for(int i = 0; i < positions.Length; i++)
            {
                if (positions[i].x < min.x)
                    min.x = positions[i].x;
                if (positions[i].y < min.y)
                    min.y = positions[i].y;

                if (positions[i].x > max.x)
                    max.x = positions[i].x;
                if (positions[i].y > max.y)
                    max.y = positions[i].y;
            }

            Encapsulate(min);
            Encapsulate(max);
        }

        /// <summary>
        /// Grows matrixes to include given position
        /// </summary>
        /// <param name="position"></param>
        private void Encapsulate(Vector3Int position)
        {
            int index_x = position.x - Origin.x;
            int index_y = position.y - Origin.y;
            int size_x = Size.x;
            int size_y = Size.y;
            
            if (index_x < 0)
            {
                int missing = -index_x;

                tileMatrix = ExpandLeft(tileMatrix, missing, size_x, size_y);
                previewTileMatrix = ExpandLeft(previewTileMatrix, missing, size_x, size_y);

                size_x += missing;
                Origin = new Vector3Int(Origin.x - missing, Origin.y, 0);
            }
            else if (index_x >= size_x)
            {
                int missing = index_x - size_x + 1;

                tileMatrix = ExpandRight(tileMatrix, missing, size_x, size_y);
                previewTileMatrix = ExpandRight(previewTileMatrix, missing, size_x, size_y);

                size_x += missing;
            }

            if (index_y < 0)
            {
                int missing = -index_y;

                tileMatrix = ExpandDown(tileMatrix, missing, size_x, size_y);
                previewTileMatrix = ExpandDown(previewTileMatrix, missing, size_x, size_y);

                size_y += missing;
                Origin = new Vector3Int(Origin.x, Origin.y - missing, 0);
            }
            else if (index_y >= size_y)
            {
                int missing = index_y - size_y + 1;

                tileMatrix = ExpandUp(tileMatrix, missing, size_x, size_y);
                previewTileMatrix = ExpandUp(previewTileMatrix, missing, size_x, size_y);

                size_y += missing;
            }

            Size = new Vector3Int(size_x, size_y, 0);
        }

        private T[,] ExpandRight<T>(T[,] target, int count, int size_x, int size_y)
        {
            T[,] result = new T[size_x + count, size_y];

            for (int i = 0; i < size_x; i++)
                for (int j = 0; j < size_y; j++)
                    result[i, j] = target[i, j];

            return result;
        }

        private T[,] ExpandLeft<T>(T[,] target, int count, int size_x, int size_y)
        {
            T[,] result = new T[size_x + count, size_y];

            for (int i = 0; i < size_x; i++)
                for (int j = 0; j < size_y; j++)
                    result[i + count, j] = target[i, j];

            return result;
        }

        private T[,] ExpandUp<T>(T[,] target, int count, int size_x, int size_y)
        {
            T[,] result = new T[size_x, size_y + count];

            for (int i = 0; i < size_x; i++)
                for (int j = 0; j < size_y; j++)
                    result[i,j] = target[i,j];

            return result;
        }

        private T[,] ExpandDown<T>(T[,] target, int count, int size_x, int size_y)
        {
            T[,] result = new T[size_x, size_y + count];

            for (int i = 0; i < size_x; i++)
                for (int j = 0; j < size_y; j++)
                    result[i, j + count] = target[i, j];

            return result;
        }

        public TileBase GetTile(int x, int y)
        {
            return GetTile(new Vector3Int(x, y, 0));
        }
        public void SetTile(int x, int y, TileBase tile)
        {
            SetTile(new Vector3Int(x, y, 0), tile);
        }
    }
}