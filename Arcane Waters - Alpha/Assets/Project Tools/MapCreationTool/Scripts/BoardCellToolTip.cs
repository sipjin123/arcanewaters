using UnityEngine;

namespace MapCreationTool
{
   public class BoardCellToolTip : ToolTip
   {
      private void OnEnable () {
         DrawBoardEvents.BeginDrag += beginDrag;
         DrawBoardEvents.PointerExit += exit;
         DrawBoardEvents.PointerHoverMoveCell += setPosition;
         DrawBoardEvents.PointerEnter += enter;
         DrawBoardEvents.PointerDown += pointerDown;
      }

      private void OnDisable () {
         DrawBoardEvents.BeginDrag -= beginDrag;
         DrawBoardEvents.PointerExit -= exit;
         DrawBoardEvents.PointerHoverMoveCell -= setPosition;
         DrawBoardEvents.PointerEnter -= enter;
         DrawBoardEvents.PointerDown -= pointerDown;
      }

      private void pointerDown (Vector3 pos) {
         pointerExit();
      }

      private void beginDrag (Vector3 pos) {
         pointerExit();
      }

      private void enter (Vector3 pos) {
         setPosition(DrawBoard.worldToCell(pos));
      }

      private void setPosition (Vector3Int pos) {
         message = ((Vector2Int) pos).ToString();
         pointerEnter();
      }

      private void exit (Vector3 pos) {
         pointerExit();
      }
   }
}
