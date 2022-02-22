using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class WhaleMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      #region Public Variables

      // Circle the shows the appear radius of a whale
      public Transform appearRadiusHighlight;

      #endregion

      public override void createdForPreview () {
         appearRadiusHighlight.gameObject.SetActive(false);
      }

      public override void createdInPalette () {
         appearRadiusHighlight.gameObject.SetActive(false);
         transform.localScale = transform.localScale * 0.5f;
      }

      public override void placedInEditor () {
         appearRadiusHighlight.gameObject.SetActive(true);
      }

      public override void setHovered (bool hovered) {
         appearRadiusHighlight.gameObject.SetActive(selected);
      }

      public override void setSelected (bool selected) {
         appearRadiusHighlight.gameObject.SetActive(selected);
      }

      public void dataFieldChanged (DataField field) {
         if (field.isKey(DataField.WHALE_RADIUS_KEY)) {
            _appearRadius = field.floatValue;
            appearRadiusHighlight.transform.localScale = new Vector3(_appearRadius * 2f, _appearRadius * 2f, 1f);
         }
      }

      #region Private Variables

      // In what area can the whale appear in
      private float _appearRadius = 1f;

      #endregion

   }
}