using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CraftableItemRequirements
{
   // The item result of the crafting
   public Item resultItem;

   // The item requirements of the crafting
   public List<Item> combinationRequirements;
}