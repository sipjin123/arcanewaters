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

   // Icon of the silver currency awarded for kills
   public Sprite silverIcon;

   // Icon of the silver currency awarded for assists
   public Sprite assistSilverIcon;

   // Reference to the title gameobject
   public Text title;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void setTitle (string newTitle) {
      if (title != null) {
         title.text = newTitle;
      }
   }

   public void populatePvpPanelData (GameStatsData pvpStatData) {
      pvpStatRowHolder.gameObject.DestroyChildren();

      foreach (GameStats playerStat in pvpStatData.stats) {
         PvpStatRow statRow = Instantiate(pvpStatRowPrefab, pvpStatRowHolder);
         statRow.kills.text = playerStat.PvpPlayerKills.ToString();
         statRow.assists.text = playerStat.playerAssists.ToString();
         statRow.deaths.text = playerStat.PvpPlayerDeaths.ToString();
         statRow.monsterKills.text = playerStat.playerMonsterKills.ToString();
         statRow.buildingsDestroyed.text = playerStat.playerStructuresDestroyed.ToString();
         statRow.shipKills.text = playerStat.playerShipKills.ToString();
         statRow.silver.text = playerStat.silver.ToString();

         statRow.userName.text = playerStat.playerName.ToString();
      }
   }

   #region Private Variables

   #endregion
}
