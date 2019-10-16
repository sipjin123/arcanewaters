using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftableItemRequirements
{
   public CraftableItemRequirements () { }

   // The item result of the crafting
   public Item resultItem;

   // The item requirements of the crafting
   public Item[] combinationRequirements;
}