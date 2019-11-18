using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PlayerClassTemplate : MonoBehaviour {
   #region Public Variables

   // Name of the class
   public Text nameText;

   // Index of the class 
   public Text indexText;

   // Button for showing the panel in charge of editing the class
   public Button editButton;

   // Button for deleting a class
   public Button deleteButton;

   // Icon of the class
   public Image itemIcon;

   // Button for duplicating this template
   public Button duplicateButton;

   #endregion

   #region Private Variables

   #endregion
}
