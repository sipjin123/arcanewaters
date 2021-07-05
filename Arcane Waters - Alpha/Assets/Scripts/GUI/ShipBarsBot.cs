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
   public Sprite naturalistsIcon;

   #endregion

   protected override void Start () {
      base.Start();

      if (_entity == null) {
         return;
      }

      initializeHealthBar();

      // If we're in a pvp game, set our guild icon based on what team we're on
      if (_entity.pvpTeam != PvpTeamType.None) {
         guildIcon.sprite = (_entity.pvpTeam == PvpTeamType.A) ? naturalistsIcon : privateersIcon;
      
      // Otherwise, set our guild icon based on our guild ID
      } else {
         if (_entity.guildId == BotShipEntity.PRIVATEERS_GUILD_ID) {
            guildIcon.sprite = privateersIcon;
         } else if (_entity.guildId == BotShipEntity.PIRATES_GUILD_ID) {
            guildIcon.sprite = piratesIcon;
         } else {
            D.debug("The bot ship " + _entity.name + " has an unsupported guild id: " + _entity.guildId);
            guildIcon.gameObject.SetActive(false);
         }
      }
   }

   #region Private Variables

   #endregion
}
