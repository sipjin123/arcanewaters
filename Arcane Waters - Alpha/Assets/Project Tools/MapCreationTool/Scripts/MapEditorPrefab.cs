using UnityEngine;

namespace MapCreationTool
{
   public class MapEditorPrefab : MonoBehaviour
   {
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
   }
}
