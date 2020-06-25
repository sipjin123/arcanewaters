using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TreasureDropsItemTemplate : MonoBehaviour {
   #region Public Variables

   // Index of the item
   public Text itemIndex;

   // Name of the Item
   public Text itemName;

   // The spawn chance
   public InputField dropChance;

   // If should spawn on secret chests
   public Toggle spawnOnSecrets;
   
   // Item Cache
   public Item item;

   // Destroys the template
   public Button destroyButton;

   // The item icon
   public Image itemIcon;

   // The type of item
   public Text itemType;

   #endregion

   public void clampValue () {
      float newValue = 0;
      try {
         newValue = float.Parse(dropChance.text);
      } catch {

      }
      newValue = Mathf.Clamp(newValue, 0f, 100f);
      dropChance.text = newValue.ToString();
   }

   #region Private Variables
      
   #endregion
}
