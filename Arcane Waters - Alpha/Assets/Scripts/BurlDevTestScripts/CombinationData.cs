using System.Collections.Generic;
using UnityEngine;

public class CombinationData : ScriptableObject
{
   public Item ResultItem;
   public List<Item> combinationRequirements;

   public bool CheckIfRequirementsPass (List<Item> itemList) {
      for (int i = 0; i < combinationRequirements.Count; i++) {
         if (itemList.Find(_ => (CraftingIngredients.Type) _.itemTypeId == (CraftingIngredients.Type) combinationRequirements[i].itemTypeId) != null) {
         } else {
            return false;
         }
      }
      return true;
   }
}