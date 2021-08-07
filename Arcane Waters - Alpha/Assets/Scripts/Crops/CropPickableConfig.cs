using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[CreateAssetMenu(fileName = "New crop pickable config", menuName = "Data/Crop pickable config")]
public class CropPickableConfig : ScriptableObject
{
   #region Public Variables

   // Crops configuration data
   public List<SinglePickableConfig> config = new List<SinglePickableConfig>();

   #endregion

   [Serializable]
   public class SinglePickableConfig
   {
      // Type of crop that this values will be used with
      public Crop.Type cropType;

      // Rotation of crop pickable
      [Tooltip("Use value 0 to do not change rotation")]
      public float finalRotation;

      // Position of crop pickable
      [Tooltip("Use value 0 to do not change position")]
      public float finalPositionX;
      public float finalPositionY;

      // Shadow size of crop pickable
      [Tooltip("Use value 0 to do not change shadow transform")]
      public float shadowWidth;
      public float shadowHeight;
   }
}
