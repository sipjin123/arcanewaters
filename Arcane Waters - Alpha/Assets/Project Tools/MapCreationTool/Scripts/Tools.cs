using MapCreationTool.PaletteTilesData;
using System;
using MapCreationTool.UndoSystem;
using UnityEngine;
namespace MapCreationTool
{
    public class Tools
    {
        public const int MaxFloodFillTileCount = 65536;

        public static event Action<ToolType, ToolType> ToolChanged;
        public static event Action<int, int> MountainLayerChanged;
        public static event Action<bool, bool> BurrowedTreesChanged;
        public static event Action<bool, bool> IndividualTilesChanged;
        public static event Action<BiomeType, BiomeType> BiomeChanged;
        public static event Action<EraserLayerMode, EraserLayerMode> EraserLayerModeChanged;
        public static event Action<TileGroup, TileGroup> TileGroupChanged;
        public static event Action<FillBounds, FillBounds> FillBoundsChanged;

        public static event Action AnythingChanged;

        public static ToolType ToolType { get; private set; }
        public static int MountainLayer { get; private set; }
        public static bool BurrowedTrees { get; private set; }
        public static bool IndividualTiles { get; private set; }
        public static BiomeType Biome { get; private set; }
        public static EraserLayerMode EraserLayerMode { get; private set; }
        public static FillBounds FillBounds { get; private set; }

        public static TileGroup TileGroup { get; private set; }
        //public static Vector2Int? TileIndex { get; private set; }

        public static GameObject SelectedPrefab
        {
            get
            {
                if (TileGroup == null)
                    return null;
                if (TileGroup.Type == TileGroupType.Prefab)
                    return (TileGroup as PrefabGroup).RefPref;
                if (TileGroup.Type == TileGroupType.TreePrefab)
                {
                    return BurrowedTrees
                        ? (TileGroup as TreePrefabGroup).BurrowedPref
                        : (TileGroup as TreePrefabGroup).RefPref;
                }
                return null;
            }
        }

        public static void SetDefaultValues()
        {
            ToolType = ToolType.Brush;
            MountainLayer = 4;
            BurrowedTrees = false;
            IndividualTiles = false;
            Biome = BiomeType.Forest;
            EraserLayerMode = EraserLayerMode.Top;
        }

        public static void PerformUndoRedo(UndoRedoData undoRedoData)
        {
            ToolUndoRedoData data = undoRedoData as ToolUndoRedoData;

            ToolType = data.ToolType ?? ToolType;
            MountainLayer = data.MountainLayer ?? MountainLayer;
            BurrowedTrees = data.BurrowedTrees ?? BurrowedTrees;
            IndividualTiles = data.IndividualTiles ?? IndividualTiles;
            FillBounds = data.FillBounds ?? FillBounds;

            if (data.Biome != null)
            {
                ChangeBiome(data.Biome.Value, false);
            }

            EraserLayerMode = data.EraserLayerMode ?? EraserLayerMode;

            if (data.HasTileGroup)
            {
                TileGroup = data.TileGroup;
            }

            AnythingChanged?.Invoke();
        }


        public static void ChangeTool(ToolType tool, bool registerUndo = true)
        {
            ToolType oldTool = ToolType;
            ToolType = tool;

            TileGroup oldGroup = TileGroup;
            if (oldTool != ToolType && (ToolType == ToolType.Eraser || ToolType == ToolType.Fill))
            {
                TileGroup = null;
            }

            ToolChanged?.Invoke(oldTool, ToolType);
            AnythingChanged?.Invoke();

            if (registerUndo)
            {
                Undo.Register(
                    PerformUndoRedo,
                    new ToolUndoRedoData { ToolType = oldTool, HasTileGroup = true, TileGroup = oldGroup },
                    new ToolUndoRedoData { ToolType = ToolType, HasTileGroup = true, TileGroup = TileGroup });
            }
        }

        public static void ChangeMountainLayer(int layer, bool registerUndo = true)
        {
            int oldLayer = MountainLayer;
            MountainLayer = layer;

            MountainLayerChanged?.Invoke(oldLayer, MountainLayer);
            AnythingChanged?.Invoke();

            if (registerUndo)
            {
                Undo.Register(
                    PerformUndoRedo,
                    new ToolUndoRedoData { MountainLayer = oldLayer },
                    new ToolUndoRedoData { MountainLayer = MountainLayer });
            }
        }

        public static void ChangeBurrowedTrees(bool burrowedTrees, bool registerUndo = true)
        {
            bool old = BurrowedTrees;
            BurrowedTrees = burrowedTrees;

            BurrowedTreesChanged?.Invoke(old, BurrowedTrees);
            AnythingChanged?.Invoke();

            if (registerUndo)
            {
                Undo.Register(
                    PerformUndoRedo,
                    new ToolUndoRedoData { BurrowedTrees = old },
                    new ToolUndoRedoData { BurrowedTrees = BurrowedTrees });
            }
        }

        public static void ChangeIndividualTiles(bool individualTiles, bool registerUndo = true)
        {
            bool old = IndividualTiles;
            IndividualTiles = individualTiles;

            TileGroup oldTileGroup = TileGroup;
            TileGroup = null;

            IndividualTilesChanged?.Invoke(old, IndividualTiles);
            AnythingChanged?.Invoke();


            if (registerUndo)
            {
                Undo.Register(
                    PerformUndoRedo,
                    new ToolUndoRedoData { IndividualTiles = old, HasTileGroup = true, TileGroup = oldTileGroup },
                    new ToolUndoRedoData { IndividualTiles = IndividualTiles, HasTileGroup = true, TileGroup = TileGroup });
            }
        }

        public static void ChangeBiome(BiomeType biome, bool registerUndo = true)
        {
            BiomeType old = Biome;
            Biome = biome;
            BiomeChanged?.Invoke(old, Biome);
            AnythingChanged?.Invoke();

            if (registerUndo)
            {
                Undo.Register(
                    PerformUndoRedo,
                    new ToolUndoRedoData { Biome = old },
                    new ToolUndoRedoData { Biome = Biome });
            }
        }

        public static void ChangeEraserLayerMode(EraserLayerMode mode, bool registerUndo = true)
        {
            EraserLayerMode old = EraserLayerMode;
            EraserLayerMode = mode;
            EraserLayerModeChanged?.Invoke(old, EraserLayerMode);
            AnythingChanged?.Invoke();

            if (registerUndo)
            {
                Undo.Register(
                    PerformUndoRedo,
                    new ToolUndoRedoData { EraserLayerMode = old },
                    new ToolUndoRedoData { EraserLayerMode = EraserLayerMode });
            }
        }

        public static void ChangeFillBounds(FillBounds fillBounds, bool registerUndo = true)
        {
            FillBounds old = FillBounds;
            FillBounds = fillBounds;
            FillBoundsChanged?.Invoke(old, FillBounds);
            AnythingChanged?.Invoke();

            if (registerUndo)
            {
                Undo.Register(
                    PerformUndoRedo,
                    new ToolUndoRedoData { FillBounds = old },
                    new ToolUndoRedoData { FillBounds = FillBounds });
            }
        }

        public static void ChangeTileGroup(TileGroup group, bool registerUndo = true)
        {
            //Check that if the tool if Fill, only 1 tile can be selected
            if (ToolType == ToolType.Fill && group != null && group.MaxTileCount != 1)
                return;

            TileGroup oldgroup = TileGroup;
            TileGroup = group;

            TileGroupChanged?.Invoke(oldgroup, TileGroup);
            AnythingChanged?.Invoke();

            ToolType oldTool = ToolType;
            if (TileGroup != null && ToolType == ToolType.Eraser)
                ToolType = ToolType.Brush;

            if (registerUndo)
            {
                Undo.Register(
                    PerformUndoRedo,
                    new ToolUndoRedoData { HasTileGroup = true, TileGroup = oldgroup, ToolType = oldTool },
                    new ToolUndoRedoData { HasTileGroup = true, TileGroup = TileGroup, ToolType = ToolType });
            }
        }
    }

    public enum ToolType
    {
        Brush = 0,
        Eraser = 1,
        Fill = 2
    }

    public enum EraserLayerMode
    {
        Top = 0,
        All = 1
    }

    public enum FillBounds
    {
        SingleLayer = 0,
        AllLayers = 1
    }
}