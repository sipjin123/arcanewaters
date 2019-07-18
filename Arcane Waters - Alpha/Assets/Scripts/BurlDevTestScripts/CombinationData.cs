using System.Collections.Generic;
using UnityEngine;

public class CombinationData : ScriptableObject
{
   public Item resultItem;
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