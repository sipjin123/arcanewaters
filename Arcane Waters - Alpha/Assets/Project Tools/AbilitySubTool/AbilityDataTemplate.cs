using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AbilityDataTemplate : MonoBehaviour
{
   #region Public Variables

   // Name of the craftable item
   public Text nameText;

   // Index of the craftable item
   public Text indexText;

   // Button for showing the panel in charge of editing the ingredients
   public Button editButton;

   // Button for deleting a craftable item
   public Button deleteButton;

   // Icon of the item
   public Image itemIcon;

   // The raw name of the item
   public string actualName;

   // Duplicate
   public Button duplicateButton;

   #endregion

   public void updateItemDisplay (BasicAbilityData resultItem) {
      string newName = "Undefined";
      try {
         actualName = resultItem.itemName;
         newName = resultItem.itemName + " (" + resultItem.abilityType + ")";
      } catch {
      }

      nameText.text = newName;
      indexText.text = "";
   }

   #region Private Variables

   #endregion
}