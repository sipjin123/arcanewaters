using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class PlayerClassData
{
   // The type
   public Class.Type type;

   // Custom name of the class
   public string className;

   // Info of the data
   public string description;

   // Image path
   public string itemIconPath;

   // Holds the stats attributed to this class
   public PlayerStats playerStats = new PlayerStats();
}