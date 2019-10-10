using System;
using System.Collections.Generic;
using UnityEngine;

public class CombinationData : ScriptableObject
{
   // The id of the blueprint
   public int blueprintTypeID;

   // The item result of the crafting
   public Item resultItem;

   // The item requirements of the crafting
   public List<Item> combinationRequirements;

   public bool checkIfRequirementsPass (List<Item> itemList) {
      for (int i = 0; i < combinationRequirements.Count; i++) {
         if (itemList.Find(_ => (CraftingIngredients.Type) _.itemTypeId == (CraftingIngredients.Type) combinationRequirements[i].itemTypeId) != null) {
         } else {
            return false;
         }
      }
      return true;
   }
}
[Serializable]
public class CraftableItemRequirements
{
   // The id of the blueprint
   public int blueprintTypeID;

   // The item result of the crafting
   public Item resultItem;

   // The item requirements of the crafting
   public List<Item> combinationRequirements;
}