using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class PvpArenaPanel : Panel
{
   #region Public Variables

   // The container for the rows
   public GameObject rowsContainer;

   // The prefab we use for creating rows
   public PvpArenaRow pvpArenaRowPrefab;

   // Load Blocker when data is fetching
   public GameObject loadBlocker;

   // The sorting icons for each column
   [Header("Sorting icons")]
   public GameObject idAsc;
   public GameObject idDesc;
   public GameObject areaAsc;
   public GameObject areaDesc;
   public GameObject playerCountAsc;
   public GameObject playerCountDesc;
   public GameObject timeAsc;
   public GameObject timeDesc;
   public GameObject gameStateAsc;
   public GameObject gameStateDesc;

   // The sub-panel displaying an individual instance info
   public PvpArenaInfoPanel pvpArenaInfoPanel;

   // Self
   public static PvpArenaPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      // Clear out any current rows
      rowsContainer.DestroyChildren();
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
      rowsContainer.DestroyChildren();
      _rows.Clear();

      foreach (Voyage voyage in pvpArenaList) {
         PvpArenaRow row = Instantiate(pvpArenaRowPrefab, rowsContainer.transform);
         row.setRowForVoyage(voyage);
         _rows.Add(row);
      }

      sortByTime(true);
   }

   public void onReloadButtonPressed () {
      if (!loadBlocker.activeSelf) {
         refreshPanel();
      }
   }

   public void onPvpArenaRowPressed (Voyage voyage) {
      pvpArenaInfoPanel.updatePanelWithPvpArena(voyage);
   }

   public void joinPvpArena (Voyage voyage) {
      Global.player.rpc.Cmd_JoinPvpArena(voyage.voyageId);
      PanelManager.self.unlinkPanel();
   }

   public override void hide () {
      base.hide();
      pvpArenaInfoPanel.hide();
   }

   public void setLoadBlocker (bool isOn) {
      loadBlocker.SetActive(isOn);
   }

   public void sortById () {
      toggleIcons(idAsc, idDesc);
      _rows.Sort((a, b) => {
         return idAsc.activeSelf ? a.voyage.voyageId.CompareTo(b.voyage.voyageId) : b.voyage.voyageId.CompareTo(a.voyage.voyageId);
      });
      orderRows();
   }

   public void sortByArea () {
      toggleIcons(areaAsc, areaDesc);
      _rows.Sort((a, b) => {
         return areaAsc.activeSelf ? a.voyage.areaName.CompareTo(b.voyage.areaName) : b.voyage.areaName.CompareTo(a.voyage.areaName);
      });
      orderRows();
   }

   public void sortByPlayerCount () {
      _rows.Sort((a, b) => {
         return playerCountAsc.activeSelf ? a.voyage.playerCount.CompareTo(b.voyage.playerCount) : b.voyage.playerCount.CompareTo(a.voyage.playerCount);
      });
      orderRows();
   }

   public void sortByTime (bool forceDesc = false) {
      if (forceDesc) {
         timeAsc.SetActive(false);
         timeDesc.SetActive(true);
      } else {
         toggleIcons(timeAsc, timeDesc);
      }

      _rows.Sort((a, b) => {
         return timeAsc.activeSelf ? a.voyage.creationDate.CompareTo(b.voyage.creationDate) : b.voyage.creationDate.CompareTo(a.voyage.creationDate);
      });
      orderRows();
   }

   public void sortByGameState () {
      toggleIcons(gameStateAsc, gameStateDesc);
      _rows.Sort((a, b) => {
         return gameStateAsc.activeSelf ? a.voyage.pvpGameState.CompareTo(b.voyage.pvpGameState) : b.voyage.pvpGameState.CompareTo(a.voyage.pvpGameState);
      });
      orderRows();
   }

   private void toggleIcons (GameObject asc, GameObject desc) {
      if (!asc.activeSelf && !desc.activeSelf) {
         disableAllSortIcons();
         asc.SetActive(true);
      } else {
         bool nameAscBool = !asc.activeSelf;
         disableAllSortIcons();
         asc.SetActive(nameAscBool);
         desc.SetActive(!nameAscBool);
      }
   }

   private void disableAllSortIcons () {
      idAsc.SetActive(false);
      idDesc.SetActive(false);
      areaAsc.SetActive(false);
      areaDesc.SetActive(false);
      playerCountAsc.SetActive(false);
      playerCountDesc.SetActive(false);
      timeAsc.SetActive(false);
      timeDesc.SetActive(false);
      gameStateAsc.SetActive(false);
      gameStateDesc.SetActive(false);
   }

   private void orderRows () {
      for (int i = 0; i < _rows.Count; i++) {
         _rows[i].transform.SetSiblingIndex(i);
      }
   }

   #region Private Variables

   // References to the row gameobjects
   private List<PvpArenaRow> _rows = new List<PvpArenaRow>();

   #endregion
}

