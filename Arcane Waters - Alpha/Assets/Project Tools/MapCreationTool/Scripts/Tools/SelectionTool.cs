using System.Collections.Generic;
using UnityEngine;

namespace MapCreationTool
{
   public class SelectionTool : Tool
   {
      protected override ToolType toolType => ToolType.Selection;

      public static bool cancelled { get; private set; }

      protected override void registerUIEvents () {
         DrawBoardEvents.PointerClick += pointerClick;
         DrawBoardEvents.PointerExit += pointerExit;
         DrawBoardEvents.DragCell += dragCell;
         DrawBoardEvents.EndDrag += endDrag;
         DrawBoardEvents.PointerHoverMove += move;
         DrawBoardEvents.Drag += drag;
         DrawBoardEvents.BeginDrag += beginDrag;
         DrawBoardEvents.CancelAction += cancelAction;
      }

      protected override void unregisterUIEvents () {
         DrawBoardEvents.PointerClick -= pointerClick;
         DrawBoardEvents.PointerExit -= pointerExit;
         DrawBoardEvents.DragCell -= dragCell;
         DrawBoardEvents.EndDrag -= endDrag;
         DrawBoardEvents.PointerHoverMove -= move;
         DrawBoardEvents.Drag -= drag;
         DrawBoardEvents.BeginDrag -= beginDrag;
         DrawBoardEvents.CancelAction -= cancelAction;
      }

      private void pointerClick (Vector3 pos) {
         if (Tools.selectionTarget != SelectionTarget.Both && Tools.selectionTarget != SelectionTarget.Prefabs) return;

         bool add = Settings.keybindings.getAction(Keybindings.Command.SelectionAdd);
         bool rem = Settings.keybindings.getAction(Keybindings.Command.SelectionRemove);

         if (add) {
            DrawBoard.instance.changeBoard(addPrefab(pos));
         } else if (rem) {
            DrawBoard.instance.changeBoard(removePrefab(pos));
         } else {
            BoardChange change = newPrefab(pos);
            change.selectionToRemove.add(DrawBoard.instance.currentSelection.tiles);
            DrawBoard.instance.changeBoard(change);
         }
      }

      private void move (Vector3 position) {
         if (Tools.selectionTarget != SelectionTarget.Both && Tools.selectionTarget != SelectionTarget.Prefabs) return;

         bool add = Settings.keybindings.getAction(Keybindings.Command.SelectionAdd);
         bool rem = Settings.keybindings.getAction(Keybindings.Command.SelectionRemove);

         if (!add && !rem)
            add = true;

         if (add) {
            DrawBoard.instance.setPrefabsModifyPreview(addPrefab(position));
         } else {
            DrawBoard.instance.setPrefabsModifyPreview(removePrefab(position));
         }
      }

      private void beginDrag (Vector3 from) {
         cancelled = false;
      }

      private void dragCell (Vector3Int from, Vector3Int to) {
         if (cancelled) return;
         if (Tools.selectionTarget != SelectionTarget.Both && Tools.selectionTarget != SelectionTarget.Tiles) return;

         bool add = Settings.keybindings.getAction(Keybindings.Command.SelectionAdd);
         bool rem = Settings.keybindings.getAction(Keybindings.Command.SelectionRemove);

         DrawBoard.instance.setTilesModifyPreview(modifyTiles(from, to, add, rem));
      }

      private void drag (Vector3 from, Vector3 to) {
         if (cancelled) return;
         if (Tools.selectionTarget != SelectionTarget.Both && Tools.selectionTarget != SelectionTarget.Prefabs) return;

         bool add = Settings.keybindings.getAction(Keybindings.Command.SelectionAdd);
         bool rem = Settings.keybindings.getAction(Keybindings.Command.SelectionRemove);

         DrawBoard.instance.setPrefabsModifyPreview(modifyPrefabs(from, to, add, rem));
      }

      private void endDrag (Vector3 from, Vector3 to) {
         if (cancelled) return;
         bool selectTiles = Tools.selectionTarget == SelectionTarget.Both || Tools.selectionTarget == SelectionTarget.Tiles;
         bool selectPrefabs = Tools.selectionTarget == SelectionTarget.Both || Tools.selectionTarget == SelectionTarget.Prefabs;

         bool add = Settings.keybindings.getAction(Keybindings.Command.SelectionAdd);
         bool rem = Settings.keybindings.getAction(Keybindings.Command.SelectionRemove);

         BoardChange change = new BoardChange();

         if (selectTiles) {
            change.add(modifyTiles(DrawBoard.worldToCell(from), DrawBoard.worldToCell(to), add, rem));
         }
         if (selectPrefabs) {
            change.add(modifyPrefabs(from, to, add, rem));
         }

         DrawBoard.instance.changeBoard(change);
         DrawBoard.instance.setTilesModifyPreview(null);
         DrawBoard.instance.setPrefabsModifyPreview(null);
      }

      private void pointerExit (Vector3 position) {
         cancelAction();
      }

      protected override void cancelAction () {
         cancelled = true;
         DrawBoard.instance.setTilesModifyPreview(null);
         DrawBoard.instance.setTilesModifyPreview(null);
      }

      private BoardChange modifyTiles (Vector3Int p1, Vector3Int p2, bool add, bool rem) {
         if (add) {
            return addTiles(p1, p2);
         } else if (rem) {
            return removeTiles(p1, p2);
         } else {
            return newTiles(p1, p2);
         }
      }

      private BoardChange addTiles (Vector3Int p1, Vector3Int p2) {
         BoardChange result = new BoardChange();

         foreach (Vector3Int tile in BoardChange.getRectTilePositions(p1, p2)) {
            if (!DrawBoard.instance.currentSelection.contains(tile)) {
               result.selectionToAdd.add(tile);
            }
         }

         return result;
      }

      private BoardChange removeTiles (Vector3Int p1, Vector3Int p2) {
         BoardChange result = new BoardChange();

         foreach (Vector3Int tile in BoardChange.getRectTilePositions(p1, p2)) {
            if (DrawBoard.instance.currentSelection.contains(tile)) {
               result.selectionToRemove.add(tile);
            }
         }

         return result;
      }

      private BoardChange newTiles (Vector3Int p1, Vector3Int p2) {
         BoardChange result = new BoardChange();

         HashSet<Vector3Int> rect = new HashSet<Vector3Int>(BoardChange.getRectTilePositions(p1, p2));
         foreach (Vector3Int tile in DrawBoard.instance.currentSelection.tiles) {
            if (!rect.Contains(tile)) {
               result.selectionToRemove.add(tile);
            }
         }
         foreach (Vector3Int tile in rect) {
            if (!DrawBoard.instance.currentSelection.contains(tile)) {
               result.selectionToAdd.add(tile);
            }
         }

         return result;
      }

      private BoardChange addPrefab (Vector3 position) {
         BoardChange result = new BoardChange();

         PlacedPrefab hovered = DrawBoard.instance.getHoveredPrefab(position);
         if (hovered != null && !DrawBoard.instance.currentSelection.contains(hovered)) {
            result.selectionToAdd.add(hovered);
         }

         return result;
      }

      private BoardChange removePrefab (Vector3 position) {
         BoardChange result = new BoardChange();

         PlacedPrefab hovered = DrawBoard.instance.getHoveredPrefab(position);
         if (hovered != null && DrawBoard.instance.currentSelection.contains(hovered)) {
            result.selectionToRemove.add(hovered);
         }

         return result;
      }

      private BoardChange newPrefab (Vector3 position) {
         BoardChange result = new BoardChange();

         PlacedPrefab hovered = DrawBoard.instance.getHoveredPrefab(position);
         foreach (PlacedPrefab pref in DrawBoard.instance.currentSelection.prefabs) {
            if (pref != hovered) {
               result.selectionToRemove.add(pref);
            }
         }

         if (hovered != null && !DrawBoard.instance.currentSelection.contains(hovered)) {
            result.selectionToAdd.add(hovered);
         }

         return result;
      }

      private BoardChange modifyPrefabs (Vector3 p1, Vector3 p2, bool add, bool rem) {
         if (add) {
            return addPrefabs(p1, p2);
         } else if (rem) {
            return removePrefabs(p1, p2);
         } else {
            return newPrefabs(p1, p2);
         }
      }

      private BoardChange addPrefabs (Vector3 p1, Vector3 p2) {
         BoardChange result = new BoardChange();

         foreach (PlacedPrefab pref in DrawBoard.instance.prefabsBetween(p1, p2)) {
            if (!DrawBoard.instance.currentSelection.contains(pref)) {
               result.selectionToAdd.add(pref);
            }
         }

         return result;
      }

      private BoardChange removePrefabs (Vector3 p1, Vector3 p2) {
         BoardChange result = new BoardChange();

         foreach (PlacedPrefab pref in DrawBoard.instance.prefabsBetween(p1, p2)) {
            if (DrawBoard.instance.currentSelection.contains(pref)) {
               result.selectionToRemove.add(pref);
            }
         }

         return result;
      }

      private BoardChange newPrefabs (Vector3 p1, Vector3 p2) {
         BoardChange result = new BoardChange();

         HashSet<PlacedPrefab> rect = new HashSet<PlacedPrefab>(DrawBoard.instance.prefabsBetween(p1, p2));
         foreach (PlacedPrefab pref in DrawBoard.instance.currentSelection.prefabs) {
            if (!rect.Contains(pref)) {
               result.selectionToRemove.add(pref);
            }
         }
         foreach (PlacedPrefab pref in rect) {
            if (!DrawBoard.instance.currentSelection.contains(pref)) {
               result.selectionToAdd.add(pref);
            }
         }

         return result;
      }
   }
}
