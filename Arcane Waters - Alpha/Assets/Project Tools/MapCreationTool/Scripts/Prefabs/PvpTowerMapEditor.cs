using UnityEngine;
using MapCreationTool.Serialization;
using UnityEngine.UI;
using System;

namespace MapCreationTool {
   public class PvpTowerMapEditor : PvpStructureMapEditor, IPrefabDataListener, IHighlightable {
      #region Public Variables

      // The scale of the range indicator
      public float rangeIndicatorScale = 1;

      // The object displaying the range indicator
      public Transform rangeIndicatorObject;

      #endregion

      // TODO: Set pvp base editor specific functionality here

      public void dataFieldChanged (DataField field) {
         base.dataFieldChanged(field);

         if (field.k.CompareTo(DataField.PVP_TOWER_RANGE) == 0) {
            try {
               float pvpTowerRange = int.Parse(field.v);
               rangeIndicatorScale = pvpTowerRange;
               rangeIndicatorObject.localScale = new Vector2(pvpTowerRange, pvpTowerRange);
            } catch { 
            
            }
         }
      }

      #region Private Variables

      #endregion
   }
}