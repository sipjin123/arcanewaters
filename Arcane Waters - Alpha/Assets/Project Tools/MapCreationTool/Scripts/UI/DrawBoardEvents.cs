using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MapCreationTool
{
   /// <summary>
   /// The class catches, restructures and re-invokes UI events on the DrawBoard.
   /// All positions are in world space.
   /// </summary>
   public class DrawBoardEvents : MonoBehaviour
   {
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
         if (isHovering && Input.mouseScrollDelta.y != 0) {
            PointerScroll.Invoke(lastHoverPosition.Value, Input.mouseScrollDelta.y);
         }

         // Check for movement
         Vector2 mPos = MainCamera.stwp(Input.mousePosition);
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
            draggingFrom = data.pointerCurrentRaycast.worldPosition;
            draggingFromCell = DrawBoard.worldToCell(draggingFrom.Value);
            lastHoverPositionCell = draggingFromCell;
            BeginDrag?.Invoke(data.pointerCurrentRaycast.worldPosition);
         }
      }

      private void drag (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left) {
            if (draggingFrom != null) {
               Drag?.Invoke(draggingFrom.Value, data.pointerCurrentRaycast.worldPosition);

               Vector3Int cellPos = DrawBoard.worldToCell(data.pointerCurrentRaycast.worldPosition);
               if (cellPos != lastHoverPositionCell.Value) {
                  lastHoverPositionCell = cellPos;
                  DragCell?.Invoke(draggingFromCell.Value, cellPos);
               }
            }
         } else {
            Vector3 curPos = MainCamera.stwp(data.position);
            Vector3 prevPos = MainCamera.stwp(data.position - data.delta);

            DragSecondary?.Invoke(curPos - prevPos);
         }
      }

      private void endDrag (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left) {
            if (draggingFrom != null) {
               EndDrag?.Invoke(draggingFrom.Value, data.pointerCurrentRaycast.worldPosition);
            }
            draggingFrom = null;
            draggingFromCell = null;
         }
      }
   }
}
