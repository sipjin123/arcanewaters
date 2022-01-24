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

      #endregion

      private void Awake () {
         _renderer = GetComponent<SpriteRenderer>();
      }

      public override void createdInPalette () {
         base.createdInPalette();
         // Make it smaller in the palette
         transform.localScale = new Vector3(2, 2, 2);
      }

      public void dataFieldChanged (DataField field) {
         string key = field.k.ToLower();

         if (key.Contains(DataField.DISCOVERY_TYPE_ID)) {
            int id = field.intValue;

            if (MapEditorDiscoveriesManager.instance.idToDiscovery.TryGetValue(id, out DiscoveryData d)) {
               _renderer.sprite = ImageManager.getSprite(d.spriteUrl);
            } else {
               _renderer.sprite = _undefinedDiscoverySprite;
            }
         }
      }

      #region Private Variables

      // The sprite renderer component
      private SpriteRenderer _renderer = default;

      // Sprite to use when an undefined discovery is selected
      [SerializeField]
      Sprite _undefinedDiscoverySprite = default;

      #endregion
   }
}