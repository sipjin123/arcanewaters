using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class EraserTool : Tool
   {
      protected override ToolType toolType => ToolType.Eraser;

      private EraserStroke stroke = new EraserStroke();

      #region Eraser scaling feature

      // The selected size of the eraser
      public static int size = 1;

      // UI of the scaling
      public Text textDisplay;
      public Slider scaleSlider;

      #endregion

      private void Start () {
         scaleSlider.onValueChanged.AddListener(_ => {
            if (_ % 2 == 0) {
               _--;
            }
            textDisplay.text = _.ToString();
            size = (int) _;
         });
         textDisplay.text = scaleSlider.value.ToString();
      }

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
         float offsetValue = 1f;
         List<Vector3> strokePositions = new List<Vector3>();
         strokePositions.Add(position);

         if (size > 0) {
            int startIndex = 0 - (size / 2);
            int endIndex = size - (size / 2);
            for (int y = startIndex; y < endIndex; y++) {
               for (int x = startIndex; x < endIndex; x++) {
                  Vector3 newVector = new Vector3(position.x + (x * offsetValue), position.y + (y * offsetValue), position.z);
                  if (!strokePositions.Contains(newVector)) {
                     strokePositions.Add(newVector);
                  }
               }
            }
         }

         simulateErase(strokePositions);

         if (size > 0) {
            Vector3Int vectorPos = new Vector3Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));
            dragCell(vectorPos, vectorPos);
         }
      }

      private void simulateErase (List<Vector3> strokePositions) {
         bool ifIsNewStroke = false;
         foreach (Vector3 newPosition in strokePositions) {
            ifIsNewStroke = newStroke(newPosition);
         }

         if (ifIsNewStroke) {
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
         float offsetValue = 1f;
         List<Vector3Int> strokePositions = new List<Vector3Int>();
         strokePositions.Add(to);

         if (size > 0) {
            int startIndex = 0 - (size / 2);
            int endIndex = size - (size / 2);
            for (int y = startIndex; y < endIndex; y++) {
               for (int x = startIndex; x < endIndex; x++) {
                  int horizontalVal = Mathf.FloorToInt (to.x + (x * offsetValue));
                  int verticalVal = Mathf.FloorToInt(to.y + (y * offsetValue));
                  Vector3Int newVector = new Vector3Int(horizontalVal, verticalVal, to.z);
                  if (!strokePositions.Contains(newVector)) {
                     strokePositions.Add(newVector);
                  }
               }
            }
         }
         simulateEraseDrag(strokePositions);
      }

      private void simulateEraseDrag (List<Vector3Int> strokePositions) {
         foreach (Vector3Int newPosition in strokePositions) {
            if (stroke.type == EraserStroke.Type.Tile) {
               stroke.paint(newPosition);
            }
         }
         DrawBoard.instance.setTilesModifyPreview(stroke.calculateTileChange());
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
