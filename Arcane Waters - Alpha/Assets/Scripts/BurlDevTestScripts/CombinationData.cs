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
        for(int i = 0; i  < itemList.Count; i++)
        {
            if(combinationRequirements.Find(_=>_.itemTypeId == itemList[i].itemTypeId) != null)
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
