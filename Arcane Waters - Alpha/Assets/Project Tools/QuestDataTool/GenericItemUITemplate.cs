using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GenericItemUITemplate : MonoBehaviour {
   // Change item button, delete button
   public Button itemButton, deleteButton;
   
   // Item info
   public Text itemName;
   public Text itemId;
   public Text itemCategory;
   public InputField itemCount;

   // Item icon
   public Image itemIcon;
}
