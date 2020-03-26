using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CompanionInfo {
   // The id of the companion in the sql database
   public int companionId;

   // The name of the companion
   public string companionName;

   // The type of companion
   public int companionType;

   // The level of the companion
   public int companionLevel;

   // The experience of the companion
   public int companionExp;

   // Determines if the companion slot in the players roster
   public int equippedSlot;

   // The path of the companion icon
   public string iconPath = "";
}
