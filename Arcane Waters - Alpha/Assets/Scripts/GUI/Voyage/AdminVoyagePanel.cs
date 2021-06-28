using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class AdminVoyagePanel : Panel
{
   #region Public Variables

   // The container for the rows
   public GameObject rowsContainer;

   // The prefab we use for creating rows
   public AdminVoyageRow voyageInstanceRowPrefab;

   // Load Blocker when data is fetching
   public GameObject loadBlocker;

   // The auction info panel
   //public AuctionInfoPanel auctionInfoPanel;

   // The sorting icons for each column
   [Header("Sorting icons")]
   public GameObject idAsc;
   public GameObject idDesc;
   public GameObject areaAsc;
   public GameObject areaDesc;
   public GameObject leagueIndexAsc;
   public GameObject leagueIndexDesc;
   public GameObject difficultyAsc;
   public GameObject difficultyDesc;
   public GameObject playerCountAsc;
   public GameObject playerCountDesc;
   public GameObject enemyCountAsc;
   public GameObject enemyCountDesc;
   public GameObject isPvPAsc;
   public GameObject isPvPDesc;
   public GameObject timeAsc;
   public GameObject timeDesc;
   public GameObject biomeAsc;
   public GameObject biomeDesc;

   // The sub-panel displaying an individual voyage info
   public AdminVoyageInfoPanel adminVoyageInfoPanel;

   // Self
   public static AdminVoyagePanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      // Clear out any current rows
      rowsContainer.DestroyChildren();
   }

   public void togglePanel () {
      if (Global.player == null || !Global.player.isAdmin()) {
         return;
      }

      if (!isShowing()) {
         SoundEffectManager.self.playGuiMenuOpenSfx();

         refreshPanel();
      } else {
         PanelManager.self.togglePanel(Type.AdminVoyage);
      }
   }

   public void refreshPanel () {
      setLoadBlocker(true);
      Global.player.rpc.Cmd_RequestAdminVoyageListFromServer();
   }

   public void receiveVoyageInstancesFromServer (List<Voyage> voyageList) {
      setLoadBlocker(false);

      // Clear out any current rows
      rowsContainer.DestroyChildren();
      _rows.Clear();

      foreach (Voyage voyage in voyageList) {
         AdminVoyageRow row = Instantiate(voyageInstanceRowPrefab, rowsContainer.transform);
         row.setRowForVoyage(voyage);
         _rows.Add(row);
      }

      sortByPlayerCount(true);
   }

   public void onReloadButtonPressed () {
      if (!loadBlocker.activeSelf) {
         refreshPanel();
      }
   }

   public void onVoyageInstanceRowPressed (Voyage voyage) {
      adminVoyageInfoPanel.updatePanelWithVoyage(voyage);
   }

   public void warpToVoyage (Voyage voyage) {
      Global.player.rpc.Cmd_WarpAdminToVoyageInstanceAsGhost(voyage.voyageId, voyage.areaKey);
      PanelManager.self.unlinkPanel();
   }

   public override void hide () {
      base.hide();
      adminVoyageInfoPanel.hide();
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

   public void sortByLeagueIndex () {
      toggleIcons(leagueIndexAsc, leagueIndexDesc);
      _rows.Sort((a, b) => {
         return leagueIndexAsc.activeSelf ? a.voyage.leagueIndex.CompareTo(b.voyage.leagueIndex) : b.voyage.leagueIndex.CompareTo(a.voyage.leagueIndex);
      });
      orderRows();
   }

   public void sortByDifficulty () {
      toggleIcons(difficultyAsc, difficultyDesc);
      _rows.Sort((a, b) => {
         return difficultyAsc.activeSelf ? a.voyage.difficulty.CompareTo(b.voyage.difficulty) : b.voyage.difficulty.CompareTo(a.voyage.difficulty);
      });
      orderRows();
   }

   public void sortByPlayerCount (bool forceDesc = false) {
      if (forceDesc) {
         disableAllSortIcons();
         playerCountAsc.SetActive(false);
         playerCountDesc.SetActive(true);
      } else {
         toggleIcons(playerCountAsc, playerCountDesc);
      }
      _rows.Sort((a, b) => {
         return playerCountAsc.activeSelf ? a.voyage.playerCount.CompareTo(b.voyage.playerCount) : b.voyage.playerCount.CompareTo(a.voyage.playerCount);
      });
      orderRows();
   }

   public void sortByEnemyCount () {
      toggleIcons(enemyCountAsc, enemyCountDesc);
      _rows.Sort((a, b) => {
         return enemyCountAsc.activeSelf ? a.voyage.aliveNPCEnemyCount.CompareTo(b.voyage.aliveNPCEnemyCount) : b.voyage.aliveNPCEnemyCount.CompareTo(a.voyage.aliveNPCEnemyCount);
      });
      orderRows();
   }

   public void sortByIsPvP () {
      toggleIcons(isPvPAsc, isPvPDesc);
      _rows.Sort((a, b) => {
         return isPvPAsc.activeSelf ? a.voyage.isPvP.CompareTo(b.voyage.isPvP) : b.voyage.isPvP.CompareTo(a.voyage.isPvP);
      });
      orderRows();
   }

   public void sortByTime () {
      toggleIcons(timeAsc, timeDesc);
      _rows.Sort((a, b) => {
         return timeAsc.activeSelf ? a.voyage.creationDate.CompareTo(b.voyage.creationDate) : b.voyage.creationDate.CompareTo(a.voyage.creationDate);
      });
      orderRows();
   }

   public void sortByBiome () {
      toggleIcons(biomeAsc, biomeDesc);
      _rows.Sort((a, b) => {
         return biomeAsc.activeSelf ? a.voyage.biome.CompareTo(b.voyage.biome) : b.voyage.biome.CompareTo(a.voyage.biome);
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
      leagueIndexAsc.SetActive(false);
      leagueIndexDesc.SetActive(false);
      difficultyAsc.SetActive(false);
      difficultyDesc.SetActive(false);
      playerCountAsc.SetActive(false);
      playerCountDesc.SetActive(false);
      enemyCountAsc.SetActive(false);
      enemyCountDesc.SetActive(false);
      isPvPAsc.SetActive(false);
      isPvPDesc.SetActive(false);
      timeAsc.SetActive(false);
      timeDesc.SetActive(false);
      biomeAsc.SetActive(false);
      biomeDesc.SetActive(false);
   }

   private void orderRows () {
      for (int i = 0; i < _rows.Count; i++) {
         _rows[i].transform.SetSiblingIndex(i);
      }
   }

   #region Private Variables

   // References to the row gameobjects
   private List<AdminVoyageRow> _rows = new List<AdminVoyageRow>();

   #endregion
}
