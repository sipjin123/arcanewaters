using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class UsableItemDataTemplate : MonoBehaviour {
   #region Public Variables

   // Name of the item 
   public Text nameText;

   // Index of the item 
   public Text indexText;

   // Button for showing the panel in charge of editing the item data
   public Button editButton;

   // Button for deleting a item data
   public Button deleteButton;

   // Icon of the item
   public Image itemIcon;

   // Button for duplicating this template
   public Button duplicateButton;

   #endregion
}
