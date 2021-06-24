using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using TMPro;

public class PvpArenaInfoPanel : MonoBehaviour, IPointerClickHandler
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The voyage id
   public Text idText;

   // The area name
   public Text areaText;

   // The time since the instance was created
   public Text timeText;

   // The state of the pvp game
   public Text gameState;

   // The player count in team A
   public Text teamAPlayerCount;

   // The player count in team B
   public Text teamBPlayerCount;

   // The join button
   public Button joinButton;

   #endregion

   public void updatePanelWithPvpArena (Voyage pvpArena) {
      _pvpArena = pvpArena;

      idText.text = pvpArena.voyageId.ToString();
      areaText.text = pvpArena.areaName + " (" + pvpArena.areaKey + ")";
      teamAPlayerCount.text = pvpArena.playerCountTeamA.ToString();
      teamBPlayerCount.text = pvpArena.playerCountTeamB.ToString();
      timeText.text = DateTime.UtcNow.Subtract(DateTime.FromBinary(pvpArena.creationDate)).ToString(@"mm\:ss");
      gameState.text = PvpGame.getGameStateLabel(pvpArena.pvpGameState);

      show();

      // Disable the join button when the game cannot be joined
      joinButton.interactable = pvpArena.pvpGameState != PvpGame.State.PostGame && pvpArena.playerCount < pvpArena.pvpGameMaxPlayerCount;
   }

   public void onJoinButtonPressed () {
      hide();
      PvpArenaPanel.self.joinPvpArena(_pvpArena);
   }

   public void show () {
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the panel
      if (eventData.rawPointerPress == this.gameObject) {
         hide();
      }
   }

   #region Private Variables

   // The pvp arena being displayed by the panel
   private Voyage _pvpArena = null;

   #endregion
}
