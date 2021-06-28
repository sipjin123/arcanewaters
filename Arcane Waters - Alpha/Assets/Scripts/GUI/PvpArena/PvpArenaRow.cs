using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.EventSystems;
using TMPro;

public class PvpArenaRow : MonoBehaviour
{
   #region Public Variables

   // The voyage id
   public TextMeshProUGUI idText;

   // The area name
   public TextMeshProUGUI areaText;

   // The player count
   public TextMeshProUGUI playerCount;

   // The time since the instance was created
   public TextMeshProUGUI timeText;

   // The state of the pvp game
   public TextMeshProUGUI gameState;

   // The voyage data
   [HideInInspector]
   public Voyage voyage;

   #endregion

   public void setRowForVoyage (Voyage voyage) {
      this.voyage = voyage;
      idText.text = voyage.voyageId.ToString();
      areaText.text = voyage.areaName;
      playerCount.text = voyage.playerCount.ToString() + "/" + voyage.pvpGameMaxPlayerCount.ToString();
      timeText.text = DateTime.UtcNow.Subtract(DateTime.FromBinary(voyage.creationDate)).ToString(@"mm\:ss");
      gameState.text = PvpGame.getGameStateLabel(voyage.pvpGameState);
   }

   public void onRowPressed () {
      ((PvpArenaPanel) PanelManager.self.get(Panel.Type.PvpArena)).onPvpArenaRowPressed(voyage);
   }

   #region Private Variables

   #endregion
}

