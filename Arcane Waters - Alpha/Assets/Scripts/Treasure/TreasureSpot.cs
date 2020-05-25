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

   // If this treasure is using a custom sprite
   public bool useCustomSprite;

   // The custom sprite path
   public string customSpritePath;

   #endregion

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.TREASURE_SPOT_SPAWN_CHANCE_KEY) == 0) {
            if (field.tryGetFloatValue(out float chance)) {
               spawnChance = Mathf.Clamp(chance, 0, 1);
            }
         }

         if (field.k.CompareTo(DataField.TREASURE_USE_CUSTOM_TYPE_KEY) == 0) {
            try {
               if (field.tryGetIntValue(out int boolResult)) {
                  useCustomSprite = boolResult == 1 ? true : false;
               }
            } catch {
               useCustomSprite = false;
            }
         }

         if (field.k.CompareTo(DataField.TREASURE_SPRITE_TYPE_KEY) == 0) {
            if (useCustomSprite) {
               customSpritePath = field.v;
            }
         }
      }
   }

   #region Private Variables

   #endregion
}
