using MapCreationTool.Serialization;
using UnityEngine;

namespace MapCreationTool
{
   public class LedgeMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      [SerializeField]
      private RectTransform boundsRect = null;

      public int width { get; private set; }
      public int height { get; private set; }

      public LedgeMapEditor () {
         width = 1;
         height = 1;
      }

      public void dataFieldChanged (string key, string value) {
         if (key.CompareTo(DataField.LEDGE_WIDTH_KEY) == 0) {
            if (int.TryParse(value, out int w)) {
               width = Mathf.Clamp(w, 1, 100);
               updateBoundsSize();
            }
         } else if (key.CompareTo(DataField.LEDGE_HEIGHT_KEY) == 0) {
            if (int.TryParse(value, out int h)) {
               height = Mathf.Clamp(h, 1, 100);
               updateBoundsSize();
            }
         }
      }

      public override void createdInPalette () {
         boundsRect.gameObject.SetActive(false);
      }

      public override void createdForPreview () {
         boundsRect.gameObject.SetActive(false);
      }

      public override void placedInEditor () {
         boundsRect.gameObject.SetActive(false);
      }

      public override void setHovered (bool hovered) {
         base.setHovered(hovered);
         boundsRect.gameObject.SetActive(hovered || selected);
      }

      private void updateBoundsSize () {
         boundsRect.sizeDelta = new Vector2(width * 100, height * 100);
      }
   }
}

