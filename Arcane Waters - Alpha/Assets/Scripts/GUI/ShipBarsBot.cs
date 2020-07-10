using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ShipBarsBot : ShipBars
{
   #region Public Variables

   // The bot ship guild icon
   public Image guildIcon;

   // The guild icons for the bot ship guilds
   public Sprite privateersIcon;
   public Sprite piratesIcon;

   #endregion

   protected override void Start () {
      base.Start();

      if (_entity == null) {
         return;
      }

      if (_entity.guildId == BotShipEntity.PRIVATEERS_GUILD_ID) {
         guildIcon.sprite = privateersIcon;
      } else if (_entity.guildId == BotShipEntity.PIRATES_GUILD_ID) {
         guildIcon.sprite = piratesIcon;
      } else {
         D.debug("The bot ship " + _entity.name + " has an unsupported guild id: " + _entity.guildId);
         guildIcon.gameObject.SetActive(false);
      }
   }

   #region Private Variables

   #endregion
}
