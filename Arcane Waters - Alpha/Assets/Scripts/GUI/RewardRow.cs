using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class RewardRow : MonoBehaviour {
   #region Public Variables

   // Shows the icon of the rewarded item
   public Image rewardIcon;

   // Name of the rewarded item
   public Text rewardName;

   // The gameobject indicating the quantity of the item
   public GameObject quantityContainer;

   // The quantity text of the item to be rewarded
   public Text quantityText;

   // The quantity shadow text of the item to be rewarded
   public Text quantityTextShadow;

   #endregion

   public void setQuantityText(string quantity) {
      quantityText.text = quantity.ToString();
      quantityTextShadow.text = quantity.ToString();
   }

   #region Private Variables

   #endregion
}