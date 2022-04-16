﻿using MapCreationTool.PaletteTilesData;
using System;
using MapCreationTool.UndoSystem;
using UnityEngine;
using System.Collections.Generic;
using MapCreationTool.Serialization;
using MapObjectStateVariables;

namespace MapCreationTool
{
   public class Tools
   {
      public const int MaxFloodFillTileCount = 65536;
      public const int MAX_BOARD_SIZE = 256;

      public static event Action<ToolType, ToolType> ToolChanged;
      public static event Action<int, int> MountainLayerChanged;
      public static event Action<bool, bool> BurrowedTreesChanged;
      public static event Action<Biome.Type, Biome.Type> BiomeChanged;
      public static event Action<TileGroup, TileGroup> TileGroupChanged;
      public static event Action<FillBounds, FillBounds> FillBoundsChanged;
      public static event Action<EditorType, EditorType> EditorTypeChanged;
      public static event Action<Vector2Int, Vector2Int> BoardSizeChanged;
      public static event Action<bool, bool> SnapToGridChanged;
      public static event Action<SelectionTarget, SelectionTarget> SelectionTargetChanged;

      public static event Action AnythingChanged;

      public static ToolType toolType { get; private set; }
      public static int mountainLayer { get; private set; }
      public static bool burrowedTrees { get; private set; }
      public static Biome.Type biome { get; private set; }
      public static FillBounds fillBounds { get; private set; }
      public static EditorType editorType { get; private set; }
      public static SelectionTarget selectionTarget { get; private set; }

      public static TileGroup tileGroup { get; private set; }

      public static Vector2Int boardSize { get; private set; }
      public static bool snapToGrid { get; set; }

      // Default data to set for prefabs when placing them, indexed by the type of prefab
      public static Dictionary<Type, Dictionary<string, string>> defaultPrefabData = new Dictionary<Type, Dictionary<string, string>>();

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

      // Gets the default prefab data which should be applied to prefabs when placing them
      public static Dictionary<string, string> getDefaultData (PrefabGroup group) {
         try {
            // Find out the type of prefab
            Type type = group.getPrefab().GetComponent<IPrefabDataListener>()?.GetType();

            Dictionary<string, string> result = new Dictionary<string, string>();

            // Get the data for this type
            if (type != null && defaultPrefabData.TryGetValue(type, out var data)) {
               result = data.Clone();
            }

            // Special default data for map state variables
            if (group.getPrefab().TryGetComponent(out PrefabDataDefinition pdd)) {
               if (pdd.hasVariableObjectState) {
                  string state = "ERROR";
                  if (pdd.validObjectStateValues.Length > 0) {
                     state = pdd.validObjectStateValues[0];
                  }

                  ObjectStateModel model = new ObjectStateModel {
                     state = state,
                     stateExpression = new Expression { type = Expression.Type.Constant, constant = state }
                  };
                  result.Add(DataField.MAP_OBJECT_STATE_MODEL_KEY, model.serialize());
               }
            }

            return result;
         } catch (Exception ex) {
            D.warning("Unable to get default prefab data: " + ex);
         }

         return null;
      }

      public static void setDefaultData (GameObject prefab, string key, string value) {
         // We do not want a default value for some of the fields
         if (key.Equals(DataField.PLACED_PREFAB_ID) || key.Equals(DataField.MAP_OBJECT_STATE_MODEL_KEY)) return;

         try {
            // Find out the type of prefab
            Type type = prefab.GetComponent<IPrefabDataListener>().GetType();

            // Set the data for this type
            if (!defaultPrefabData.ContainsKey(type)) {
               defaultPrefabData.Add(type, new Dictionary<string, string>());
            }

            if (defaultPrefabData[type].ContainsKey(key)) {
               defaultPrefabData[type][key] = value;
            } else {
               defaultPrefabData[type].Add(key, value);
            }

            // Update prefab in palette
            if (Palette.instance != null) {
               Palette.instance.updatePrefabData(prefab, key, value);
            }
         } catch (Exception ex) {
            D.warning("Unable to set default prefab data: " + ex);
         }
      }

      public static void setDefaultValues () {
         toolType = ToolType.Brush;
         mountainLayer = 4;
         burrowedTrees = false;
         biome = Biome.Type.Forest;
         editorType = EditorType.Area;
         boardSize = new Vector2Int(64, 64);
         snapToGrid = false;
         selectionTarget = SelectionTarget.Both;
      }

      public static void performUndoRedo (UndoRedoData undoRedoData) {
         ToolUndoRedoData data = undoRedoData as ToolUndoRedoData;

         toolType = data.toolType ?? toolType;
         mountainLayer = data.mountainLayer ?? mountainLayer;
         burrowedTrees = data.burrowedTrees ?? burrowedTrees;
         fillBounds = data.fillBounds ?? fillBounds;
         snapToGrid = data.snapToGrid ?? snapToGrid;
         selectionTarget = data.selectionTarget ?? selectionTarget;

         if (data.biome != null) {
            changeBiome(data.biome.Value, false);
         }

         if (data.hasTileGroup) {
            tileGroup = data.tileGroup;
         }

         AnythingChanged?.Invoke();
      }


      public static void changeTool (ToolType tool, bool registerUndo = true) {
         ToolType oldTool = toolType;
         toolType = tool;

         TileGroup oldGroup = tileGroup;
         if (oldTool != toolType && toolType == ToolType.Fill) {
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

      public static void changeBiome (Biome.Type biome, bool registerUndo = true) {
         Biome.Type old = Tools.biome;
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

      public static void changeSnapToGrid (bool enabled, bool registerUndo = true) {
         bool old = snapToGrid;
         snapToGrid = enabled;
         SnapToGridChanged?.Invoke(old, snapToGrid);
         AnythingChanged?.Invoke();

         if (registerUndo) {
            Undo.register(
                performUndoRedo,
                new ToolUndoRedoData { snapToGrid = old },
                new ToolUndoRedoData { snapToGrid = snapToGrid });
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

         ToolType oldTool = toolType;
         if (tileGroup != null && toolType != ToolType.Fill) {
            toolType = ToolType.Brush;
            ToolChanged?.Invoke(oldTool, toolType);
         }

         AnythingChanged?.Invoke();

         if (registerUndo) {
            Undo.register(
                performUndoRedo,
                new ToolUndoRedoData { hasTileGroup = true, tileGroup = oldgroup, toolType = oldTool },
                new ToolUndoRedoData { hasTileGroup = true, tileGroup = tileGroup, toolType = toolType });
         }
      }

      public static void changeEditorType (EditorType type) {
         EditorType oldType = editorType;
         editorType = type;


         if (tileGroup != null) {
            TileGroup oldGroup = tileGroup;
            tileGroup = null;

            TileGroupChanged?.Invoke(oldGroup, tileGroup);
         }

         EditorTypeChanged?.Invoke(oldType, editorType);
         AnythingChanged?.Invoke();

         Undo.clear();
      }

      public static void changeSelectionTarget (SelectionTarget type, bool registerUndo = true) {
         SelectionTarget oldType = selectionTarget;
         selectionTarget = type;

         SelectionTargetChanged?.Invoke(oldType, selectionTarget);
         AnythingChanged?.Invoke();

         if (registerUndo) {
            Undo.register(
                performUndoRedo,
                new ToolUndoRedoData { selectionTarget = oldType },
                new ToolUndoRedoData { selectionTarget = selectionTarget });
         }
      }

      public static void changeBoardSize (Vector2Int size) {
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
      Selection = 3,
      Move = 4
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

   public enum SelectionTarget
   {
      Both = 1,
      Tiles = 2,
      Prefabs = 3
   }
}
