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
         foreach (GuildMemberRow row in GuildPanel.self.getGuildMemberRows()) {
            row.highlightRow.SetActive(false);
            foreach (Image image in row.backgroundImages) {
               image.sprite = row.inactiveBackgroundSprite;
            }
         }

         // Update buttons interactivity
         GuildPanel.self.checkButtonPermissions();
      }
   }

   #region Private Variables

   #endregion
}
