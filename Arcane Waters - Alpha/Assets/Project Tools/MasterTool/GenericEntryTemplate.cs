using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GenericEntryTemplate : MonoBehaviour {
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

   // Duplicate
   public Button duplicateButton;

   // Reference to the xml manager
   public XmlDataToolManager xmlToolReference;

   #endregion

   protected void updateDisplay (string templateName, int templateID = 0) {
      nameText.text = templateName;
      indexText.text = "[" + templateID.ToString() + "]";
   }

   protected void setNameRestriction (string templateName) {
      if (!MasterToolAccountManager.canAlterData()) {
         deleteButton.gameObject.SetActive(false);
         duplicateButton.gameObject.SetActive(false);
      }

      if (MasterToolAccountManager.PERMISSION_LEVEL == AdminManager.Type.ContentWriter) {
         if (Util.hasValidEntryName(templateName) && !XmlDataToolManager.self.didUserCreateData(templateName)) {
            deleteButton.gameObject.SetActive(false);
            editButton.gameObject.SetActive(false);
         }
      }
   }

   protected void setIDRestriction (int xml_id) {
      if (!MasterToolAccountManager.canAlterData()) {
         deleteButton.gameObject.SetActive(false);
         duplicateButton.gameObject.SetActive(false);
      }

      if (MasterToolAccountManager.PERMISSION_LEVEL == AdminManager.Type.ContentWriter) {
         if (Util.hasValidEntryName(nameText.text) && !xmlToolReference.didUserCreateData(xml_id)) {
            deleteButton.gameObject.SetActive(false);
            editButton.gameObject.SetActive(false);
         } 
      }
   }

   #region Private Variables

   #endregion
}
