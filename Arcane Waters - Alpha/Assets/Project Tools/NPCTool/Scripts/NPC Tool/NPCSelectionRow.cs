using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NPCSelectionRow : GenericEntryTemplate
{
   #region Public Variables

   // Holds the button icon of the npc
   public Button npcIconButton;

   #endregion

   public void setRowForNPC(NPCSelectionScreen npcSelectionScreen, int npcId, string npcName) {
      _npcSelectionScreen = npcSelectionScreen;
      _npcId = npcId;

      updateDisplay(npcName, npcId);
      setIdRestriction(npcId);
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
