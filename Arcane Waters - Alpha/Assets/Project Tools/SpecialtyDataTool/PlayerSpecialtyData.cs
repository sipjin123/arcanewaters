using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class PlayerSpecialtyData
{
   // The type of specialty
   public Specialty.Type type;

   // Custom name of the specialty
   public string specialtyName;

   // Info of the data
   public string description;

   // Image path
   public string specialtyIconPath;

   // Holds the stats attributed to this specialty
   public PlayerStats playerStats = new PlayerStats();
}