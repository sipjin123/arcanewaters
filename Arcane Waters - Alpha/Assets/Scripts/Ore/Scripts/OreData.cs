using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OreData : ScriptableObject {

   #region Public Variables

   // Holds the sprite icons which changes each time the ore is mined
   public List<Sprite> miningDurabilityIcon;

   // Determines the type of ore
   public OreType oreType;

   // Determines the reward received after mining
   public CraftingIngredients.Type ingredientReward;

   #endregion

   #region Private Variables

   #endregion
}
