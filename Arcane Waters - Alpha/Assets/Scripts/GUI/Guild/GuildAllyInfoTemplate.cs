using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class GuildAllyInfoTemplate : MonoBehaviour {
   #region Public Variables

   // The basic guild info UI
   public TextMeshProUGUI guildName, guildNumber;

   // The guild icon
   public GuildIcon guildIcon;

   // The guild ally id
   public int allyId;

   #endregion

   public void removeGuildAlliance () {
      if (Global.player == null) {
         return;
      }

      if (Global.player.guildId < 1 || allyId < 1) {
         D.debug("Invalid guild ids {" + Global.player.guildId + ":" + allyId + "}");
      }

      GuildPanel.self.guildAllyLoadBlocker.SetActive(true);
      Global.player.rpc.Cmd_RemoveGuildAlly(Global.player.guildId, allyId);
   }

   public void setGuildInfo (GuildInfo info) {
      guildName.text = info.guildName;
      guildNumber.text = info.guildId.ToString();
      allyId = info.guildId;
      guildIcon.initialize(info);
   }

   #region Private Variables
      
   #endregion
}
