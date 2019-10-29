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

        private List<List<TileBase>> tileMatrix = new List<List<TileBase>>();
        private List<List<TileBase>> previewTileMatrix = new List<List<TileBase>>();

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
                        if (tileMatrix[i][j] != null)
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
            if (tilemap == null)
                throw new System.InvalidOperationException("Layer is defined as a container, but is used as a tilemap!");

            Vector3Int index = position - Origin;
            if (index.x < 0 || index.x >= Size.x || index.y < 0 || index.y >= Size.y)
                return null;

            return tileMatrix[index.x][index.y];
        }
        public void SetTile(Vector3Int position, TileBase tile)
        {
            if (tilemap == null)
                throw new System.InvalidOperationException("Layer is defined as a container, but is used as a tilemap!");

            Encapsulate(position);

            Vector3Int index = position - Origin;
            tileMatrix[index.x][index.y] = tile;
            tilemap.SetTile(position, tile);
            if (tile == null)
            {
                tilemap.SetTile(position, previewTileMatrix[index.x][index.y]);
            }
        }
        public bool HasTile(Vector3Int position)
        {
            if (tilemap == null)
                throw new System.InvalidOperationException("Layer is defined as a container, but is used as a tilemap!");

            return GetTile(position) != null;
        }
        public void ClearAllPreviewTiles()
        {
            for (int i = 0; i < previewTileMatrix.Count; i++)
            {
                for (int j = 0; j < previewTileMatrix[i].Count; j++)
                {
                    if (previewTileMatrix[i][j] != null)
                    {
                        SetTilemapTile(new Vector3Int(i, j, 0) + Origin, GetTile(new Vector3Int(i, j, 0) + Origin));
                    }
                    previewTileMatrix[i][j] = null;
                }
            }
        }
        public void SetPreviewTile(Vector3Int position, TileBase tile)
        {
            if (tilemap == null)
                throw new System.InvalidOperationException("Layer is defined as a container, but is used as a tilemap!");

            Encapsulate(position);
            Vector3Int index = position - Origin;
            previewTileMatrix[index.x][index.y] = tile;
            if (tile != null)
                SetTilemapTile(position, tile);
            else
                SetTilemapTile(position, tileMatrix[index.x][index.y]);  
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
        private void Encapsulate(Vector3Int position)
        {
            Vector3Int index = position - Origin;

            if (index.x < 0)
            {
                int missing = -index.x;
                tileMatrix.InsertRange(0, Enumerable.Range(0, missing).Select(n => Enumerable.Repeat<TileBase>(null, Size.y).ToList()));
                previewTileMatrix.InsertRange(0, Enumerable.Range(0, missing).Select(n => Enumerable.Repeat<TileBase>(null, Size.y).ToList()));

                index.x = 0;
                Size = new Vector3Int(Size.x + missing, Size.y, 0);
                Origin = new Vector3Int(Origin.x - missing, Origin.y, 0);
            }
            else if (index.x >= Size.x)
            {
                int missing = index.x - Size.x + 1;
                tileMatrix.AddRange(Enumerable.Range(0, missing).Select(n => Enumerable.Repeat<TileBase>(null, Size.y).ToList()));
                previewTileMatrix.AddRange(Enumerable.Range(0, missing).Select(n => Enumerable.Repeat<TileBase>(null, Size.y).ToList()));

                Size = new Vector3Int(Size.x + missing, Size.y, 0);
            }

            if (index.y < 0)
            {
                int missing = -index.y;
                for (int i = 0; i < tileMatrix.Count; i++)
                    tileMatrix[i].InsertRange(0, Enumerable.Repeat<TileBase>(null, missing));
                for (int i = 0; i < previewTileMatrix.Count; i++)
                    previewTileMatrix[i].InsertRange(0, Enumerable.Repeat<TileBase>(null, missing));

                index.y = 0;
                Size = new Vector3Int(Size.x, Size.y + missing, 0);
                Origin = new Vector3Int(Origin.x, Origin.y - missing, 0);
            }
            else if (index.y >= Size.y)
            {
                int missing = index.y - Size.y + 1;

                for (int i = 0; i < tileMatrix.Count; i++)
                    tileMatrix[i].AddRange(Enumerable.Repeat<TileBase>(null, missing));
                for (int i = 0; i < tileMatrix.Count; i++)
                    previewTileMatrix[i].AddRange(Enumerable.Repeat<TileBase>(null, missing));

                Size = new Vector3Int(Size.x, Size.y + missing, 0);
            }
        }
        public bool HasTile(int x, int y)
        {
            return HasTile(new Vector3Int(x, y, 0));
        }
        public TileBase GetTile(int x, int y)
        {
            return GetTile(new Vector3Int(x, y, 0));
        }
        public void SetTile(int x, int y, TileBase tile)
        {
            SetTile(new Vector3Int(x, y, 0), tile);
        }
        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            return new Vector3Int(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y), 0);
        }
    }
}