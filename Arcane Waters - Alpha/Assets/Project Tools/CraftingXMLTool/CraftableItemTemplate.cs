﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CraftableItemTemplate : MonoBehaviour {
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

   #endregion

   public void updateItemDisplay(Item resultItem) {
      string newName = "Undefined";
      try {
         newName = resultItem.getCastItem().getName();
      } catch {
      }

      nameText.text = newName;
      indexText.text = "ID# "+resultItem.itemTypeId.ToString();
   }

   #region Private Variables
      
   #endregion
}