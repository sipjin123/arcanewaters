using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapCreationTool.PaletteTilesData;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class BrushTool : Tool
   {
      public BrushStroke stroke = new BrushStroke();

      // If horizontal is the beginning behavior of the brush
      public bool hasPaintStarted = false;
      public bool isHorizontal = false;

      protected override ToolType toolType => ToolType.Brush;

      #region Brush scaling feature

      // The selected size of the eraser
      public static int size = 1;

      // UI of the scaling
      public Text textDisplay;
      public Slider scaleSlider;

      #endregion

      // Self reference
      public static BrushTool instance;

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

      private void Awake () {
         instance = this;
      }

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

      public void pointerDown (Vector3 position) {
         float offsetValue = 1f;
         List<Vector3> strokePositions = new List<Vector3>();
         strokePositions.Add(position);
         newStroke(position);
         if (stroke.tileGroup != null && stroke.type != BrushStroke.Type.Prefab) {
            if (size > 1) {
               int startIndex = 0 - (size / 2);
               int endIndex = size - (size / 2);
               for (int y = startIndex; y < endIndex; y++) {
                  for (int x = startIndex; x < endIndex; x++) {
                     Vector3 newVector = new Vector3(position.x + (x * offsetValue), position.y + (y * offsetValue), position.z);
                     if (!strokePositions.Contains(newVector)) {
                        strokePositions.Add(position);
                     }
                  }
               }
            }

            simulateBrush(strokePositions);
            if (size > 1) {
               Vector3Int vectorPos = new Vector3Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));
               dragCell(vectorPos, vectorPos);
            }
         }
      }

      private void simulateBrush (List<Vector3> strokePositions) {
         foreach (Vector3 newPosition in strokePositions) {
            newStroke(newPosition);
            stroke.paintPosition(newPosition);
         }
      }

      public void pointerUp (Vector3 position) {
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

            List<Vector3Int> strokePositions = new List<Vector3Int>();
            strokePositions.Add(to);
            float offsetValue = 1f;
            if (size > 1) {
               int startIndex = 0 - (size / 2);
               int endIndex = size - (size / 2);
               for (int y = startIndex; y < endIndex; y++) {
                  for (int x = startIndex; x < endIndex; x++) {
                     int horizontalVal = Mathf.FloorToInt(to.x + (x * offsetValue));
                     int verticalVal = Mathf.FloorToInt(to.y + (y * offsetValue));
                     Vector3Int newVector = new Vector3Int(horizontalVal, verticalVal, to.z);
                     if (!strokePositions.Contains(newVector)) {
                        strokePositions.Add(newVector);
                     }
                  }
               }
            } else {
               if (stroke.type != BrushStroke.Type.Prefab) {
                  stroke.paintPosition(to);
                  DrawBoard.instance.setTilesModifyPreview(stroke.calculateTileChange());
               }
            }

            if (stroke.type != BrushStroke.Type.Prefab) {
               simulateBrushDrag(strokePositions);
            }
         }
      }

      private void simulateBrushDrag (List<Vector3Int> strokePositions) {
         foreach (Vector3Int newPosition in strokePositions) {
            stroke.paintPosition(newPosition);
         }
         DrawBoard.instance.setTilesModifyPreview(stroke.calculateTileChange());
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

      public void newStroke (Vector3 position) {
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