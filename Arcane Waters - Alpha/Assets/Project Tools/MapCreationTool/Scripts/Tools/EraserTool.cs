using UnityEngine;

namespace MapCreationTool
{
   public class EraserTool : Tool
   {
      protected override ToolType toolType => ToolType.Eraser;

      private EraserStroke stroke = new EraserStroke();

      protected override void registerUIEvents () {
         DrawBoardEvents.PointerDown += pointerDown;
         DrawBoardEvents.PointerUp += pointerUp;
         DrawBoardEvents.PointerExit += pointerExit;
         DrawBoardEvents.DragCell += dragCell;
         DrawBoardEvents.EndDrag += endDrag;
         DrawBoardEvents.CancelAction += cancelAction;
      }

      protected override void unregisterUIEvents () {
         DrawBoardEvents.PointerDown -= pointerDown;
         DrawBoardEvents.PointerUp -= pointerUp;
         DrawBoardEvents.PointerExit -= pointerExit;
         DrawBoardEvents.DragCell -= dragCell;
         DrawBoardEvents.EndDrag -= endDrag;
         DrawBoardEvents.CancelAction -= cancelAction;
      }

      private void pointerDown (Vector3 position) {
         if (newStroke(position)) {
            if (stroke.type == EraserStroke.Type.Prefab) {
               DrawBoard.instance.setPrefabsModifyPreview(stroke.calculatePrefabChange());
            } else {
               DrawBoard.instance.setTilesModifyPreview(stroke.calculateTileChange());
            }
         }
      }

      private void pointerUp (Vector3 position) {
         if (stroke.type == EraserStroke.Type.Prefab) {
            DrawBoard.instance.changeBoard(stroke.calculatePrefabChange());
         } else {
            DrawBoard.instance.changeBoard(stroke.calculateTileChange());
         }

         stroke.clear();
         DrawBoard.instance.setPrefabsModifyPreview(null);
         DrawBoard.instance.setTilesModifyPreview(null);
      }

      private void dragCell (Vector3Int from, Vector3Int to) {
         if (stroke.type == EraserStroke.Type.Tile) {
            stroke.paint(to);
            DrawBoard.instance.setTilesModifyPreview(stroke.calculateTileChange());
         }
      }

      /// <summary>
      /// Clears the old stroke and starts a new one.
      /// </summary>
      /// <param name="position"></param>
      /// <returns>True, if stroke was started, false, if it wasn't (nothing to delete at a position)</returns>
      private bool newStroke (Vector3 position) {
         PlacedPrefab hoveredPref = DrawBoard.instance.getHoveredPrefab(position);
         if (hoveredPref != null) {
            stroke.newStroke(hoveredPref);
            return true;
         }

         Layer targetLayer = DrawBoard.instance.getTopLayerWithTile(DrawBoard.worldToCell(position));
         if (targetLayer != null) {
            stroke.newStroke(targetLayer);
            stroke.paint(DrawBoard.worldToCell(position));
            return true;
         }

         stroke.clear();
         return false;
      }

      private void endDrag (Vector3 from, Vector3 to) {
         if (stroke.type == EraserStroke.Type.Prefab) {
            DrawBoard.instance.changeBoard(stroke.calculatePrefabChange());
         } else {
            DrawBoard.instance.changeBoard(stroke.calculateTileChange());
         }

         stroke.clear();
         DrawBoard.instance.setPrefabsModifyPreview(null);
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
   }
}
