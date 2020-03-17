using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class SpawnMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      [SerializeField]
      private RectTransform boundsRect = null;
      [SerializeField]
      private Text text = null;

      public string spawnName { get; private set; }

      private float width = 1f;
      private float height = 1f;

      public void dataFieldChanged (string key, string value) {
         if (key.CompareTo(DataField.SPAWN_WIDTH_KEY) == 0) {
            if (float.TryParse(value, out float w)) {
               width = Mathf.Clamp(w, 0.1f, 100);
               updateBoundsSize();
            }
         } else if (key.CompareTo(DataField.SPAWN_HEIGHT_KEY) == 0) {
            if (float.TryParse(value, out float h)) {
               height = Mathf.Clamp(h, 0.1f, 100);
               updateBoundsSize();
            }
         } else if (key.CompareTo(DataField.SPAWN_NAME_KEY) == 0) {
            spawnName = value;
            updateText();
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

      private void updateText () {
         text.text = spawnName;
      }
   }
}