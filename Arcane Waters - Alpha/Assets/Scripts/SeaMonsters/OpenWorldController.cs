using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class OpenWorldController : MonoBehaviour, IMapEditorDataReceiver {
   #region Public Variables

   // The max enemy count
   public int maxEnemyCount = 10;

   // The respawn time
   public float respawnTimer = -1;

   #endregion

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.OPEN_WORLD_ENEMY_COUNT) == 0) {
            try {
               int newVal = int.Parse(field.v.Split(':')[0]);
               maxEnemyCount = newVal;
            } catch {
               D.debug("Failed to process: " + DataField.OPEN_WORLD_ENEMY_COUNT);
            }
         }

         if (field.k.CompareTo(DataField.RESPAWN_TIME) == 0) {
            try {
               float newVal = int.Parse(field.v.Split(':')[0]);
               respawnTimer = newVal;
            } catch {
               D.debug("Failed to process: " + DataField.RESPAWN_TIME);
            }
         }
      }
   }

   #region Private Variables

   #endregion
}
