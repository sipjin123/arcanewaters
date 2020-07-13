using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GenericItemUITemplate : MonoBehaviour {
   // Change item button
   public Button itemButton;
   
   // Item info
   public Text itemName;
   public Text itemId;
   public Text itemCategory;
   public InputField itemCount;

   // Item icon
   public Image itemIcon;
}
