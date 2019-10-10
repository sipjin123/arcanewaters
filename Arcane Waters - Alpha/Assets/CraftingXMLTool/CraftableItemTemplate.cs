using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CraftableItemTemplate : MonoBehaviour {
   #region Public Variables

   // Name of the craftable item
   public Text nameText;

   // Button for showing the panel in charge of editing the ingredients
   public Button editButton;

   // Button for deleting a craftable item
   public Button deleteButton;

   #endregion

   public void updateItemDisplay(Item resultItem) {
      string newName = "Undefined";
      try {
         newName = resultItem.getCastItem().getName();
      } catch {
      }

      nameText.text = newName;
   }

   #region Private Variables
      
   #endregion
}
