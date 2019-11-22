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
      public void setHighlight(bool hovered, bool selected) {
         if (ren != null) {
            if (!hovered && !selected) {
               ren.color = new Color(0, 0, 0, 0);
            } else if (hovered) {
               ren.color = Color.white;
            } else if (selected) {
               ren.color = Color.green;
            }
         }
      }
   }
}
