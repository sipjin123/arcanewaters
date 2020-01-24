using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AchievementToolTemplate : MonoBehaviour {
   #region Public Variables

   // Name of the achievement 
   public Text nameText;

   // Index of the achievement 
   public Text indexText;

   // Button for showing the panel in charge of editing the achievement data
   public Button editButton;

   // Button for deleting an achievement data
   public Button deleteButton;

   // Icon of the item
   public Image itemIcon;

   // Button for duplicating this template
   public Button duplicateButton;

   #endregion

   private void OnEnable () {
      if (!MasterToolAccountManager.canAlterData()) {
         deleteButton.gameObject.SetActive(false);
         duplicateButton.gameObject.SetActive(false);
      }

      if (Util.hasValidEntryName(nameText.text) && !AchievementToolManager.self.didUserCreateData(nameText.text)) {
         deleteButton.gameObject.SetActive(false);
         editButton.gameObject.SetActive(false);
      }
   }

   #region Private Variables

   #endregion
}
