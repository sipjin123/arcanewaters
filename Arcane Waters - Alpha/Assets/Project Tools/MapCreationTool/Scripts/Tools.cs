using MapCreationTool.PaletteTilesData;
using System;
using MapCreationTool.UndoSystem;
using UnityEngine;
namespace MapCreationTool
{
    public class Tools
    {
        public static event Action<ToolType, ToolType> ToolChanged;
        public static event Action<int, int> MountainLayerChanged;
        public static event Action<bool, bool> BurrowedTreesChanged;
        public static event Action<bool, bool> IndividualTilesChanged;
        public static event Action<BiomeType, BiomeType> BiomeChanged;
        public static event Action<EraserLayerMode, EraserLayerMode> EraserLayerModeChanged;
        public static event Action<TileGroup, TileGroup> TileGroupChanged;

        public static event Action AnythingChanged;

        public static ToolType ToolType { get; private set; }
        public static int MountainLayer { get; private set; }
        public static bool BurrowedTrees { get; private set; }
        public static bool IndividualTiles { get; private set; }
        public static BiomeType Biome { get; private set; }
        public static EraserLayerMode EraserLayerMode { get; private set; }

        public static TileGroup TileGroup { get; private set; }
        public static Vector2Int? TileIndex { get; private set; }

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

            if (data.Biome != null)
            {
                ChangeBiome(data.Biome.Value, false);
            }

            EraserLayerMode = data.EraserLayerMode ?? EraserLayerMode;

            if (data.HasTileGroup)
            {
                TileGroup = data.TileGroup;
                TileIndex = data.TileIndex;
            }

            AnythingChanged?.Invoke();
        }


        public static void ChangeTool(ToolType tool, bool registerUndo = true)
        {
            ToolType oldTool = ToolType;
            ToolType = tool;

            TileGroup oldGroup = TileGroup;
            Vector2Int? oldIndex = TileIndex;
            if (oldTool != ToolType.Eraser && ToolType == ToolType.Eraser)
            {
                TileGroup = null;
                TileIndex = null;
            }

            ToolChanged?.Invoke(oldTool, ToolType);
            AnythingChanged?.Invoke();

            if (registerUndo)
            {
                Undo.Register(
                    PerformUndoRedo,
                    new ToolUndoRedoData { ToolType = oldTool, HasTileGroup = true, TileGroup = oldGroup, TileIndex = oldIndex },
                    new ToolUndoRedoData { ToolType = ToolType, HasTileGroup = true, TileGroup = TileGroup, TileIndex = TileIndex });
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
                    new ToolUndoRedoData { IndividualTiles = old, HasTileGroup = true, TileGroup = oldTileGroup, TileIndex = TileIndex },
                    new ToolUndoRedoData { IndividualTiles = IndividualTiles, HasTileGroup = true, TileGroup = TileGroup, TileIndex = null });
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

        public static void ChangeTileGroup(TileGroup group, Vector2Int? tile = null, bool registerUndo = true)
        {
            TileGroup oldgroup = TileGroup;
            TileGroup = group;
            Vector2Int? oldIndex = TileIndex;
            TileIndex = tile;

            TileGroupChanged?.Invoke(oldgroup, TileGroup);
            AnythingChanged?.Invoke();

            ToolType oldTool = ToolType;
            if (TileGroup != null && ToolType == ToolType.Eraser)
                ToolType = ToolType.Brush;

            if (registerUndo)
            {
                Undo.Register(
                    PerformUndoRedo,
                    new ToolUndoRedoData { HasTileGroup = true, TileGroup = oldgroup, TileIndex = oldIndex, ToolType = oldTool },
                    new ToolUndoRedoData { HasTileGroup = true, TileGroup = TileGroup, TileIndex = TileIndex, ToolType = ToolType });
            }
        }
    }

    public enum ToolType
    {
        Brush = 0,
        Eraser = 1
    }

    public enum EraserLayerMode
    {
        Top = 0,
        All = 1
    }
}