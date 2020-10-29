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
   public string xmlName = "Undefined";

   // The info of the crops
   public string xmlDescription = "";

   // The unique xml id registered in the database
   public int xmlId = 0;

   // The type of crop selected
   public int cropsType = 0;

   // Determines if this is enabled in the database
   public bool isEnabled;

   // The icon path of the crop
   public string iconPath = "";

   // The speed of the growth of the crop
   public float growthRate = .1f;

   // The cost of the crop in the stores
   public int cost = 1;

   // The xp reward for the crop
   public int rewardXp = 1;

   // The max crop count
   public int maxCrop = 1;

   // The number of levels before the crop reaches max level
   public int maxGrowthLevel = 2;

   // The number of minutes before the plant can be watered
   public float minutesToRipe = 3;

   #endregion
}
