using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OreSpot : MonoBehaviour
{
   #region Public Variables

   // The Types of Ore that can spawn here
   public List<WeightedItem<OreNode.Type>> possibleOreTypes = new List<WeightedItem<OreNode.Type>>() {
         WeightedItem.Create(.60f, OreNode.Type.Iron),
         WeightedItem.Create(.30f, OreNode.Type.Silver),
         WeightedItem.Create(.10f, OreNode.Type.Gold),
      };

   #endregion

   #region Private Variables

   #endregion
}
