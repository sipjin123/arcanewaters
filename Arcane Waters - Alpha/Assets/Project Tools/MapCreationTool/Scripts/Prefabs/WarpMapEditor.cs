using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class WarpMapEditor : MonoBehaviour, IPrefabDataListener
   {
      private BoxCollider2D col;
      private RectTransform canvasRect;
      private SpriteRenderer ren;
      private Text text;

      private string targetMap = "";
      private string targetSpawn = "";

      private void Awake () {
         var canvas = GetComponentInChildren<Canvas>();

         canvasRect = canvas.GetComponent<RectTransform>();
         col = GetComponent<BoxCollider2D>();
         ren = GetComponent<SpriteRenderer>();
         text = GetComponentInChildren<Text>();

         canvas.worldCamera = Camera.main;
      }

      public void dataFieldChanged (string key, string value) {
         if (key.CompareTo(DataField.WARP_WIDTH_KEY) == 0) {
            float width = 1;
            if (float.TryParse(value, out float result))
               width = Mathf.Clamp(result, 1, 100);

            if (canvasRect != null)
               canvasRect.sizeDelta = new Vector2(width * 100, canvasRect.sizeDelta.y);
            if (ren != null) {
               ren.size = new Vector2(width, ren.size.y);
            }
            if (col != null)
               col.size = new Vector2(width, col.size.y);
         } else if (key.CompareTo(DataField.WARP_HEIGHT_KEY) == 0) {
            float height = 1;
            if (float.TryParse(value, out float result))
               height = Mathf.Clamp(result, 1, 100);

            if (canvasRect != null)
               canvasRect.sizeDelta = new Vector2(canvasRect.sizeDelta.x, height * 100);
            if (ren != null)
               ren.size = new Vector2(ren.size.x, height);
            if (col != null)
               col.size = new Vector2(col.size.x, height);
         } else if (key.CompareTo(DataField.WARP_TARGET_MAP_KEY) == 0) {
            targetMap = value;
            rewriteText();
         } else if (key.CompareTo(DataField.WARP_TARGET_SPAWN_KEY) == 0) {
            targetSpawn = value;
            rewriteText();
         }
      }

      private void rewriteText () {
         if (text != null)
            text.text = $"WARP\nmap: {targetMap}\nspawn: {targetSpawn}";
      }
   }
}