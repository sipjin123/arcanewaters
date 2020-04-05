using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class TreasureSpot : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The chance of treasure spawning at this spot
   public float spawnChance = 0f;

   #endregion

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.TREASURE_SPOT_SPAWN_CHANCE_KEY) == 0) {
            if (field.tryGetFloatValue(out float chance)) {
               spawnChance = Mathf.Clamp(chance, 0, 1);
               Debug.Log("parsed chance " + chance);
            }
         }
      }
   }

   #region Private Variables

   #endregion
}
