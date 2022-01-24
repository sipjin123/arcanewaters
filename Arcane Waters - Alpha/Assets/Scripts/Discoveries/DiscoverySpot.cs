using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;

public class DiscoverySpot : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The ID of the discovery this spot if populated by
   public int targetDiscoveryID = 0;

   // The object id assigned in the map editor
   public int placedDiscoveryId = 0;

   #endregion

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.Contains(DataField.DISCOVERY_TYPE_ID)) {
            targetDiscoveryID = field.intValue;
         } else if (field.k.Contains(DataField.PLACED_PREFAB_ID)) {
            placedDiscoveryId = field.intValue;
         }
      }
   }

   #region Private Variables

   #endregion
}
