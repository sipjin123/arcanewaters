using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class PvpArenaCell : MonoBehaviour
{
   #region Public Variables

   // The voyage id
   public Text idText;

   // The area name
   public Text areaText;

   // The player count
   public Text playerCount;

   // The time since the instance was created
   public Text timeText;

   // The state of the pvp game
   public Text gameState;

   // The game mode and arena size
   public Text gameModeAndarenaSizeText;

   // The minimap image
   public Image minimapImage;

   // A default minimap image used when the minimap doesn't exist
   public Sprite defaultMinimap;

   // The minimap button
   public Button button;

   // References to all the text components
   public Text[] textList;

   // References to all the image components
   public Image[] imageList;

   // The colors of the text depending on the pvp arena status
   public Color canBeJoinedTextColor;
   public Color cannotBeJoinedTextColor;

   // The voyage data
   [HideInInspector]
   public Voyage voyage;

   #endregion

   public void setCellForPvpArena (Voyage pvpArena) {
      setCellForPvpArena(pvpArena, null);
      disablePointerEvents();
   }

   public void setCellForPvpArena (Voyage pvpArena, UnityAction action) {
      this.voyage = pvpArena;
      idText.text = pvpArena.voyageId.ToString();
      areaText.text = pvpArena.areaName;
      playerCount.text = pvpArena.playerCount.ToString() + "/" + pvpArena.pvpGameMaxPlayerCount.ToString();
      timeText.text = DateTime.UtcNow.Subtract(DateTime.FromBinary(pvpArena.creationDate)).ToString(@"mm\:ss");
      gameState.text = PvpGame.getGameStateLabel(pvpArena.pvpGameState);
      gameModeAndarenaSizeText.text = PvpGame.getGameModeLabel(AreaManager.self.getAreaPvpGameMode(pvpArena.areaKey)) + " - " + AreaManager.self.getAreaPvpArenaSize(pvpArena.areaKey).ToString();

      // Try to find an existing minimap
      minimapImage.sprite = ImageManager.getSprite("GUI/Pvp Arena/" + pvpArena.areaKey, true);
      if (minimapImage.sprite == null || minimapImage.sprite == ImageManager.self.blankSprite) {
         // If no minimap exist, search for a placeholder one
         minimapImage.sprite = ImageManager.getSprite("Minimaps/" + pvpArena.areaKey, true);
         if (minimapImage.sprite == null || minimapImage.sprite == ImageManager.self.blankSprite) {
            minimapImage.sprite = defaultMinimap;
         }
      }

      if (PvpGame.canGameBeJoined(pvpArena)) {
         foreach (Text text in textList) {
            text.color = canBeJoinedTextColor;
         }

         foreach (Image image in imageList) {
            image.color = canBeJoinedTextColor;
         }

         button.interactable = true;
      } else {
         foreach (Text text in textList) {
            text.color = cannotBeJoinedTextColor;
         }

         foreach (Image image in imageList) {
            image.color = cannotBeJoinedTextColor;
         }

         button.interactable = false;
      }

      // Set the button click event
      button.onClick.RemoveAllListeners();
      button.onClick.AddListener(action);
   }

   public void disablePointerEvents () {
      button.interactable = false;
   }

   #region Private Variables

   #endregion
}


