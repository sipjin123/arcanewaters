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

   // The PvP-PvE mode image
   public Image pvpPveModeImage;

   // The PvP-PvE text
   public Text pvpPveText;
   
   // The player count text
   public Text playerCountText;

   // The time since the voyage started
   public Text timeText;

   // The treasure site count text
   public Text treasureSiteCountText;

   // The PvP-PvE text outline
   public Outline pvpPveOutline;

   // The player count text outline
   public Outline playerCountOutline;

   // The time text outline
   public Outline timeOutline;

   // The treasure site count outline
   public Outline treasureSiteCountOutline;

   // The colors for the outlines
   public Color bronzeOutlineColor;
   public Color silverOutlineColor;
   public Color goldOutlineColor;

   // The icons for PvP and PvE modes
   public Sprite pvpIcon;
   public Sprite pveIcon;

   // The click event
   public UnityEvent buttonClickEvent;

   #endregion

   public void setCellForVoyage (Voyage voyage, UnityAction action) {
      _voyage = voyage;

      // Set default sprites
      onPointerExitButton();

      // Set the map name
      mapNameText.text = voyage.areaName;

      // Set the plaque images
      statsPlaqueImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + "plaque_" + getFrameName(_voyage.difficulty));

      // Set the PvP-PvE label
      pvpPveText.text = voyage.isPvP ? "PvP" : "PvE";
      pvpPveModeImage.sprite = voyage.isPvP ? pvpIcon : pveIcon;

      // Set the player count
      playerCountText.text = (voyage.groupCount * Voyage.getMaxGroupSize(voyage.difficulty)) + "/" +
         Voyage.getMaxGroupsPerInstance(voyage.difficulty) * Voyage.getMaxGroupSize(voyage.difficulty);

      // Set the treasure site count
      if (voyage.treasureSiteCount == 0) {
         // If the number of sites is 0, the area has not yet been instantiated on the server
         treasureSiteCountText.text = "?/?";
      } else {
         treasureSiteCountText.text = (voyage.treasureSiteCount - voyage.capturedTreasureSiteCount).ToString() + "/" + voyage.treasureSiteCount;
      }

      // Set the time since the voyage started
      timeText.text = DateTime.UtcNow.Subtract(DateTime.FromBinary(voyage.creationDate)).ToString(@"mm\:ss");

      // Set the plaque labels outline color
      switch (voyage.difficulty) {
         case Voyage.Difficulty.Easy:
            playerCountOutline.effectColor = bronzeOutlineColor;
            pvpPveOutline.effectColor = bronzeOutlineColor;
            treasureSiteCountOutline.effectColor = bronzeOutlineColor;
            timeOutline.effectColor = bronzeOutlineColor;
            break;
         case Voyage.Difficulty.Medium:
            playerCountOutline.effectColor = silverOutlineColor;
            pvpPveOutline.effectColor = silverOutlineColor;
            treasureSiteCountOutline.effectColor = silverOutlineColor;
            timeOutline.effectColor = silverOutlineColor;
            break;
         case Voyage.Difficulty.Hard:
            playerCountOutline.effectColor = goldOutlineColor;
            pvpPveOutline.effectColor = goldOutlineColor;
            treasureSiteCountOutline.effectColor = goldOutlineColor;
            timeOutline.effectColor = goldOutlineColor;
            break;
         default:
            playerCountOutline.effectColor = Color.white;
            pvpPveOutline.effectColor = Color.white;
            treasureSiteCountOutline.effectColor = Color.white;
            timeOutline.effectColor = Color.white;
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
         SoundManager.play2DClip(SoundManager.Type.Layouts_Destinations);
      }
   }

   public void disablePointerEvents () {
      _interactable = false;
   }

   public static string getFrameName (Voyage.Difficulty voyageDifficulty) {
      switch (voyageDifficulty) {
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
