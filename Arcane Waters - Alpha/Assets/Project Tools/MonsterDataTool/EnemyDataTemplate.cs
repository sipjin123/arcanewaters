using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class EnemyDataTemplate : MonoBehaviour {
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

   // Button for duplicating this template
   public Button duplicateButton;

   #endregion

   public void updateItemDisplay (BattlerData resultItem) {
      string newName = "Undefined";
      try {
         newName = resultItem.enemyName + "\n(" + ((Enemy.Type) resultItem.enemyType).ToString() + ")";
      } catch {
      }

      nameText.text = newName;
      indexText.text = "ID# " + ((int) resultItem.enemyType).ToString();

      if (!MasterToolAccountManager.canAlterData()) {
         deleteButton.gameObject.SetActive(false);
         duplicateButton.gameObject.SetActive(false);
      }

      if (resultItem.enemyType != Enemy.Type.None && !MonsterToolManager.self.didUserCreateData((int) resultItem.enemyType)) {
         deleteButton.gameObject.SetActive(false);
         editButton.gameObject.SetActive(false);
      }
   }

   #region Private Variables

   #endregion
}
