using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class PlayerJobData { 
   // The type of job
   public Jobs.Type type;

   // Custom name of the job
   public string jobName;

   // Info of the data
   public string description;

   // Image path
   public string jobIconPath;

   // Holds the stats attributed to this job
   public PlayerStats playerStats = new PlayerStats();
}