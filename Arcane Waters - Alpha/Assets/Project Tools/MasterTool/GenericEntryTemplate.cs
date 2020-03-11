using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GenericEntryTemplate : MonoBehaviour {
   #region Public Variables

   // Name of the template
   public Text nameText;

   // Entry ID of the template
   public Text indexText;

   // Button for showing the panel in charge of editing the template
   public Button editButton;

   // Button for deleting a template
   public Button deleteButton;

   // Icon of the template
   public Image itemIcon;

   // Duplicate
   public Button duplicateButton;

   #endregion

   public static GenericEntryTemplate createGenericTemplate (GameObject prefab, XmlDataToolManager xmlManager, Transform prefabParent) {
      GenericEntryTemplate newTemplate = Instantiate(prefab.gameObject, prefabParent).GetComponent<GenericEntryTemplate>();
      newTemplate._xmlToolReference = xmlManager;
      return newTemplate;
   }

   public void setWarning () {
      _warningIndicator.SetActive(true);
   }

   protected void updateDisplay (string templateName, int templateID = 0) {
      nameText.text = templateName;
      indexText.text = templateID.ToString();
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

   protected void setIdRestriction (int xmlId) {
      if (!MasterToolAccountManager.canAlterData()) {
         deleteButton.gameObject.SetActive(false);
         duplicateButton.gameObject.SetActive(false);
      }

      if (MasterToolAccountManager.PERMISSION_LEVEL == AdminManager.Type.ContentWriter) {
         if (Util.hasValidEntryName(nameText.text) && !_xmlToolReference.didUserCreateData(xmlId)) {
            deleteButton.gameObject.SetActive(false);
            editButton.gameObject.SetActive(false);
         } 
      }
   }

   #region Private Variables

   // Reference to the xml manager
   protected XmlDataToolManager _xmlToolReference;


   // Determines if the template needs attention
   [SerializeField] protected GameObject _warningIndicator;

   #endregion
}