using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterDataTemplate : MonoBehaviour
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

   #endregion

   public void updateItemDisplay (SeaMonsterEntityDataCopy resultItem) {
      string newName = "Undefined";
      try {
         newName = resultItem.monsterName + " (" + ((Enemy.Type) resultItem.seaMonsterType).ToString() + ")";
      } catch {
      }

      nameText.text = newName;
      indexText.text = "ID# " + ((int) resultItem.seaMonsterType).ToString();
   }

   #region Private Variables

   #endregion
}
