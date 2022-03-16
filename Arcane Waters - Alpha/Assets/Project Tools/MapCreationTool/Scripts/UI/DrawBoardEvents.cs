using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MapCreationTool
{
   /// <summary>
   /// The class catches, restructures and re-invokes UI events on the DrawBoard.
   /// All positions are in world space.
   /// </summary>
   public class DrawBoardEvents : MonoBehaviour {
      // Reference to self
      public static DrawBoardEvents instance;

      public static event Action<Vector3> PointerClick;
      public static event Action<Vector3> PointerDown;
      public static event Action<Vector3> PointerUp;
      public static event Action<Vector3> PointerEnter;
      public static event Action<Vector3> PointerExit;
      public static event Action<Vector3> BeginDrag;
      public static event Action<Vector3, Vector3> Drag;
      public static event Action<Vector3> DragSecondary;
      // Only invoked when the cell position of the pointer changes
      public static event Action<Vector3Int, Vector3Int> DragCell;
      public static event Action<Vector3, Vector3> EndDrag;
      // Only invoked when the cell position of the pointer changes
      public static event Action<Vector3Int> PointerHoverMoveCell;
      public static event Action<Vector3> PointerHoverMove;
      public static event Action<Vector3, float> PointerScroll;
      public static Action CancelAction;

      public static Vector3? draggingFrom { get; private set; }
      private static Vector3Int? draggingFromCell = null;
      private static Vector3? lastHoverPosition = null;
      private static Vector3Int? lastHoverPositionCell = null;

      private void Awake () {
         EventTrigger eventTrigger = GetComponentInChildren<EventTrigger>();

         Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerClick, pointerClick);
         Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerDown, pointerDown);
         Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerUp, pointerUp);
         Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerEnter, pointerEnter);
         Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerExit, pointerExit);
         Utilities.addPointerListener(eventTrigger, EventTriggerType.BeginDrag, beginDrag);
         Utilities.addPointerListener(eventTrigger, EventTriggerType.Drag, drag);
         Utilities.addPointerListener(eventTrigger, EventTriggerType.EndDrag, endDrag);

         instance = this;
      }

      public static bool isDragging
      {
         get { return draggingFrom.HasValue; }
      }

      public static bool isHovering
      {
         get { return lastHoverPosition.HasValue; }
      }

      private void Update () {
         // Check from scrolling
         if (isHovering && MouseUtils.mouseScrollY != 0) {
            float mouseValue = MouseUtils.mouseScrollY;
            if (Util.isLinux()) {
               mouseValue *= -1;
            }
            PointerScroll.Invoke(lastHoverPosition.Value, mouseValue);
         }

         // Check for movement
         Vector2 mPos = MainCamera.stwp(MouseUtils.mousePosition);
         if (isHovering && (Vector2) lastHoverPosition.Value != mPos) {
            lastHoverPosition = mPos;
            if (!draggingFrom.HasValue) {
               PointerHoverMove?.Invoke(lastHoverPosition.Value);
            }

            Vector3Int cellPos = DrawBoard.worldToCell(lastHoverPosition.Value);
            if (cellPos != lastHoverPositionCell.Value) {
               lastHoverPositionCell = cellPos;
               if (!draggingFromCell.HasValue) {
                  PointerHoverMoveCell?.Invoke(cellPos);
               }
            }
         }
      }

      public void stopAllCoroutines () {
         StopAllCoroutines();
      }

      public IEnumerator simulateBrushAction (WorldMapSector worldMapData) {
         // Data setup
         BrushStroke stroke = BrushTool.instance.stroke;
         char cachedChar = 'x';
         char landRepresentation = '2';
         char seaRepresentation = '1';
         bool forceBreak = false;
         int widthOffset = 0;

         DateTime startTime = DateTime.UtcNow;
         int dataStart = 0;
         int waterValCount = 0;
         int landValCount = 0;

         Vector3Int seaTileCoord = new Vector3Int(8, 31, 1);
         Vector3Int landTileCoord = new Vector3Int(10, 33, 1);
         //Vector3Int landData = new Vector3Int(3, 14, 1);

         foreach (char charData in worldMapData.tilesString) {
            if (charData == landRepresentation) {
               landValCount++;
            } else {
               waterValCount++;
            }
         }

         // Update UI of progress panel
         D.debug("MapContent: {" + landValCount + "} {" + waterValCount + "}");
         WorldMapTranslator.instance.updateMapDataText("Generating map {" + WorldMapTranslator.instance.currentMapIndex + "}, end goal map is {" + WorldMapTranslator.instance.maxMapCounter + "}" +
            "\nLandTiles:{" + landValCount + "} WaterTiles:{" + waterValCount + "}");
         WorldMapTranslator.instance.triggerProgressPanel(true);

         // Offset setup
         float newHorizontalOffset = (0 - (worldMapData.w * .5f) - widthOffset);
         float newVerticalOffset = (0 - (worldMapData.h * .5f));
         int tileStringIndex = dataStart;
         Vector3Int targetClick = Vector3Int.zero;

         yield return new WaitForSeconds(2);

         for (int h = 0; h < worldMapData.h; h++) {
            // Coordinate Setup
            float verticalValue = newVerticalOffset + h;
            Vector3 tstartClick = new Vector3(newHorizontalOffset, verticalValue, 1);
            beginDragProcess(stroke, tstartClick);

            // Update Display
            string progressMessage = "Map {" + WorldMapTranslator.instance.currentMapIndex + "}\nProcessing Line {" + h + "} out of {" + worldMapData.h + "}\nTotal: " + (((float) h / (float) worldMapData.h) * 100).ToString("f1") + "%";
            WorldMapTranslator.instance.updateDisplayText(progressMessage);
            yield return new WaitForSeconds(.5f);

            for (int w = 0; w < worldMapData.w; w++) {
               float horizontalValue = newHorizontalOffset + w;
               targetClick = new Vector3Int((int) horizontalValue, (int) verticalValue, 1);

               // Tile selection
               if (tileStringIndex >= worldMapData.tilesString.Length) {
                  forceBreak = true;
                  break;
               }
               if (worldMapData.tilesString[tileStringIndex] == landRepresentation) {
                  if (cachedChar != landRepresentation) {
                     // End drag, change tile to land and begin drag again
                     endDragProcess(stroke, targetClick);
                     Palette.instance.simulateTileSelection(landTileCoord);
                     cachedChar = landRepresentation;
                     beginDragProcess(stroke, targetClick);
                  }
               } else {
                  if (cachedChar != seaRepresentation) {
                     // End drag, change tile to sea and begin drag again
                     endDragProcess(stroke, targetClick);
                     cachedChar = seaRepresentation;
                     Palette.instance.simulateTileSelection(seaTileCoord);
                     beginDragProcess(stroke, targetClick);
                  }
               }

               // Drag Tile
               if (draggingFrom != null) {
                  try {
                     processDrag(targetClick);
                  } catch {
                  }
               }

               tileStringIndex++;
               if (forceBreak) {
                  break;
               }
            }

            endDragProcess(stroke, targetClick);
            if (forceBreak) {
               break;
            }
         }

         WorldMapTranslator.instance.triggerProgressPanel(false);
         D.debug("Ended Map Generation in: " + ((DateTime.UtcNow.Subtract(startTime).TotalSeconds) / 60) + " mins");
         WorldMapTranslator.instance.simulateSave();
      }

      private void beginDragProcess (BrushStroke stroke, Vector3 tstartClick) {
         BrushTool.instance.newStroke(tstartClick);
         if (stroke.tileGroup != null && stroke.type != BrushStroke.Type.Prefab) {
            stroke.paintPosition(tstartClick);
         }
         processBeginDrag(tstartClick);
      }

      private void endDragProcess (BrushStroke stroke, Vector3 targetClick) {
         DrawBoard.instance.changeBoard(stroke.calculateTileChange());
         stroke.clear();
      }

      private void pointerClick (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left) {
            if (draggingFrom == null) {
               PointerClick?.Invoke(data.pointerCurrentRaycast.worldPosition);
            }
         }
      }

      private void pointerDown (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left) {
            PointerDown?.Invoke(data.pointerCurrentRaycast.worldPosition);
         }
      }

      private void pointerUp (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left) {
            PointerUp?.Invoke(data.pointerCurrentRaycast.worldPosition);
         }
      }

      private void pointerEnter (PointerEventData data) {
         lastHoverPosition = data.pointerCurrentRaycast.worldPosition;
         lastHoverPositionCell = DrawBoard.worldToCell(lastHoverPosition.Value);

         PointerEnter?.Invoke(data.pointerCurrentRaycast.worldPosition);
         PointerHoverMove?.Invoke(lastHoverPosition.Value);
      }

      private void pointerExit (PointerEventData data) {
         lastHoverPosition = null;
         lastHoverPositionCell = null;
         draggingFrom = null;
         PointerExit?.Invoke(data.pointerCurrentRaycast.worldPosition);
      }

      private void beginDrag (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left) {
            processBeginDrag(data.pointerCurrentRaycast.worldPosition);
         }
      }

      public void processBeginDrag (Vector3 targetVal) {
         draggingFrom = targetVal;
         draggingFromCell = DrawBoard.worldToCell(draggingFrom.Value);
         lastHoverPositionCell = draggingFromCell;
         BeginDrag?.Invoke(targetVal);
      }

      public void drag (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left) {
            if (draggingFrom != null) {
               processDrag(data.pointerCurrentRaycast.worldPosition);
            }
         } else {
            Vector3 curPos = MainCamera.stwp(data.position);
            Vector3 prevPos = MainCamera.stwp(data.position - data.delta);

            DragSecondary?.Invoke(curPos - prevPos);
         }
      }

      public void processDrag (Vector3 targetVal) {
         Drag?.Invoke(draggingFrom.Value, targetVal);

         Vector3Int cellPos = DrawBoard.worldToCell(targetVal);
         if (cellPos != lastHoverPositionCell.Value) {
            lastHoverPositionCell = cellPos;
            if (draggingFromCell == null) {
               D.debug("Missing dragging cell!");
            } else {
               DragCell?.Invoke(draggingFromCell.Value, cellPos);
            }
         }
      }

      private void endDrag (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left) {
            processEndDrag(data.pointerCurrentRaycast.worldPosition);
         }
      }

      public void processEndDrag (Vector3 targetVal) {
         if (draggingFrom != null) {
            EndDrag?.Invoke(draggingFrom.Value, targetVal);
         }
         draggingFrom = null;
         draggingFromCell = null;
      }
   }
}
