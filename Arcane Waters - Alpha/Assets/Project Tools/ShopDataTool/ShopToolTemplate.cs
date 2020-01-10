using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShopToolTemplate : MonoBehaviour {
   // Name of the template 
   public Text nameText;

   // Index of the template 
   public Text indexText;

   // Button for showing the panel in charge of editing the data
   public Button editButton;

   // Button for deleting a template
   public Button deleteButton;

   // Icon of the ship
   public Image itemIcon;

   // Button for duplicating this template
   public Button duplicateButton;
}
