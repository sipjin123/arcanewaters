using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class GuildPanelBackground : MonoBehaviour, IPointerClickHandler
{
   #region Public Variables

   #endregion

   public virtual void OnPointerClick (PointerEventData eventData) {
      if (eventData.button == PointerEventData.InputButton.Left) {
         GuildPanel.self.getGuildMemeberRows().ForEach(row => row.highlightRow.SetActive(false));

         // Update buttons interactivity
         GuildPanel.self.checkButtonPermissions();
      }
   }

   #region Private Variables

   #endregion
}
