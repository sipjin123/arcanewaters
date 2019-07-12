using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class GuildInviteChatLine : ChatLine, IPointerClickHandler {
   #region Public Variables

   // The associated guild invite
   public GuildInvite invite;

   #endregion

   public void OnPointerClick (PointerEventData eventData) {
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => Global.player.rpc.Cmd_AcceptInvite(invite));

      // Show a confirmation panel with the user name
      string message = "The player " + invite.senderName + " has invited you to join the guild " + invite.guildName + "!";
      PanelManager.self.confirmScreen.show(message);
   }

   #region Private Variables

   #endregion
}
