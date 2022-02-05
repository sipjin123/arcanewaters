using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class VoyageMapCell : MonoBehaviour {
   #region Public Variables

   // Path for Sea Random map sprites
   public static string SEA_MAP_PATH = "GUI/Voyages/";

   // The biome image
   public Image biomeImage;

   // The frame image
   public Image frameImage;

   // The map name
   public Text mapNameText;

   // The stats plaque image
   public Image statsPlaqueImage;

   // The player count text
   public Text playerCountText;

   // The time since the voyage started
   public Text timeText;

   // The time since the league started
   public Text timeLeagueText;

   // The number of alive enemies
   public Text aliveEnemyCountText;

   // The index of the current league
   public Text leagueIndexText;

   // The voyage id
   public Text voyageIdText;

   // The pvp game state
   public Text pvpGameStateText;

   // The text outlines
   public Outline[] outlines;

   // The colors for the outlines
   public Color bronzeOutlineColor;
   public Color silverOutlineColor;
   public Color goldOutlineColor;

   // The icons for PvP and PvE modes
   public Sprite pvpIcon;
   public Sprite pveIcon;

   // The statuses rows displayed in pvp arenas and league instances
   public GameObject[] pvpArenaStatusRows = new GameObject[0];
   public GameObject[] leagueStatusRows = new GameObject[0];

   // The click event
   public UnityEvent buttonClickEvent;

   #endregion

   public void setCellForVoyage (Voyage voyage) {
      setCellForVoyage(voyage, null);
      disablePointerEvents();
   }

   public void setCellForVoyage (Voyage voyage, UnityAction action) {
      _voyage = voyage;

      // Set default sprites
      onPointerExitButton();

      // Set the map name
      mapNameText.text = voyage.areaName;

      // Set the plaque images
      statsPlaqueImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + "plaque_" + getFrameName(_voyage.difficulty));

      // Show different relevant statuses for voyage and league instances
      if (voyage.isLeague) {
         foreach (GameObject gO in pvpArenaStatusRows) {
            if (gO.activeSelf) {
               gO.SetActive(false);
            }
         }

         foreach (GameObject gO in leagueStatusRows) {
            if (!gO.activeSelf) {
               gO.SetActive(true);
            }
         }
      } else {
         foreach (GameObject gO in leagueStatusRows) {
            if (gO.activeSelf) {
               gO.SetActive(false);
            }
         }

         foreach (GameObject gO in pvpArenaStatusRows) {
            if (!gO.activeSelf) {
               gO.SetActive(true);
            }
         }
      }

      // Set the player count
      if (voyage.isPvP) {
         playerCountText.text = voyage.playerCount + "/" + voyage.pvpGameMaxPlayerCount;
      } else {
         playerCountText.text = (voyage.groupCount * Voyage.getMaxGroupSize(voyage.difficulty)) + "/" +
            Voyage.getMaxGroupsPerInstance(voyage.difficulty) * Voyage.getMaxGroupSize(voyage.difficulty);
      }

      // Set the time since the voyage started
      timeText.text = DateTime.UtcNow.Subtract(DateTime.FromBinary(voyage.creationDate)).ToString(@"mm\:ss");
      timeLeagueText.text = timeText.text;

      // Set the enemy count
      string aliveEnemiesCount = (voyage.aliveNPCEnemyCount == -1) ? "0" : voyage.aliveNPCEnemyCount.ToString();
      aliveEnemyCountText.text = aliveEnemiesCount + "/" + (voyage.totalNPCEnemyCount).ToString();

      // Set the league index
      leagueIndexText.text = Voyage.getLeagueAreaName(voyage.leagueIndex);

      // Set the pvp game attributes
      voyageIdText.text = voyage.voyageId.ToString();
      pvpGameStateText.text = PvpGame.getGameStateLabel(voyage.pvpGameState);

      // Set the plaque labels outline color
      switch (Voyage.getDifficultyEnum(voyage.difficulty)) {
         case Voyage.Difficulty.Easy:
            foreach (Outline outline in outlines) {
               outline.effectColor = bronzeOutlineColor;
            }
            break;
         case Voyage.Difficulty.Medium:
            foreach (Outline outline in outlines) {
               outline.effectColor = silverOutlineColor;
            }
            break;
         case Voyage.Difficulty.Hard:
            foreach (Outline outline in outlines) {
               outline.effectColor = goldOutlineColor;
            }
            break;
         default:
            foreach (Outline outline in outlines) {
               outline.effectColor = Color.white;
            }
            break;
      }

      // Set the button click event
      buttonClickEvent.RemoveAllListeners();
      buttonClickEvent.AddListener(action);
   }

   public void onPointerEnterButton () {
      if (_interactable) {
         // Change frame sprite - HOVERED
         frameImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getFrameName(_voyage.difficulty) + "_frame_hover");

         // Change biome sprite - HOVERED
         biomeImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getBiomeName() + "_hover");
      }
   }

   public void onPointerExitButton () {
      if (_interactable) {
         // Change frame sprite - DEFAULT
         frameImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getFrameName(_voyage.difficulty) + "_frame_default");

         // Change biome sprite - DEFAULT
         biomeImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getBiomeName() + "_default");
      }
   }

   public void onPointerDownOnButton () {
      if (_interactable) {
         // Change frame sprite - PRESSED
         frameImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getFrameName(_voyage.difficulty) + "_frame_pressed");

         // Change biome sprite - PRESSED
         biomeImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getBiomeName() + "_pressed");
      }
   }

   public void OnPointerUpOnButton () {
      if (_interactable) {
         // Set default sprites
         onPointerExitButton();
      }
   }

   public void onPointerClickButton () {
      if (_interactable) {
         buttonClickEvent.Invoke();

         // Play sound
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.LAYOUTS_DESTINATIONS);
      }
   }

   public void disablePointerEvents () {
      _interactable = false;
   }

   public static string getFrameName (int voyageDifficulty) {
      switch (Voyage.getDifficultyEnum(voyageDifficulty)) {
         case Voyage.Difficulty.Easy:
            return "bronze";
         case Voyage.Difficulty.Medium:
            return "silver";
         case Voyage.Difficulty.Hard:
            return "gold";
      }
      return "";
   }

   private string getBiomeName () {      
      return _voyage.biome.ToString().ToLower();
   }

   #region Private Variables

   // The voyage being displayed
   private Voyage _voyage;

   // Gets set to true when the cell reacts to pointer events
   private bool _interactable = true;

   #endregion
}
