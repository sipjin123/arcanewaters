using System.Collections.Generic;
using System.Linq;
using MapCreationTool.UndoSystem;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class MoveTool : Tool
   {
      protected override ToolType toolType => ToolType.Move;

      private List<TileChange> selectedTiles = new List<TileChange>();
      private HashSet<Vector3Int> selectedPositions = new HashSet<Vector3Int>();

      private bool dragging = true;

      protected override void registerUIEvents () {
         DrawBoardEvents.BeginDrag += beginDrag;
         DrawBoardEvents.Drag += drag;
         DrawBoardEvents.DragCell += dragCell;
         DrawBoardEvents.EndDrag += endDrag;
         DrawBoardEvents.PointerExit += pointerExit;
         DrawBoardEvents.CancelAction += cancelAction;
         Undo.UndoPerformed += setSelectedTiles;
         Undo.RedoPerformed += setSelectedTiles;

         setSelectedTiles();
      }

      protected override void unregisterUIEvents () {
         DrawBoardEvents.BeginDrag -= beginDrag;
         DrawBoardEvents.Drag -= drag;
         DrawBoardEvents.DragCell -= dragCell;
         DrawBoardEvents.EndDrag -= endDrag;
         DrawBoardEvents.PointerExit -= pointerExit;
         DrawBoardEvents.CancelAction -= cancelAction;
         Undo.UndoPerformed -= setSelectedTiles;
         Undo.RedoPerformed -= setSelectedTiles;
      }

      private void setSelectedTiles () {
         selectedPositions = new HashSet<Vector3Int>(DrawBoard.instance.currentSelection.tiles);

         selectedTiles.Clear();
         selectedTiles.AddRange(getTilesAtPositions(selectedPositions));
      }

      private static IEnumerable<TileChange> getTilesAtPositions (IEnumerable<Vector3Int> positions) {
         foreach (Layer layer in DrawBoard.instance.nonEmptySublayers()) {
            foreach (Vector3Int pos in positions) {
               TileBase tile = layer.getTile(pos);
               if (tile != null) {
                  yield return new TileChange {
                     tile = tile,
                     position = pos,
                     layer = layer
                  };
               }
            }
         }
      }

      private void pointerExit (Vector3 positio) {
         cancelAction();
      }

      private void beginDrag (Vector3 position) {
         dragging = true;
      }

      private void drag (Vector3 from, Vector3 to) {
         if (!dragging) return;

         DrawBoard.instance.setPrefabsModifyPreview(calculatePrefabChange(from, to));
      }

      private void dragCell (Vector3Int from, Vector3Int to) {
         if (!dragging) return;

         DrawBoard.instance.setTilesModifyPreview(calculateTileChange(from, to));
      }

      private void endDrag (Vector3 from, Vector3 to) {
         if (!dragging || from == to) return;

         Vector3Int fromCell = DrawBoard.worldToCell(from);
         Vector3Int toCell = DrawBoard.worldToCell(to);

         BoardChange change = calculatePrefabChange(from, to);
         if (fromCell != toCell) {
            change.add(calculateTileChange(DrawBoard.worldToCell(from), DrawBoard.worldToCell(to)));
         }

         DrawBoard.instance.changeBoard(change);

         DrawBoard.instance.setTilesModifyPreview(null);
         DrawBoard.instance.setPrefabsModifyPreview(null);

         setSelectedTiles();
      }

      protected override void cancelAction () {
         dragging = false;
         DrawBoard.instance.setTilesModifyPreview(null);
         DrawBoard.instance.setPrefabsModifyPreview(null);
      }

      private BoardChange calculateTileChange (Vector3Int from, Vector3Int to) {
         if (from == to) return null;

         BoardChange result = new BoardChange() { isSelectionChange = true };
         BoundsInt bounds = DrawBoard.getBoardBoundsInt();

         (int x, int y) min = (bounds.min.x, bounds.min.y);
         (int x, int y) max = (bounds.max.x, bounds.max.y);
         Vector3Int cellTransition = to - from;

         HashSet<Vector3Int> newPositions = new HashSet<Vector3Int>(selectedPositions.Select(p => p + cellTransition)
            .Where(p => !(p.x < min.x || p.x > max.x || p.y < min.y || p.y > max.y)));

         foreach (TileChange tc in selectedTiles) {
            result.tileChanges.Add(new TileChange {
               tile = null,
               position = tc.position,
               layer = tc.layer
            });

            result.selectionToRemove.add(tc.position);
         }

         foreach (TileChange tc in getTilesAtPositions(newPositions)) {
            result.tileChanges.Add(new TileChange {
               tile = null,
               position = tc.position,
               layer = tc.layer
            });
         }

         foreach (TileChange tc in selectedTiles) {
            Vector3Int newPos = tc.position + cellTransition;

            if (!(newPos.x < min.x || newPos.x > max.x || newPos.y < min.y || newPos.y > max.y)) {
               result.tileChanges.Add(new TileChange {
                  tile = tc.tile,
                  position = newPos,
                  layer = tc.layer
               });

               result.selectionToAdd.add(newPos);
            }
         }

         return result;
      }

      private BoardChange calculatePrefabChange (Vector3 from, Vector3 to) {
         if (from == to) return null;

         BoardChange result = new BoardChange() { isSelectionChange = true };
         Bounds boundsFloat = DrawBoard.getPrefabBounds();

         if (from != to) {
            Vector3 transition = to - from;
            foreach (PlacedPrefab prefab in DrawBoard.instance.currentSelection.prefabs) {
               result.prefabChanges.Add(new PrefabChange {
                  prefabToDestroy = prefab.original,
                  positionToDestroy = prefab.placedInstance.transform.position
               });

               Vector3 targetPos = prefab.placedInstance.transform.position + transition;
               if (targetPos.x >= boundsFloat.min.x & targetPos.x <= boundsFloat.max.x & targetPos.y >= boundsFloat.min.y && targetPos.y <= boundsFloat.max.y) {
                  result.prefabChanges.Add(new PrefabChange {
                     positionToPlace = targetPos,
                     prefabToPlace = prefab.original,
                     dataToSet = prefab.data.Clone(),
                     select = true
                  });
               }
            }
         }

         return result;
      }
   }
}
