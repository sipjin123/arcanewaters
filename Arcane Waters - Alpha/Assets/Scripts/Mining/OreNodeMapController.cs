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
   public float oreRespawnTimer = 888;

   // The total allowed active ores
   public int oreMaxActiveCount = 1;

   // List of ore nodes
   public List<OreNode> oreSpotList = new List<OreNode>();

   // The ore lifetime before respawning
   public Dictionary<OreNode, double> oreLifetime = new Dictionary<OreNode, double>();

   #endregion

   private IEnumerator CO_Initialize () {
      yield return new WaitForSeconds(3);
      oreLifetime = new Dictionary<OreNode, double>();

      foreach (OreNode oreNode in oreSpotList) {
         double startTime = NetworkTime.time;
         oreLifetime.Add(oreNode, startTime);
         oreNode.refreshTimer = oreRespawnTimer;
         oreNode.resetSettings();
      }

      if (oreSpotList.Count > oreMaxActiveCount) {
         List<OreNode> oreNodesToDisable = new List<OreNode>();
         List<OreNode> oreNodeListCopy = oreSpotList;

         int totalOresToDisable = oreSpotList.Count - oreMaxActiveCount;
         while (oreNodesToDisable.Count < totalOresToDisable) {
            OreNode randomOre = oreNodeListCopy.ChooseRandom();
            if (oreNodesToDisable.Find(_ => _ == randomOre) == null) {
               randomOre.isDisabledByController = true;
               oreNodesToDisable.Add(randomOre);
            }
         }
      }
   }

   public void setOreSpotList (List<OreNode> oreSpotList) {
      this.oreSpotList = oreSpotList;
      StartCoroutine(CO_Initialize());
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
