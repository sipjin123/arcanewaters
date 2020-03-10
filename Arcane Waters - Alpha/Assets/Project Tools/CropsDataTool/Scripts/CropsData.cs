using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class CropsData {
   #region Public Variables

   // The name of the crops
   public string xmlName;

   // The info of the crops
   public string xmlDescription;

   // The unique xml id registered in the database
   public int xmlId;

   // The type of crop selected
   public int cropsType;

   // Determines if this is enabled in the database
   public bool isEnabled;

   // The icon path of the crop
   public string iconPath;

   // The speed of the growth of the crop
   public float growthRate;

   // The cost of the crop in the stores
   public int cost;

   #endregion
}
