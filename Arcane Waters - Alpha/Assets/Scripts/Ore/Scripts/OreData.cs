using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OreData : ScriptableObject {

   #region Public Variables

   public List<Sprite> miningDurabilityIcon;
   public OreType oreType;
   public CraftingIngredients.Type ingredientReward;

   #endregion

   #region Private Variables

   #endregion
}
