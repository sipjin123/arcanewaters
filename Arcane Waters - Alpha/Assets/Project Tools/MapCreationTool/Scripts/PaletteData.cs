using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool.PaletteTilesData 
{
    public class PaletteData
    {
        public const int PathLayer = 2;
        public const int WaterLayer = 9;
        public const int MountainLayer = 6;
        public TileGroup[,] TileGroups { get; set; }
        public List<PrefabGroup> PrefabGroups { get; set; }
        public BiomeType? Type { get; set; }

        public TileData GetTile(int x, int y)
        {
            TileGroup group = GetGroup(x, y);
            if (group == null)
                return null;
            return group.Tiles[x - group.Start.x, y - group.Start.y];
        }

        public TileGroup GetGroup(int x, int y)
        {
            if (x < 0 || x >= TileGroups.GetLength(0) || y < 0 || y >= TileGroups.GetLength(1))
                return null;
            return TileGroups[x, y];
        }

        public Vector2Int IndexOf(TileBase tile)
        {
            for (int i = 0; i < TileGroups.GetLength(0); i++)
            {
                for (int j = 0; j < TileGroups.GetLength(1); j++)
                {
                    TileData tileData = GetTile(i, j);
                    if (tileData != null && tileData.Tile == tile)
                        return new Vector2Int(i, j);
                }
            }
            return new Vector2Int(-1, -1);
        }

        public IEnumerable<TileGroup> GetTileGroupsEnumerator()
        {
            HashSet<TileGroup> proccessed = new HashSet<TileGroup>();
            for(int i = 0; i < TileGroups.GetLength(0); i++)
            {
                for(int j = 0; j < TileGroups.GetLength(1); j++)
                {
                    if(TileGroups[i, j] != null && !proccessed.Contains(TileGroups[i,j]))
                    {
                        proccessed.Add(TileGroups[i, j]);
                        yield return TileGroups[i, j];
                    }
                }
            }
        }

        public void Merge(PaletteData p)
        {
            TileGroup[,] newG = new TileGroup[
                Mathf.Max(TileGroups.GetLength(0), p.TileGroups.GetLength(0)),
                Mathf.Max(TileGroups.GetLength(1), p.TileGroups.GetLength(1))
            ];

            for (int i = 0; i < newG.GetLength(0); i++)
            {
                for (int j = 0; j < newG.GetLength(1); j++)
                {
                    TileGroup curGroup = i >= TileGroups.GetLength(0) || j >= TileGroups.GetLength(1) ? null : TileGroups[i, j];
                    TileGroup otherGroup = i >= p.TileGroups.GetLength(0) || j >= p.TileGroups.GetLength(1) ? null : p.TileGroups[i, j];

                    newG[i, j] = otherGroup ?? curGroup;
                }
            }

            List<PrefabGroup> newPrefs = new List<PrefabGroup>();
            HashSet<TileGroup> used = new HashSet<TileGroup>();

            for (int i = 0; i < newG.GetLength(0); i++)
            {
                for (int j = 0; j < newG.GetLength(1); j++)
                {
                    if(newG[i, j] != null && newG[i, j] is PrefabGroup && !used.Contains(newG[i, j]))
                    {
                        used.Add(newG[i, j]);
                        newPrefs.Add(newG[i, j] as PrefabGroup);
                    }
                }
            }

            TileGroups = newG;
            PrefabGroups = newPrefs;
        }
    }

    public class MountainGroup : TileGroup
    {
        public TileBase[,] InnerTiles { get; set; }
        public TileBase[,] OuterTiles { get; set; }
        public int Layer { get; set; }
        public MountainGroup()
        {
            Type = TileGroupType.Mountain;
        }
    }

    public class NineSliceInOutGroup : TileGroup
    {
        public TileData[,] InnerTiles { get; set; }
        public TileData[,] OuterTiles { get; set; }
        public NineSliceInOutGroup()
        {
            Type = TileGroupType.NineSliceInOut;
        }
    }

    public class PrefabGroup : TileGroup
    {
        public GameObject RefPref { get; set; }

        public PrefabGroup()
        {
            Type = TileGroupType.Prefab;
        }
    }

    public class TreePrefabGroup : PrefabGroup 
    {
        public GameObject BurrowedPref { get; set; }

        public TreePrefabGroup()
        {
            Type = TileGroupType.TreePrefab;
        }
    }


    public class NineFourGroup : TileGroup
    {
        public TileData[,] MainTiles { get; set; }
        public TileData[,] CornerTiles { get; set; }
        public int Layer { get; set; }

        public NineFourGroup()
        {
            Type = TileGroupType.NineFour;
        }
    }

    public class NineGroup : TileGroup
    {
        public int Layer { get; set; }

        public NineGroup() 
        {
            Type = TileGroupType.Nine;
        }
    }

    public class TileGroup
    {
        public TileData[,] Tiles { get; set; }
        public Vector3Int Start { get; set; }
        public TileGroupType Type { get; set; }

        public int MaxTileCount
        {
            get { return Tiles.GetLength(0) * Tiles.GetLength(1); }
        }

        public Vector2Int Size
        {
            get { return new Vector2Int(Tiles.GetLength(0), Tiles.GetLength(1)); }
        }

        public Vector2 CenterPoint
        {
            get { return new Vector2(Start.x + Size.x * 0.5f, Start.y + Size.y * 0.5f); }
        }

        public bool AllInLayer(int layer)
        {
            for (int i = 0; i < Tiles.GetLength(0); i++)
            {
                for (int j = 0; j < Tiles.GetLength(1); j++)
                {
                    if (Tiles[i, j] != null && Tiles[i, j].Layer != layer)
                        return false;
                }
            }
            return true;
        }

        public bool Contains(TileBase tile)
        {
            for (int i = 0; i < Tiles.GetLength(0); i++)
            {
                for (int j = 0; j < Tiles.GetLength(1); j++)
                {
                    if (Tiles[i, j] != null && Tiles[i, j].Tile == tile)
                        return true;
                }
            }
            return false;
        }
    }

    public enum TileGroupType
    {
        Regular,
        Prefab,
        NineSliceInOut,
        Nine,
        TreePrefab,
        Mountain,
        NineFour
    }

    public class TileData
    {
        public TileBase Tile { get; set; }
        public int Layer { get; set; }
    }
}
