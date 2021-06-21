using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;
using MapCreationTool.Serialization;
using System;

public class PvpWaypointEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
{
   #region Public Variables

   // The pvp lane
   public Text pvpLane;

   #endregion

   #region Private Variables

   #endregion

   public void dataFieldChanged (DataField field) {
      if (field.k.CompareTo(DataField.PVP_LANE) == 0) {
         try {
            PvpLane pvpLaneVal = (PvpLane) Enum.Parse(typeof(PvpLane), field.v);
            pvpLane.text = pvpLaneVal.ToString();
         } catch {
            pvpLane.text = PvpLane.None.ToString();
         }
      }
   }

   public void setHighlight (bool hovered, bool selected, bool deleting) {
   }
}
