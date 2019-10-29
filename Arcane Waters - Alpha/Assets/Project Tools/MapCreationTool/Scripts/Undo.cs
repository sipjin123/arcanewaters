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

        public static void Register(Action<UndoRedoData> handler, UndoRedoData undoData, UndoRedoData redoData)
        {
            Register(new Action<UndoRedoData>[] { handler }, undoData, redoData);
        }

        public static void Register(Action<UndoRedoData>[] handlers, UndoRedoData undoData, UndoRedoData redoData)
        {
            Log.RemoveRange(head, Log.Count - head);

            if (Log.Count == LogLength)
                Log.RemoveAt(0);

            Log.Add(new Entry
            {
                Handlers = handlers,
                UndoData = undoData,
                RedoData = redoData
            });

            head = Log.Count;

            UndoRegisterd?.Invoke();
        }

        public static void DoUndo()
        {
            if(UndoCount > 0)
            {
                foreach (var handler in Log[head - 1].Handlers)
                    handler(Log[head - 1].UndoData);
                head--;

                UndoPerformed?.Invoke();
            }
        }

        public static void DoRedo()
        {
            if(RedoCount > 0)
            {
                foreach (var handler in Log[head].Handlers)
                    handler(Log[head].RedoData);

                head++;

                RedoPerformed?.Invoke();
            }
        }

        public static int UndoCount
        {
            get { return head; }
        }

        public static int RedoCount
        {
            get { return Log.Count - head; }
        }

        public static void Clear()
        {
            Log.Clear();
            head = 0;
            LogCleared?.Invoke();
        }
    }

    public class Entry
    {
        public Action<UndoRedoData>[] Handlers { get; set; }
        public UndoRedoData UndoData { get; set; }
        public UndoRedoData RedoData { get; set; }
    }

    public class UndoRedoData { }

    public class BoardUndoRedoData : UndoRedoData 
    { 
        public BoardChange Change { get; set; }
    }

    public class ToolUndoRedoData : UndoRedoData 
    {
        public ToolType? ToolType { get; set; }
        public int? MountainLayer { get; set; }
        public bool? BurrowedTrees { get; set; }
        public bool? IndividualTiles { get; set; }
        public BiomeType? Biome { get; set; }
        public EraserLayerMode? EraserLayerMode { get; set; }

        public bool HasTileGroup { get; set; }
        public TileGroup TileGroup { get; set; }
        public Vector2Int? TileIndex { get; set; }
    }
}
