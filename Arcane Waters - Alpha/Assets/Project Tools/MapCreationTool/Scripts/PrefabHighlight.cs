using UnityEngine;

namespace MapCreationTool
{
   public class PrefabHighlight : MonoBehaviour
   {
      [SerializeField, Header("If null, will look on this gameObject")]
      private SpriteRenderer ren;

      private void Awake () {
         if (ren == null)
            ren = GetComponent<SpriteRenderer>();

         if (ren != null)
            ren.color = new Color(0, 0, 0, 0);
      }
      public void setHighlight (bool hovered, bool selected, bool deleting) {
         if (ren != null) {
            if (deleting) {
               ren.color = MapEditorPrefab.DELETING_HIGHLIGHT_COLOR;
            } else if (!hovered && !selected) {
               ren.color = new Color(0, 0, 0, 0);
            } else if (hovered) {
               ren.color = MapEditorPrefab.HOVERED_HIGHLIGHT_COLOR;
            } else if (selected) {
               ren.color = MapEditorPrefab.SELECTED_HIGHLIGHT_COLOR;
            }
         }
      }
   }
}
