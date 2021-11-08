using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class OreNodeMapController : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The respawn timer
   public float oreRespawnTimer = 9999;

   // The total allowed active ores
   public int oreMaxActiveCount = 1;

   // List of ore nodes
   public List<OreNode> oreSpotList = new List<OreNode>();

   #endregion

   public void setOreSpotList (List<OreNode> oreSpotList) {
      this.oreSpotList = oreSpotList;
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.ORE_TOTAL_ACTIVE_DATA_KEY) == 0) {
            try {
               int newVal = int.Parse(field.v.Split(':')[0]);
               oreMaxActiveCount = newVal;
            } catch {
               D.debug("Failed to process: " + DataField.ORE_TOTAL_ACTIVE_DATA_KEY);
            }
         }
         if (field.k.CompareTo(DataField.ORE_RESPAWN_TIME_DATA_KEY) == 0) {
            try {
               int newVal = int.Parse(field.v.Split(':')[0]);
               oreRespawnTimer = newVal;
            } catch {
               D.debug("Failed to process: " + DataField.ORE_RESPAWN_TIME_DATA_KEY);
            }
         }
      }
   }

   #region Private Variables

   #endregion
}
