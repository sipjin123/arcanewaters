using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CraftableItemRequirements {
   public CraftableItemRequirements () { }

   // The xml id
   public int xmlId;

   // The item result of the crafting
   public Item resultItem;

   // The item requirements of the crafting
   public Item[] combinationRequirements;

   // Determines if this is enabled in the database
   public bool isEnabled;

   // Determines if this is always available to the player
   public bool isAlwaysAvailable;
}