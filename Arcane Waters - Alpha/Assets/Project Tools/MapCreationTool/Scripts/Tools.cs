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
      public static event Action<BiomeType, BiomeType> BiomeChanged;
      public static event Action<EraserLayerMode, EraserLayerMode> EraserLayerModeChanged;
      public static event Action<TileGroup, TileGroup> TileGroupChanged;
      public static event Action<FillBounds, FillBounds> FillBoundsChanged;
      public static event Action<EditorType, EditorType> EditorTypeChanged;
      public static event Action<Vector2Int, Vector2Int> BoardSizeChanged;

      public static event Action AnythingChanged;

      public static ToolType toolType { get; private set; }
      public static int mountainLayer { get; private set; }
      public static bool burrowedTrees { get; private set; }
      public static BiomeType biome { get; private set; }
      public static EraserLayerMode eraserLayerMode { get; private set; }
      public static FillBounds fillBounds { get; private set; }

      public static EditorType editorType { get; private set; }

      public static TileGroup tileGroup { get; private set; }

      public static Vector2Int boardSize { get; private set; }

      public static GameObject selectedPrefab
      {
         get
         {
            if (tileGroup == null)
               return null;
            if (tileGroup.type == TileGroupType.Prefab)
               return (tileGroup as PrefabGroup).refPref;
            if (tileGroup.type == TileGroupType.TreePrefab) {
               return burrowedTrees
                   ? (tileGroup as TreePrefabGroup).burrowedPref
                   : (tileGroup as TreePrefabGroup).refPref;
            }
            return null;
         }
      }

      public static void setDefaultValues () {
         toolType = ToolType.Brush;
         mountainLayer = 4;
         burrowedTrees = false;
         biome = BiomeType.Forest;
         eraserLayerMode = EraserLayerMode.Top;
         editorType = EditorType.Area;
         boardSize = new Vector2Int(64, 64);
      }

      public static void performUndoRedo (UndoRedoData undoRedoData) {
         ToolUndoRedoData data = undoRedoData as ToolUndoRedoData;

         toolType = data.toolType ?? toolType;
         mountainLayer = data.mountainLayer ?? mountainLayer;
         burrowedTrees = data.burrowedTrees ?? burrowedTrees;
         fillBounds = data.fillBounds ?? fillBounds;

         if (data.biome != null) {
            changeBiome(data.biome.Value, false);
         }

         eraserLayerMode = data.eraserLayerMode ?? eraserLayerMode;

         if (data.hasTileGroup) {
            tileGroup = data.tileGroup;
         }

         AnythingChanged?.Invoke();
      }


      public static void changeTool (ToolType tool, bool registerUndo = true) {
         ToolType oldTool = toolType;
         toolType = tool;

         TileGroup oldGroup = tileGroup;
         if (oldTool != toolType && (toolType == ToolType.Eraser || toolType == ToolType.Fill)) {
            tileGroup = null;
         }

         ToolChanged?.Invoke(oldTool, toolType);
         AnythingChanged?.Invoke();

         if (registerUndo) {
            Undo.register(
                performUndoRedo,
                new ToolUndoRedoData { toolType = oldTool, hasTileGroup = true, tileGroup = oldGroup },
                new ToolUndoRedoData { toolType = toolType, hasTileGroup = true, tileGroup = tileGroup });
         }
      }

      public static void changeMountainLayer (int layer, bool registerUndo = true) {
         int oldLayer = mountainLayer;
         mountainLayer = layer;

         MountainLayerChanged?.Invoke(oldLayer, mountainLayer);
         AnythingChanged?.Invoke();

         if (registerUndo) {
            Undo.register(
                performUndoRedo,
                new ToolUndoRedoData { mountainLayer = oldLayer },
                new ToolUndoRedoData { mountainLayer = mountainLayer });
         }
      }

      public static void changeBurrowedTrees (bool burrowedTrees, bool registerUndo = true) {
         bool old = Tools.burrowedTrees;
         Tools.burrowedTrees = burrowedTrees;

         BurrowedTreesChanged?.Invoke(old, Tools.burrowedTrees);
         AnythingChanged?.Invoke();

         if (registerUndo) {
            Undo.register(
                performUndoRedo,
                new ToolUndoRedoData { burrowedTrees = old },
                new ToolUndoRedoData { burrowedTrees = Tools.burrowedTrees });
         }
      }

      public static void changeBiome (BiomeType biome, bool registerUndo = true) {
         BiomeType old = Tools.biome;
         Tools.biome = biome;
         BiomeChanged?.Invoke(old, Tools.biome);
         AnythingChanged?.Invoke();

         if (registerUndo) {
            Undo.register(
                performUndoRedo,
                new ToolUndoRedoData { biome = old },
                new ToolUndoRedoData { biome = Tools.biome });
         }
      }

      public static void changeEraserLayerMode (EraserLayerMode mode, bool registerUndo = true) {
         EraserLayerMode old = eraserLayerMode;
         eraserLayerMode = mode;
         EraserLayerModeChanged?.Invoke(old, eraserLayerMode);
         AnythingChanged?.Invoke();

         if (registerUndo) {
            Undo.register(
                performUndoRedo,
                new ToolUndoRedoData { eraserLayerMode = old },
                new ToolUndoRedoData { eraserLayerMode = eraserLayerMode });
         }
      }

      public static void changeFillBounds (FillBounds fillBounds, bool registerUndo = true) {
         FillBounds old = Tools.fillBounds;
         Tools.fillBounds = fillBounds;
         FillBoundsChanged?.Invoke(old, Tools.fillBounds);
         AnythingChanged?.Invoke();

         if (registerUndo) {
            Undo.register(
                performUndoRedo,
                new ToolUndoRedoData { fillBounds = old },
                new ToolUndoRedoData { fillBounds = Tools.fillBounds });
         }
      }

      public static void changeTileGroup (TileGroup group, bool registerUndo = true) {
         //Check that if the tool if Fill, only 1 tile can be selected
         if (toolType == ToolType.Fill && group != null && group.maxTileCount != 1)
            return;

         TileGroup oldgroup = tileGroup;
         tileGroup = group;

         TileGroupChanged?.Invoke(oldgroup, tileGroup);
         AnythingChanged?.Invoke();

         ToolType oldTool = toolType;
         if (tileGroup != null && toolType == ToolType.Eraser)
            toolType = ToolType.Brush;

         if (registerUndo) {
            Undo.register(
                performUndoRedo,
                new ToolUndoRedoData { hasTileGroup = true, tileGroup = oldgroup, toolType = oldTool },
                new ToolUndoRedoData { hasTileGroup = true, tileGroup = tileGroup, toolType = toolType });
         }
      }

      public static void changeEditorType(EditorType type) {
         EditorType oldType = editorType;
         editorType = type;


         if(tileGroup != null) {
            TileGroup oldGroup = tileGroup;
            tileGroup = null;

            TileGroupChanged?.Invoke(oldGroup, tileGroup);
         }

         EditorTypeChanged?.Invoke(oldType, editorType);
         AnythingChanged?.Invoke();

         Undo.clear();
      }

      public static void changeBoardSize(Vector2Int size) {
         Vector2Int oldSize = boardSize;
         boardSize = size;

         BoardSizeChanged?.Invoke(oldSize, boardSize);
         AnythingChanged?.Invoke();

         Undo.clear();
      }
   }

   public enum ToolType
   {
      Brush = 0,
      Eraser = 1,
      Fill = 2,
      Selection = 3
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

   public enum EditorType
   {
      Area = 0,
      Interior = 1,
      Sea = 2
   }
}