using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class SpawnMapEditor : MonoBehaviour, IPrefabDataListener
   {
      private BoxCollider2D col;
      private RectTransform canvasRect;
      private SpriteRenderer ren;
      private Text text;

      public string spawnName { get; private set; }

      private void Awake () {
         var canvas = GetComponentInChildren<Canvas>();

         canvasRect = canvas.GetComponent<RectTransform>();
         col = GetComponent<BoxCollider2D>();
         ren = GetComponent<SpriteRenderer>();
         text = GetComponentInChildren<Text>();

         canvas.worldCamera = Camera.main;
      }

      public void dataFieldChanged (string key, string value) {
         if (key.CompareTo(DataField.SPAWN_WIDTH_KEY) == 0) {
            float width = value.CompareTo(string.Empty) == 0 ? 1 : Mathf.Clamp(float.Parse(value), 1, 100);

            if (canvasRect != null)
               canvasRect.sizeDelta = new Vector2(width * 100, canvasRect.sizeDelta.y);
            if (ren != null) {
               ren.size = new Vector2(width, ren.size.y);
            }
            if (col != null)
               col.size = new Vector2(width, col.size.y);
         } else if (key.CompareTo(DataField.SPAWN_HEIGHT_KEY) == 0) {
            float height = value.CompareTo(string.Empty) == 0 ? 1 : Mathf.Clamp(float.Parse(value), 1, 100);

            if (canvasRect != null)
               canvasRect.sizeDelta = new Vector2(canvasRect.sizeDelta.x, height * 100);
            if (ren != null)
               ren.size = new Vector2(ren.size.x, height);
            if (col != null)
               col.size = new Vector2(col.size.x, height);
         } else if (key.CompareTo(DataField.SPAWN_NAME_KEY) == 0) {
            spawnName = value;
            rewriteText();
         }
      }

      private void rewriteText () {
         text.text = $"SPAWN\nname: {spawnName}";
      }
   }
}