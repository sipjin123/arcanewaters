using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static EquipmentToolManager;

public class EquipmentDataTemplate : GenericEntryTemplate {
   #region Public Variables

   // The type of equipment
   public EquipmentType equipmentType;

   // Cached xml id
   public int xmlId;

   #endregion

   public void setData (string dataName, int dataID, EquipmentType dataType, int templateID) {
      equipmentType = dataType;
      nameText.text = dataName;
      xmlId = templateID;
      indexText.text = dataID.ToString();
      setEquipmentRestriction(templateID);
   }

   protected void setEquipmentRestriction (int template_id) {
      if (!MasterToolAccountManager.canAlterData()) {
         deleteButton.gameObject.SetActive(false);
         duplicateButton.gameObject.SetActive(false);
      }

      if (MasterToolAccountManager.PERMISSION_LEVEL == AdminManager.Type.ContentWriter) {
         if (Util.hasValidEntryName(nameText.text) && !EquipmentToolManager.equipmentToolSelf.didUserCreateData(template_id, equipmentType)) {
            deleteButton.gameObject.SetActive(false);
            editButton.gameObject.SetActive(false);
         } 
      }
   }

}
