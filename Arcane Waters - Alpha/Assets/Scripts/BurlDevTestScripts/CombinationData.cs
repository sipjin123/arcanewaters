using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CombinationData : ScriptableObject {

    public Item ResultItem;
    public List<Item> combinationRequirements;

    public bool CheckIfRequirementsPass(List<Item> itemList)
    {

        DebugCustom.Print("------------------------------------------------ "+ResultItem.getName());
        for (int i = 0; i < combinationRequirements.Count; i++)
        {
            DebugCustom.Print("This item needs : " + (CraftingIngredients.Type) combinationRequirements[i].itemTypeId);
        }

        for(int i = 0; i < combinationRequirements.Count; i ++)
        {
           Debug.LogError("This sword requirement is : "+ (CraftingIngredients.Type)combinationRequirements[i].itemTypeId);
        }

        for(int i = 0; i  < combinationRequirements.Count; i++)
        {
            Debug.LogError("Comparing : " + (CraftingIngredients.Type)itemList[i].itemTypeId);
            //if(combinationRequirements.Find(_=> (CraftingIngredients.Type)_.itemTypeId == (CraftingIngredients.Type)itemList[i].itemTypeId) != null)
            if (itemList.Find(_ => (CraftingIngredients.Type)_.itemTypeId == (CraftingIngredients.Type)combinationRequirements[i].itemTypeId) != null)
            {

            }
            else
            {
                return false;
            }
        }
        return true;
    }
}
