using MapCreationTool.PaletteTilesData;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapCreationTool.UndoSystem
{
   public class Undo
   {
      public static event Action UndoRegisterd;
      public static event Action UndoPerformed;
      public static event Action RedoPerformed;
      public static event Action LogCleared;

      public const int LogLength = 10000;
      private static List<Entry> Log = new List<Entry>();

      private static int head;

      public static void register (Action<UndoRedoData> handler, UndoRedoData undoData, UndoRedoData redoData) {
         register(new Action<UndoRedoData>[] { handler }, undoData, redoData);
      }

      public static void register (Action<UndoRedoData>[] handlers, UndoRedoData undoData, UndoRedoData redoData) {
         undoData.isUndo = true;
         redoData.isUndo = false;

         Log.RemoveRange(head, Log.Count - head);

         if (Log.Count == LogLength)
            Log.RemoveAt(0);

         Log.Add(new Entry {
            handlers = handlers,
            undoData = undoData,
            redoData = redoData
         });

         head = Log.Count;

         UndoRegisterd?.Invoke();
      }

      public static void doUndo () {
         int entiresHandled = 0;

         while (undoCount > 0) {
            foreach (var handler in Log[head - 1].handlers)
               handler(Log[head - 1].undoData);

            head--;
            entiresHandled++;

            if (!Log[head].undoData.cascade) {
               break;
            }
         }

         UndoPerformed?.Invoke();
      }

      public static void doRedo () {
         int entiresHandled = 0;

         while (redoCount > 0) {
            foreach (var handler in Log[head].handlers)
               handler(Log[head].redoData);

            head++;
            entiresHandled++;

            if (redoCount == 0 || !Log[head].redoData.cascade) {
               break;
            }
         }

         RedoPerformed?.Invoke();
      }

      public static int undoCount
      {
         get { return head; }
      }

      public static int redoCount
      {
         get { return Log.Count - head; }
      }

      public static void clear () {
         Log.Clear();
         head = 0;
         LogCleared?.Invoke();
      }
   }

   public class Entry
   {
      public Action<UndoRedoData>[] handlers { get; set; }
      public UndoRedoData undoData { get; set; }
      public UndoRedoData redoData { get; set; }
   }

   public class UndoRedoData
   {
      /// <summary>
      /// When handling, whether to handle to adjacent entriers as well
      /// </summary>
      public bool cascade { get; set; }
      public bool isUndo { get; set; }
   }

   public class BoardUndoRedoData : UndoRedoData
   {
      public BoardChange change { get; set; }
   }

   public class PrefabDataUndoRedoData : UndoRedoData
   {
      public GameObject prefab { get; set; }
      public Vector3 position { get; set; }
      public string key { get; set; }
      public string value { get; set; }
   }

   public class ToolUndoRedoData : UndoRedoData
   {
      public ToolType? toolType { get; set; }
      public int? mountainLayer { get; set; }
      public bool? burrowedTrees { get; set; }
      public bool? individualTiles { get; set; }
      public Biome.Type? biome { get; set; }
      public FillBounds? fillBounds { get; set; }
      public bool? snapToGrid { get; set; }
      public SelectionTarget? selectionTarget { get; set; }

      public bool hasTileGroup { get; set; }
      public TileGroup tileGroup { get; set; }
   }
}
