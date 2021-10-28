using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using TMPro;

public class PvpArenaInfoPanel : SubPanel
{
   #region Public Variables

   // The container for the map cell
   public GameObject mapCellContainer;

   // The prefab we use for creating pvp arena cells
   public PvpArenaCell pvpArenaCellPrefab;

   // The player count in team A
   public Text teamAPlayerCount;

   // The player count in team B
   public Text teamBPlayerCount;

   // The join team A button
   public Button joinTeamAButton;

   // The join team A button
   public Button joinTeamBButton;

   #endregion

   public void updatePanelWithPvpArena (Voyage pvpArena) {
      _pvpArena = pvpArena;

      // Clear out any old info
      mapCellContainer.DestroyChildren();

      // Instantiate the cell
      PvpArenaCell cell = Instantiate(pvpArenaCellPrefab, mapCellContainer.transform, false);
      cell.setCellForPvpArena(pvpArena);

      teamAPlayerCount.text = pvpArena.playerCountTeamA.ToString() + " Players";
      teamBPlayerCount.text = pvpArena.playerCountTeamB.ToString() + " Players";

      show();

      // Disable the join buttons when the game or team cannot be joined
      if (!PvpGame.canGameBeJoined(pvpArena)) {
         joinTeamAButton.interactable = false;
         joinTeamBButton.interactable = false;
      } else {
         joinTeamAButton.interactable = pvpArena.playerCountTeamA <= pvpArena.playerCountTeamB && pvpArena.playerCountTeamA < Voyage.MAX_PLAYERS_PER_GROUP_PVP;
         joinTeamBButton.interactable = pvpArena.playerCountTeamB <= pvpArena.playerCountTeamA && pvpArena.playerCountTeamB < Voyage.MAX_PLAYERS_PER_GROUP_PVP;
      }
   }

   public void onJoinTeamAButtonPressed () {
      hide();
      PvpArenaPanel.self.joinPvpArena(_pvpArena, PvpTeamType.A);
   }

   public void onJoinTeamBButtonPressed () {
      hide();
      PvpArenaPanel.self.joinPvpArena(_pvpArena, PvpTeamType.B);
   }

   #region Private Variables

   // The pvp arena being displayed by the panel
   private Voyage _pvpArena = null;

   #endregion
}
