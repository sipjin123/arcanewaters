using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CraftableItemRequirements
{
   public CraftableItemRequirements () { }

   // The item result of the crafting
   public Item resultItem;

   // The item requirements of the crafting
   public Item[] combinationRequirements;
}