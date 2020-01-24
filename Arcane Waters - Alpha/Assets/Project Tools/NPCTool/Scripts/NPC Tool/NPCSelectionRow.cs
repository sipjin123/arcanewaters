using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NPCSelectionRow : MonoBehaviour
{
   #region Public Variables

   // The text component displaying the npcId
   public Text npcIdText;

   // The text component displaying the npc name
   public Text npcNameText;

   // Holds the icon of the npc
   public Image npcIcon;

   // Holds the button icon of the npc
   public Button npcIconButton;

   // Holds the button for the deletion of the npc
   public Button deleteButton;

   // Button for duplicating this template
   public Button duplicateButton;

   // Button for editing this template
   public Button editButton;

   #endregion

   public void setRowForNPC(NPCSelectionScreen npcSelectionScreen, int npcId, string npcName) {
      _npcSelectionScreen = npcSelectionScreen;
      _npcId = npcId;
      npcIdText.text = npcId.ToString();
      npcNameText.text = npcName;

      if (!MasterToolAccountManager.canAlterData()) {
         duplicateButton.gameObject.SetActive(false);
         deleteButton.gameObject.SetActive(false);
      }

      if (!NPCToolManager.self.didUserCreateData(npcId)) {
         deleteButton.gameObject.SetActive(false);
         editButton.gameObject.SetActive(false);
      }
   }

   public void editButtonClickedOn () {
      _npcSelectionScreen.editNPC(_npcId);
   }

   #region Private Variables

   // The id of the npc
   private int _npcId;

   // A reference to the parent screen
   private NPCSelectionScreen _npcSelectionScreen;

   #endregion
}
