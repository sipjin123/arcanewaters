using System.Collections.Generic;
using System.Linq;
using MapCreationTool.PaletteTilesData;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class BrushTool : Tool
   {
      private BrushStroke stroke = new BrushStroke();

      // If horizontal is the beginning behavior of the brush
      public bool hasPaintStarted = false;
      public bool isHorizontal = false;

      protected override ToolType toolType => ToolType.Brush;

      protected override void registerUIEvents () {
         DrawBoardEvents.PointerDown += pointerDown;
         DrawBoardEvents.PointerUp += pointerUp;
         DrawBoardEvents.PointerExit += pointerExit;
         DrawBoardEvents.DragCell += dragCell;
         DrawBoardEvents.EndDrag += endDrag;
         DrawBoardEvents.PointerHoverMoveCell += moveCell;
         DrawBoardEvents.PointerHoverMove += move;
         DrawBoardEvents.Drag += drag;
         DrawBoardEvents.CancelAction += cancelAction;
      }

      protected override void unregisterUIEvents () {
         DrawBoardEvents.PointerDown -= pointerDown;
         DrawBoardEvents.PointerUp -= pointerUp;
         DrawBoardEvents.PointerExit -= pointerExit;
         DrawBoardEvents.DragCell -= dragCell;
         DrawBoardEvents.EndDrag -= endDrag;
         DrawBoardEvents.PointerHoverMoveCell -= moveCell;
         DrawBoardEvents.PointerHoverMove -= move;
         DrawBoardEvents.Drag -= drag;
         DrawBoardEvents.CancelAction -= cancelAction;
      }

      private void pointerDown (Vector3 position) {
         newStroke(position);
         if (stroke.tileGroup != null && stroke.type != BrushStroke.Type.Prefab) {
            stroke.paintPosition(position);
            DrawBoard.instance.setTilesModifyPreview(stroke.calculateTileChange());
         }
      }

      private void pointerUp (Vector3 position) {
         DrawBoard.instance.changeBoard(stroke.calculateTileChange());

         newStroke(position);
         if (stroke.tileGroup != null) {
            if (stroke.type == BrushStroke.Type.Prefab) {
               stroke.paintPosition(position);
               DrawBoard.instance.changeBoard(stroke.calculatePrefabChange());
            }
         }

         stroke.clear();
         DrawBoard.instance.setPrefabsModifyPreview(null);
         DrawBoard.instance.setTilesModifyPreview(null);
      }

      private void dragCell (Vector3Int from, Vector3Int to) {
         if (stroke.tileGroup != null) {
            if (KeyUtils.GetKey(Key.LeftShift)) {
               if (!hasPaintStarted) {
                  if (Mathf.Abs(from.x - to.x) > Mathf.Abs(from.y - to.y)) {
                     isHorizontal = true;
                  } else {
                     isHorizontal = false;
                  }

                  hasPaintStarted = true;
               }

               if (isHorizontal) {
                  to.y = from.y;
               } else {
                  to.x = from.x;
               }
            }
            if (stroke.type != BrushStroke.Type.Prefab) {
               stroke.paintPosition(to);
               DrawBoard.instance.setTilesModifyPreview(stroke.calculateTileChange());
            }
         }
      }

      private void moveCell (Vector3Int position) {
         newStroke(position);
         if (stroke.tileGroup != null) {
            if (stroke.type != BrushStroke.Type.Prefab) {
               stroke.paintPosition(position);
               DrawBoard.instance.setTilesModifyPreview(stroke.calculateTileChange());
            }
         }
      }

      private void move (Vector3 position) {
         if (Tools.selectedPrefab != null) {
            newStroke(position);
            if (stroke.tileGroup != null) {
               if (stroke.type == BrushStroke.Type.Prefab) {
                  stroke.paintPosition(position);
                  DrawBoard.instance.setPrefabsModifyPreview(stroke.calculatePrefabChange());
               }
            }
         }
      }

      private void drag (Vector3 from, Vector3 to) {
         move(to);
      }

      private void endDrag (Vector3 from, Vector3 to) {
         hasPaintStarted = false;
         DrawBoard.instance.changeBoard(stroke.calculateTileChange());
         stroke.clear();
         DrawBoard.instance.setTilesModifyPreview(null);
      }

      private void pointerExit (Vector3 position) {
         cancelAction();
      }

      protected override void cancelAction () {
         stroke.clear();
         DrawBoard.instance.setTilesModifyPreview(null);
         DrawBoard.instance.setPrefabsModifyPreview(null);
      }

      private void newStroke (Vector3 position) {
         if (Tools.tileGroup != null) {
            if (Tools.tileGroup.type == TileGroupType.Prefab || Tools.tileGroup.type == TileGroupType.TreePrefab) {
               stroke.newPrefabStroke(Tools.tileGroup, position);
            } else {
               stroke.newTileStroke(Tools.tileGroup, Tools.boardSize, DrawBoard.worldToCell(position));
            }
         }
      }
   }
}