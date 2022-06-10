using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class NoticeBoardPanel : Panel
{
   #region Public Variables

   public enum Mode {
      None = 0,
      PvpArena = 1,
      BiomeActivity = 2
   }

   // The biome activity tab section
   public BiomeActivityPanelSection biomeActivityPanelSection;

   // The pvp arena tab section
   public PvpArenaPanelSection pvpArenaSection;

   // The tab renderers
   public GameObject biomeActivityTab;
   public GameObject pvpArenaTab;

   // The tab buttons
   public Button biomeActivityTabUnderButton;
   public Button pvpArenaTabUnderButton;

   // Self
   public static NoticeBoardPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public override void Start () {
      base.Start();

      // Set the tab button click events
      biomeActivityTabUnderButton.onClick.RemoveAllListeners();
      pvpArenaTabUnderButton.onClick.RemoveAllListeners();

      biomeActivityTabUnderButton.onClick.AddListener(onBiomeActivityTabButtonPress);
      pvpArenaTabUnderButton.onClick.AddListener(onPvpArenaTabButtonPress);
   }

   public void refreshPanel (Mode mode) {
      if (mode == Mode.None) {
         mode = Mode.BiomeActivity;
         return;
      }

      switch (mode) {
         case Mode.PvpArena:
            biomeActivityPanelSection.hide();
            pvpArenaSection.show();
            break;
         case Mode.BiomeActivity:
            pvpArenaSection.hide();
            biomeActivityPanelSection.show();
            break;
         default:
            break;
      }

      updateTabs(mode);
   }

   public void onBiomeActivityTabButtonPress () {
      refreshPanel(Mode.BiomeActivity);
   }

   public void onPvpArenaTabButtonPress () {
      refreshPanel(Mode.PvpArena);
   }

   public void updateTabs (Mode mode) {
      switch (mode) {
         case Mode.PvpArena:
            biomeActivityTab.SetActive(false);
            pvpArenaTab.SetActive(true);

            biomeActivityTabUnderButton.gameObject.SetActive(true);
            pvpArenaTabUnderButton.gameObject.SetActive(false);
            break;
         case Mode.BiomeActivity:
            biomeActivityTab.SetActive(true);
            pvpArenaTab.SetActive(false);

            biomeActivityTabUnderButton.gameObject.SetActive(false);
            pvpArenaTabUnderButton.gameObject.SetActive(true);
            break;
         default:
            break;
      }
   }

   #region Private Variables

   // The current mode
   private Mode _mode = Mode.None;

   #endregion
}

