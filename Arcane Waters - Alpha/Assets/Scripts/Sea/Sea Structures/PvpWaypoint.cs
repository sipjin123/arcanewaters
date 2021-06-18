using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;

public class PvpWaypoint : MonoBehaviour, IMapEditorDataReceiver {
   #region Public Variables

   public PvpLane lane = PvpLane.None;

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.PVP_LANE) == 0) {
            try {
               PvpLane pvpLane = (PvpLane) Enum.Parse(typeof(PvpLane), field.v);
               this.lane = pvpLane;
            } catch {
               this.lane = PvpLane.None;
            }
         }
      }
   }

   #endregion

   #region Private Variables

   #endregion
}
