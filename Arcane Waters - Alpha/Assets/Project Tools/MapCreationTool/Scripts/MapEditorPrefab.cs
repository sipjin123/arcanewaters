﻿using UnityEngine;

namespace MapCreationTool
{
   public class MapEditorPrefab : MonoBehaviour
   {
      public static readonly Color SELECTED_HIGHLIGHT_COLOR = Color.green;
      public static readonly Color HOVERED_HIGHLIGHT_COLOR = new Color(204 / 255f, 230 / 255f, 255 / 255f);
      public static readonly Color DELETING_HIGHLIGHT_COLOR = Color.red;

      public virtual void createdInPalette () { }

      public virtual void createdForPreview () { }

      public virtual void placedInEditor () { }

      public void setOutlineHighlight (SpriteOutline outline, bool hovered, bool selected, bool deleting) {
         if (deleting) {
            outline.setVisibility(true);
            outline.setNewColor(DELETING_HIGHLIGHT_COLOR);
            outline.Regenerate();
         } else if (!hovered && !selected) {
            outline.setVisibility(false);
         } else if (hovered) {
            outline.setVisibility(true);
            outline.setNewColor(HOVERED_HIGHLIGHT_COLOR);
            outline.Regenerate();
         } else if (selected) {
            outline.setVisibility(true);
            outline.setNewColor(SELECTED_HIGHLIGHT_COLOR);
            outline.Regenerate();
         }
      }

      public void setSpriteOutline (SpriteRenderer sr, bool hovered, bool selected, bool deleting) {
         if (deleting) {
            sr.color = DELETING_HIGHLIGHT_COLOR;
         } else if (!hovered && !selected) {
            sr.color = new Color(1, 1, 1, 0);
         } else if (hovered) {
            sr.color = HOVERED_HIGHLIGHT_COLOR;
         } else if (selected) {
            sr.color = SELECTED_HIGHLIGHT_COLOR;
         }
      }
   }
}
