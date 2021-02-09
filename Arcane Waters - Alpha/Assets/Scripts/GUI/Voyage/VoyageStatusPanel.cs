﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class VoyageStatusPanel : ClientMonoBehaviour
{
   #region Public Variables

   // Our Canvas Group
   public CanvasGroup canvasGroup;

   // When the mouse is over this defined zone, we consider that it hovers the panel
   public RectTransform panelHoveringZone;

   // The section that is only visible when the mouse is over the panel
   public GameObject collapsingContainer;

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

   // The alive enemies count
   public Text aliveEnemiesCountText;

   // The index of the current league
   public Text leagueIndexText;

   // The text outlines
   public Outline[] outlines;

   // The colors for the outlines
   public Color bronzeOutlineColor;
   public Color silverOutlineColor;
   public Color goldOutlineColor;

   // The icons for PvP and PvE modes
   public Sprite pvpIcon;
   public Sprite pveIcon;

   // The statuses that are only relevant in voyage instances or league instances (common statuses should not be set here)
   public GameObject[] voyageStatuses = new GameObject[0];
   public GameObject[] leagueStatuses = new GameObject[0];

   // Self
   public static VoyageStatusPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void Start () {
      // Hide the panel by default
      hide();
   }

   public void Update () {
      // Hide the panel in certain situations
      if (Global.player == null || !VoyageGroupManager.isInGroup(Global.player) || Global.player.isInBattle()) {
         hide();
         return;
      }

      // Show the panel
      show();

      // Collapse the panel if the mouse is not over it
      if (RectTransformUtility.RectangleContainsScreenPoint(panelHoveringZone, Input.mousePosition)) {
         collapsingContainer.SetActive(true);
      } else {
         collapsingContainer.SetActive(false);
      }

      // Get the current instance
      Instance instance = Global.player.getInstance();

      // Set a default status if this is not the voyage instance
      if (instance == null || !instance.isVoyage) {
         setDefaultStatus();
         return;
      }

      // Show different relevant statuses for voyage and league instances
      if (instance.isLeague) {
         foreach (GameObject gO in voyageStatuses) {
            if (gO.activeSelf) {
               gO.SetActive(false);
            }
         }

         foreach (GameObject gO in leagueStatuses) {
            if (!gO.activeSelf) {
               gO.SetActive(true);
            }
         }
      } else {
         foreach (GameObject gO in leagueStatuses) {
            if (gO.activeSelf) {
               gO.SetActive(false);
            }
         }

         foreach (GameObject gO in voyageStatuses) {
            if (!gO.activeSelf) {
               gO.SetActive(true);
            }
         }
      }

      // Set the status that are always visible
      if (instance.treasureSiteCount == 0) {
         // If the number of sites is 0, the area has not yet been instantiated on the server
         treasureSiteCountText.text = "?/?";
      } else {
         treasureSiteCountText.text = (instance.treasureSiteCount - instance.capturedTreasureSiteCount).ToString() + "/" + instance.treasureSiteCount;
      }
      aliveEnemiesCountText.text = instance.aliveNPCEnemiesCount.ToString() + "/" + (instance.getTotalNPCEnemyCount()).ToString();

      // Set the plaque labels outline color
      switch (Voyage.getDifficultyEnum(instance.difficulty)) {
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

      statsPlaqueImage.sprite = ImageManager.getSprite(VoyageMapCell.SEA_MAP_PATH + "plaque_" + VoyageMapCell.getFrameName(instance.difficulty));

      // If the panel is collapsed, there is no need to update the rest
      if (!collapsingContainer.activeSelf) {
         return;
      }

      // Update the voyage status
      //mapNameText.text = Area.getName(instance.areaKey);
      pvpPveText.text = instance.isPvP ? "PvP" : "PvE";
      pvpPveModeImage.sprite = instance.isPvP ? pvpIcon : pveIcon;
      playerCountText.text = EntityManager.self.getEntityCount() + "/" + instance.getMaxPlayers();
      timeText.text = (DateTime.UtcNow - DateTime.FromBinary(instance.creationDate)).ToString(@"mm\:ss");
      leagueIndexText.text = (instance.leagueIndex + 1) + " of " + Voyage.MAPS_PER_LEAGUE;
   }

   private void setDefaultStatus () {
      pvpPveText.text = "?";
      playerCountText.text = "?/?";
      treasureSiteCountText.text = "?/?";
      timeText.text = "?";
      aliveEnemiesCountText.text = "?/?";
      leagueIndexText.text = "? of ?";
   }

   public void show () {
      if (this.canvasGroup.alpha < 1f) {
         this.canvasGroup.alpha = 1f;
         this.canvasGroup.blocksRaycasts = true;
         this.canvasGroup.interactable = true;
         setDefaultStatus();
      }
   }

   public void hide () {
      if (this.canvasGroup.alpha > 0f) {
         this.canvasGroup.alpha = 0f;
         this.canvasGroup.blocksRaycasts = false;
         this.canvasGroup.interactable = false;
         setDefaultStatus();
      }
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   #region Private Variables

   #endregion
}
