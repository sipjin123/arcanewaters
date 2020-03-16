using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class WarpMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      [SerializeField]
      private RectTransform boundsRect = null;
      [SerializeField]
      private Text text = null;

      private string targetMap = "";
      private string targetSpawn = "";

      private float width = 1f;
      private float height = 1f;

      public void dataFieldChanged (string key, string value) {
         if (key.CompareTo(DataField.WARP_WIDTH_KEY) == 0) {
            width = value.CompareTo(string.Empty) == 0 ? 1 : Mathf.Clamp(float.Parse(value), 1, 100);
            updateBoundsSize();
         } else if (key.CompareTo(DataField.WARP_HEIGHT_KEY) == 0) {
            height = value.CompareTo(string.Empty) == 0 ? 1 : Mathf.Clamp(float.Parse(value), 1, 100);
            updateBoundsSize();
         } else if (key.CompareTo(DataField.WARP_TARGET_MAP_KEY) == 0) {
            if (int.TryParse(value, out int mapId)) {
               if (!Overlord.remoteMaps.maps.ContainsKey(mapId)) {
                  targetMap = "Unrecognized";
               } else {
                  targetMap = Overlord.remoteMaps.maps[mapId].name;
               }
            } else {
               targetMap = "Unrecognized";
            }
            updateText();
         } else if (key.CompareTo(DataField.WARP_TARGET_SPAWN_KEY) == 0) {
            targetSpawn = value;
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
         text.text = $"{ targetMap }\n{ targetSpawn }";
      }
   }
}