using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class PvpArenaPanel : Panel
{
   #region Public Variables

   // The container for the cells
   public GameObject cellContainer;

   // The prefab we use for creating cells
   public PvpArenaCell pvpArenaCellPrefab;

   // Load Blocker when data is fetching
   public GameObject loadBlocker;

   // The sub-panel displaying an individual instance info
   public PvpArenaInfoPanel pvpArenaInfoPanel;

   // Self
   public static PvpArenaPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      // Clear out any current rows
      cellContainer.DestroyChildren();
   }

   public void togglePanel () {
      if (Global.player == null) {
         return;
      }

      if (!isShowing()) {
         SoundEffectManager.self.playGuiMenuOpenSfx();

         refreshPanel();
      } else {
         PanelManager.self.togglePanel(Type.PvpArena);
      }
   }

   public void refreshPanel () {
      setLoadBlocker(true);
      Global.player.rpc.Cmd_RequestPvpArenaListFromServer();
   }

   public void receivePvpArenasFromServer (List<Voyage> pvpArenaList) {
      setLoadBlocker(false);

      // Clear out any current rows
      cellContainer.DestroyChildren();

      // Sort by time
      pvpArenaList.Sort((a, b) => { return a.creationDate.CompareTo(b.creationDate); });

      foreach (Voyage pvpArena in pvpArenaList) {
         PvpArenaCell cell = Instantiate(pvpArenaCellPrefab, cellContainer.transform);
         cell.setCellForPvpArena(pvpArena, () => onPvpArenaRowPressed(pvpArena));
      }
   }

   public void onReloadButtonPressed () {
      if (!loadBlocker.activeSelf) {
         refreshPanel();
      }
   }

   public void onPvpArenaRowPressed (Voyage voyage) {
      if (Global.player.isDemoUser) {
         if (voyage.biome != Biome.Type.Forest && voyage.biome != Biome.Type.Desert) {
            PanelManager.self.noticeScreen.show("Target biome is closed for demo");
            return;
         }
      }

      Global.player.rpc.Cmd_RequestPvpArenaInfoFromServer(voyage.voyageId);
   }

   public void joinPvpArena (Voyage voyage, PvpTeamType team) {
      if (Global.player.isDemoUser) {
         if (voyage.biome != Biome.Type.Forest && voyage.biome != Biome.Type.Desert) {
            PanelManager.self.noticeScreen.show("Target biome is closed for demo");
            return;
         }
      }

      Global.player.rpc.Cmd_JoinPvpArena(voyage.voyageId, team);
      PanelManager.self.unlinkPanel();
   }

   public override void hide () {
      base.hide();
      pvpArenaInfoPanel.hide();
   }

   public void setLoadBlocker (bool isOn) {
      loadBlocker.SetActive(isOn);
   }

   #region Private Variables

   #endregion
}

