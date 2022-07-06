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

   // The group instance id
   public Text groupInstanceIdText;

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

   public void setCellForVoyage (GroupInstance groupInstance) {
      setCellForVoyage(groupInstance, null);
      disablePointerEvents();
   }

   public void setCellForVoyage (GroupInstance groupInstance, UnityAction action) {
      _groupInstance = groupInstance;

      // Set default sprites
      onPointerExitButton();

      // Set the map name
      mapNameText.text = groupInstance.areaName;

      // Set the plaque images
      statsPlaqueImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + "plaque_" + getFrameName(_groupInstance.difficulty));

      // Show different relevant statuses for pvp and league instances
      if (groupInstance.isLeague) {
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
      if (groupInstance.isPvP) {
         playerCountText.text = groupInstance.playerCount + "/" + groupInstance.pvpGameMaxPlayerCount;
      } else {
         playerCountText.text = (groupInstance.groupCount * GroupInstance.getMaxGroupSize(groupInstance.difficulty)) + "/" +
            GroupInstance.getMaxGroupsPerInstance(groupInstance.difficulty) * GroupInstance.getMaxGroupSize(groupInstance.difficulty);
      }

      // Set the time since the group instance was created
      timeText.text = DateTime.UtcNow.Subtract(DateTime.FromBinary(groupInstance.creationDate)).ToString(@"mm\:ss");
      timeLeagueText.text = timeText.text;

      // Set the enemy count
      string aliveEnemiesCount = (groupInstance.aliveNPCEnemyCount == -1) ? "0" : groupInstance.aliveNPCEnemyCount.ToString();
      aliveEnemyCountText.text = aliveEnemiesCount + "/" + (groupInstance.totalNPCEnemyCount).ToString();

      // Set the league index
      leagueIndexText.text = GroupInstance.getLeagueAreaName(groupInstance.leagueIndex);

      // Set the pvp game attributes
      groupInstanceIdText.text = groupInstance.groupInstanceId.ToString();
      pvpGameStateText.text = PvpGame.getGameStateLabel(groupInstance.pvpGameState);

      // Set the plaque labels outline color
      switch (GroupInstance.getDifficultyEnum(groupInstance.difficulty)) {
         case GroupInstance.Difficulty.Easy:
            foreach (Outline outline in outlines) {
               outline.effectColor = bronzeOutlineColor;
            }
            break;
         case GroupInstance.Difficulty.Medium:
            foreach (Outline outline in outlines) {
               outline.effectColor = silverOutlineColor;
            }
            break;
         case GroupInstance.Difficulty.Hard:
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
         frameImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getFrameName(_groupInstance.difficulty) + "_frame_hover");

         // Change biome sprite - HOVERED
         biomeImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getBiomeName() + "_hover");
      }
   }

   public void onPointerExitButton () {
      if (_interactable) {
         // Change frame sprite - DEFAULT
         frameImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getFrameName(_groupInstance.difficulty) + "_frame_default");

         // Change biome sprite - DEFAULT
         biomeImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getBiomeName() + "_default");
      }
   }

   public void onPointerDownOnButton () {
      if (_interactable) {
         // Change frame sprite - PRESSED
         frameImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getFrameName(_groupInstance.difficulty) + "_frame_pressed");

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
      switch (GroupInstance.getDifficultyEnum(voyageDifficulty)) {
         case GroupInstance.Difficulty.Easy:
            return "bronze";
         case GroupInstance.Difficulty.Medium:
            return "silver";
         case GroupInstance.Difficulty.Hard:
            return "gold";
      }
      return "";
   }

   private string getBiomeName () {      
      return _groupInstance.biome.ToString().ToLower();
   }

   #region Private Variables

   // The group instance being displayed
   private GroupInstance _groupInstance;

   // Gets set to true when the cell reacts to pointer events
   private bool _interactable = true;

   #endregion
}
