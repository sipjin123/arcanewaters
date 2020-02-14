using UnityEngine;

namespace MapCreationTool
{
   public class MapEditorPrefab : MonoBehaviour
   {
      public virtual void createdInPalette () { }

      public virtual void createdForPrieview () { }

      public virtual void placedInEditor () { }

      public void setOutlineHighlight (SpriteOutline outline, bool hovered, bool selected) {
         if (!hovered && !selected) {
            outline.setVisibility(false);
         } else if (hovered) {
            outline.setVisibility(true);
            outline.setNewColor(Color.white);
            outline.Regenerate();
         } else if (selected) {
            outline.setVisibility(true);
            outline.setNewColor(Color.green);
            outline.Regenerate();
         }
      }

      public void setSpriteOutline (SpriteRenderer sr, bool hovered, bool selected) {
         if (!hovered && !selected) {
            sr.color = new Color(1, 1, 1, 0);
         } else if (hovered) {
            sr.color = Color.white; ;
         } else if (selected) {
            sr.color = Color.green;
         }
      }
   }
}
