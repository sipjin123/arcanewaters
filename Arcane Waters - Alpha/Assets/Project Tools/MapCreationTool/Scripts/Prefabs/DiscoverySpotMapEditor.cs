using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class DiscoverySpotMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      #region Public Variables

      // The world space canvas for this discovery
      public Canvas canvas;

      #endregion

      private void Awake () {
         _renderer = GetComponent<SpriteRenderer>();
      }

      public override void setSelected (bool selected) {
         base.setSelected(selected);
         canvas.gameObject.SetActive(selected || selected);
      }

      public override void setHovered (bool hovered) {
         base.setHovered(hovered);
         canvas.gameObject.SetActive(hovered || selected);
      }

      public override void placedInEditor () {
         canvas.gameObject.SetActive(false);
      }

      public override void createdInPalette () {
         canvas.gameObject.SetActive(false);
      }

      public void dataFieldChanged (DataField field) {
         string key = field.k.ToLower();

         if (key == DataField.DISCOVERY_SPAWN_CHANCE) {
            _chanceText.text = $"{Mathf.RoundToInt(field.floatValue * 100)}%";
         }
      }

      #region Private Variables

      // The sprite renderer component
      private SpriteRenderer _renderer;

      // The text showing the chances as a 0-100 number
      [SerializeField]
      private Text _chanceText;

      #endregion
   }
}