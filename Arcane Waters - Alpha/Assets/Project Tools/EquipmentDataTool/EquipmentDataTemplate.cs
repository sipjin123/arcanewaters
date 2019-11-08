using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static EquipmentToolManager;

public class EquipmentDataTemplate : MonoBehaviour {
   #region Public Variables

   // Name of the item 
   public Text nameText;

   // Index of the item 
   public Text indexText;

   // Button for showing the panel in charge of editing the data
   public Button editButton;

   // Button for deleting a data
   public Button deleteButton;

   // Icon of the item
   public Image itemIcon;

   // Button for duplicating this template
   public Button duplicateButton;

   // The type of equipment
   public EquipmentType equipmentType;

   #endregion
}
