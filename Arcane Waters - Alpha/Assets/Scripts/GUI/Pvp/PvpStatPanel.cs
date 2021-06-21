using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PvpStatPanel : Panel {
   #region Public Variables

   // The object holding all pvp stat rows
   public Transform pvpStatRowHolder;

   // Prefab of the pvp stat row
   public PvpStatRow pvpStatRowPrefab;

   // Self
   public static PvpStatPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void populatePvpPanelData (PvpStatsData pvpStatData) {
      pvpStatRowHolder.gameObject.DestroyChildren();

      foreach (PvpPlayerStat playerStat in pvpStatData.playerStats) {
         PvpStatRow statRow = Instantiate(pvpStatRowPrefab, pvpStatRowHolder);
         statRow.kills.text = playerStat.playerKills.ToString();
         statRow.assists.text = playerStat.playerAssists.ToString();
         statRow.deaths.text = playerStat.playerDeaths.ToString();
         statRow.monsterKills.text = playerStat.playerMonsterKills.ToString();
         statRow.buildingsDestroyed.text = playerStat.playerStructuresDestroyed.ToString();
         statRow.shipKills.text = playerStat.playerShipKills.ToString();

         statRow.userName.text = playerStat.playerName.ToString();
      }
   }

   #region Private Variables

   #endregion
}
