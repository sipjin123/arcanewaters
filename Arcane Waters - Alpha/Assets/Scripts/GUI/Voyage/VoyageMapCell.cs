using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using ProceduralMap;
using Mirror;

public class VoyageMapCell : MonoBehaviour {
   #region Public Variables

   // The biome image
   public Image biomeImage;

   // The frame image
   public Image frameImage;

   // The map name
   public Text mapNameText;

   // The PvP-PvE plaque image
   public Image pvpPvePlaque;

   // The player count plaque image
   public Image playerCountPlaque;

   // The PvP-PvE text
   public Text pvpPveText;
   
   // The player count text
   public Text playerCountText;

   // The PvP-PvE text outline
   public Outline pvpPveOutline;

   // The player count text outline
   public Outline playerCountOutline;

   // The colors for the plaque text outline
   public Color bronzeOutlineColor;
   public Color silverOutlineColor;
   public Color goldOutlineColor;

   #endregion

   public void setCellForVoyage (Voyage voyage) {
      _voyage = voyage;

      // Set default sprites
      OnPointerExit();

      // Set the map name
      mapNameText.text = Area.getName(voyage.areaKey);

      // Set the plaque images
      pvpPvePlaque.sprite = ImageManager.getSprite(SEA_MAP_PATH + "plaque_" + getFrameName());
      playerCountPlaque.sprite = ImageManager.getSprite(SEA_MAP_PATH + "count_plaque_" + getFrameName());

      // Set the PvP-PvE label
      if (voyage.isPvP) {
         pvpPveText.text = "PvP";
      } else {
         pvpPveText.text = "PvE";
      }

      // Set the player count label
      playerCountText.text = Voyage.getMaxGroupSize(voyage.difficulty).ToString();

      // Set the plaque labels outline color
      switch (voyage.difficulty) {
         case Voyage.Difficulty.Easy:
            playerCountOutline.effectColor = bronzeOutlineColor;
            pvpPveOutline.effectColor = bronzeOutlineColor;
            break;
         case Voyage.Difficulty.Medium:
            playerCountOutline.effectColor = silverOutlineColor;
            pvpPveOutline.effectColor = silverOutlineColor;
            break;
         case Voyage.Difficulty.Hard:
            playerCountOutline.effectColor = goldOutlineColor;
            pvpPveOutline.effectColor = goldOutlineColor;
            break;
         default:
            playerCountOutline.effectColor = Color.white;
            pvpPveOutline.effectColor = Color.white;
            break;
      }
   }

   public void OnPointerEnter () {
      if (_interactable) {
         // Change frame sprite - HOVERED
         frameImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getFrameName() + "_frame_hover");

         // Change biome sprite - HOVERED
         biomeImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getBiomeName() + "_hover");
      }
   }

   public void OnPointerExit () {
      if (_interactable) {
         // Change frame sprite - DEFAULT
         frameImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getFrameName() + "_frame_default");

         // Change biome sprite - DEFAULT
         biomeImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getBiomeName() + "_default");
      }
   }

   public void OnPointerDown () {
      if (_interactable) {
         // Change frame sprite - PRESSED
         frameImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getFrameName() + "_frame_pressed");

         // Change biome sprite - PRESSED
         biomeImage.sprite = ImageManager.getSprite(SEA_MAP_PATH + getBiomeName() + "_pressed");
      }
   }

   public void OnPointerUp () {
      if (_interactable) {
         // Set default sprites
         OnPointerExit();
      }
   }

   public void OnPointerClick () {
      if (_interactable) {
         VoyagePanel.self.selectVoyageMap(_voyage);
      }
   }

   public void disablePointerEvents () {
      _interactable = false;
   }

   private string getFrameName () {
      switch (_voyage.difficulty) {
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
      return Area.getBiome(_voyage.areaKey).ToString().ToLower();
   }

   #region Private Variables

   // Path for Sea Random map sprites
   private static string SEA_MAP_PATH = "GUI/Voyages/";

   // The voyage being displayed
   private Voyage _voyage;

   // Gets set to true when the cell reacts to pointer events
   private bool _interactable = true;

   #endregion
}
