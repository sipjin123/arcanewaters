using MapCreationTool;
using MapCreationTool.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool {
   public class WishingWellMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
      #region Public Variables

      // The object holding the range display
      public GameObject rangeDisplay;

      #endregion

      private void Start () {
         if (transform.GetComponentInParent<Palette>() != null) {
            rangeDisplay.SetActive(false);
         }
      }

      public void dataFieldChanged (DataField field) {
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
      }

      #region Private Variables

      #endregion
   }
}