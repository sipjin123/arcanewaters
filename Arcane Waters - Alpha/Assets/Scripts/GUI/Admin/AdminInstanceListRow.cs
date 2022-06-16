using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.EventSystems;
using TMPro;

public class AdminInstanceListRow : MonoBehaviour
{
   #region Public Variables

   // The instance id
   public TextMeshProUGUI idText;

   // The server port
   public TextMeshProUGUI portText;

   // The area name
   public TextMeshProUGUI areaText;

   // The league index
   public TextMeshProUGUI leagueIndexText;

   // The instance difficulty
   public TextMeshProUGUI difficultyText;

   // The player count
   public TextMeshProUGUI playerCountText;

   // The enemies count
   public TextMeshProUGUI enemiesCountText;

   // The pvp status
   public TextMeshProUGUI isPvPText;

   // The time since the instance was created
   public TextMeshProUGUI timeText;

   // The instance biome
   public TextMeshProUGUI biomeText;

   // The voyage data
   [HideInInspector]
   public InstanceOverview instance;

   #endregion

   public void setRowForInstance (InstanceOverview instance) {
      this.instance = instance;
      idText.text = instance.id.ToString();
      portText.text = instance.port.ToString();
      areaText.text = instance.area + (string.IsNullOrWhiteSpace(instance.voyage.areaName) ? "" : " (" + instance.voyage.areaName + ")");
      leagueIndexText.text = instance.voyage.isLeague ? instance.voyage.leagueIndex.ToString() : "-";
      difficultyText.text = Voyage.getDifficultyEnum(instance.difficulty).ToString() + " (" + instance.difficulty + ")";
      playerCountText.text = instance.pCount.ToString() + "/" + instance.maxPlayerCount.ToString();
      if (instance.voyage == null || instance.voyage.voyageId <= 0) {
         enemiesCountText.text = instance.totalEnemyCount.ToString();
      } else {
         enemiesCountText.text = (instance.aliveEnemyCount < 0 ? "0" : instance.aliveEnemyCount.ToString()) + "/" + instance.totalEnemyCount;
      }
      isPvPText.text = instance.isPvp ? "Yes" : "No";
      timeText.text = DateTime.UtcNow.Subtract(DateTime.FromBinary(instance.creationDate)).ToString(@"mm\:ss");
      biomeText.text = instance.biome.ToString();
   }

   public void onRowPressed () {
      PanelManager.self.get<AdminInstanceListPanel>(Panel.Type.AdminInstanceList).onVoyageInstanceRowPressed(instance.voyage);
   }

   #region Private Variables

   #endregion
}
