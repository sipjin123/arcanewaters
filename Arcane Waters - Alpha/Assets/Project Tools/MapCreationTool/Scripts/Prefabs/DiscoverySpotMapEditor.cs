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
         canvas.enabled = selected;
      }

      public void dataFieldChanged (string key, string value) {
         key = key.ToLower();

         if (key == DataField.POSSIBLE_DISCOVERY) {
            _renderer.sprite = ImageManager.getSprite(MapEditorDiscoveriesManager.instance.idToDiscovery[int.Parse(value)].spriteUrl);
         } else if (key == DataField.DISCOVERY_SPAWN_CHANCE) {
            _chanceText.text = $"{Mathf.RoundToInt(float.Parse(value) * 100)}%";
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
