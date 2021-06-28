using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.EventSystems;
using TMPro;

public class AdminVoyageRow : MonoBehaviour
{
   #region Public Variables

   // The voyage id
   public TextMeshProUGUI idText;

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
   public Voyage voyage;

   #endregion

   public void setRowForVoyage (Voyage voyage) {
      this.voyage = voyage;
      idText.text = voyage.instanceId.ToString();
      areaText.text = voyage.areaName + " (" + voyage.areaKey + ")";
      leagueIndexText.text = voyage.isLeague ? voyage.leagueIndex.ToString() : "-";
      difficultyText.text = Voyage.getDifficultyEnum(voyage.difficulty).ToString() + " (" + voyage.difficulty + ")";
      playerCountText.text = voyage.playerCount.ToString();
      enemiesCountText.text = voyage.aliveNPCEnemyCount + "/" + voyage.totalNPCEnemyCount;
      isPvPText.text = voyage.isPvP ? "Yes" : "No";
      timeText.text = DateTime.UtcNow.Subtract(DateTime.FromBinary(voyage.creationDate)).ToString(@"mm\:ss");
      biomeText.text = voyage.biome.ToString();
   }

   public void onRowPressed () {
      ((AdminVoyagePanel) PanelManager.self.get(Panel.Type.AdminVoyage)).onVoyageInstanceRowPressed(voyage);
   }

   #region Private Variables

   #endregion
}
