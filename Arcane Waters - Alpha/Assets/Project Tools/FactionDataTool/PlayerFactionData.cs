using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class PlayerFactionData 
{
   // The type of faction
   public Faction.Type type;

   // Custom name of the faction
   public string factionName;

   // Info of the data
   public string description;

   // Image path
   public string factionIconPath;

   // Holds the stats attributed to this faction
   public PlayerStats playerStats = new PlayerStats();
}