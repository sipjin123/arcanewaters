using System.Collections.Generic;
using UnityEngine;

namespace MapCreationTool
{
   public class PlacedPrefab
   {
      public GameObject placedInstance { get; set; }

      public GameObject original { get; set; }

      public Dictionary<string, string> data { get; private set; }

      public static int nextPrefabId { get; set; }

      public PlacedPrefab () {
         data = new Dictionary<string, string>();
      }

      public bool isOriginalAtPosition (GameObject original, Vector2 position) {
         return this.original == original && (Vector2) placedInstance.transform.position == position;
      }

      public void setVisible (bool visible) {
         foreach (Renderer ren in placedInstance.GetComponentsInChildren<Renderer>(true)) {
            ren.enabled = visible;
         }

         foreach (Canvas canvas in placedInstance.GetComponentsInChildren<Canvas>(true)) {
            canvas.enabled = visible;
         }
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         var outline = placedInstance.GetComponent<SpriteOutline>();
         var highlight = placedInstance.GetComponent<PrefabHighlight>();
         var highlightable = placedInstance.GetComponent<IHighlightable>();

         if (outline != null) {
            if (deleting) {
               outline.setVisibility(true);
               outline.setNewColor(MapEditorPrefab.DELETING_HIGHLIGHT_COLOR);
               SpriteRenderer sr = outline.transform.Find("Outline")?.GetComponent<SpriteRenderer>();
               if (sr != null) {
                  sr.color = MapEditorPrefab.DELETING_HIGHLIGHT_COLOR;
               }
               outline.Regenerate();
            } else if (!hovered && !selected) {
               outline.setVisibility(false);
            } else if (hovered) {
               outline.setVisibility(true);
               outline.setNewColor(MapEditorPrefab.HOVERED_HIGHLIGHT_COLOR);
               SpriteRenderer sr = outline.transform.Find("Outline")?.GetComponent<SpriteRenderer>();
               if (sr != null) {
                  sr.color = MapEditorPrefab.HOVERED_HIGHLIGHT_COLOR;
               }
               outline.Regenerate();
            } else if (selected) {
               outline.setVisibility(true);
               outline.setNewColor(MapEditorPrefab.SELECTED_HIGHLIGHT_COLOR);
               outline.color = MapEditorPrefab.SELECTED_HIGHLIGHT_COLOR;
               SpriteRenderer sr = outline.transform.Find("Outline")?.GetComponent<SpriteRenderer>();
               if (sr != null) {
                  sr.color = MapEditorPrefab.SELECTED_HIGHLIGHT_COLOR;
               }
               outline.Regenerate();
            }
         }

         if (highlight != null)
            highlight.setHighlight(hovered, selected, deleting);

         if (highlightable != null)
            highlightable.setHighlight(hovered, selected, deleting);
      }

      public void setData (string key, string value) {
         if (data.ContainsKey(key)) {
            data[key] = value;
         } else {
            data.Add(key, value);
         }
      }

      public string getData (string key) {
         if (data.TryGetValue(key, out string value))
            return value;
         return string.Empty;
      }
   }
}
