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

   // The chances of spawning this discovery
   public float spawnChance;

   // The list of IDs for all the possible discoveries
   public List<int> possibleDiscoveryIds = new List<int>();

   #endregion

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k == DataField.DISCOVERY_SPAWN_CHANCE) {
            spawnChance = field.floatValue;
         } else if (field.k.Contains(DataField.POSSIBLE_DISCOVERY)) {
            int id = field.intValue;

            // When ID is 0, it's because it wasn't assigned in the MapEditor
            if (id > 0) {
               possibleDiscoveryIds.Add(id);
            }
         }
      }
   }

   #region Private Variables

   #endregion
}
