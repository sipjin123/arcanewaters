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

   // Indicator determining if this template entry is active in the database
   public GameObject isEnabledIndicator;

   // Shows the sprite id being used by the equipment
   public Text spriteID;

   #endregion

   public void setData (string dataName, EquipmentType dataType, int xmlId) {
      equipmentType = dataType;
      nameText.text = dataName;
      this.xmlId = xmlId;
      indexText.text = xmlId.ToString();
      setEquipmentRestriction(xmlId);
   }

   protected void setEquipmentRestriction (int template_id) {
      if (!MasterToolAccountManager.canAlterData()) {
         deleteButton.gameObject.SetActive(false);
         duplicateButton.gameObject.SetActive(false);
      }

      if (MasterToolAccountManager.PERMISSION_LEVEL == PrivilegeType.ContentWriter) {
         if (Util.hasValidEntryName(nameText.text) && !EquipmentToolManager.equipmentToolSelf.didUserCreateData(template_id, equipmentType)) {
            deleteButton.gameObject.SetActive(false);
            editButton.gameObject.SetActive(false);
         } 
      }
   }

}
