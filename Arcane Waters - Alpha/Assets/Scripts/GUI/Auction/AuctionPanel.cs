using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class AuctionPanel : Panel
{
   #region Public Variables

   // The number of items to display per page
   public static int ROWS_PER_PAGE = 30;

   // The list filters
   public enum ListFilter
   {
      AllActive = 0,
      MyAuctions = 1,
      MyBids = 2
   }

   // The container for the rows
   public GameObject rowsContainer;

   // The prefab we use for creating rows
   public AuctionRow auctionRowPrefab;

   // Load Blocker when data is fetching
   public GameObject loadBlocker;

   // The page number text
   public Text pageNumberText;

   // The next page button
   public Button nextPageButton;

   // The previous page button
   public Button previousPageButton;

   // The item tab filters
   public ItemTabs itemTabs;

   // When set to true, all active auctions will be listed
   public Toggle allToggle;

   // When set to true, only the user's auctions will be listed
   public Toggle myAuctionsToggle;

   // When set to true, only the auctions the user has bid on will be listed
   public Toggle myBidsToggle;

   // The auction info panel
   public AuctionInfoPanel auctionInfoPanel;

   // Self
   public static AuctionPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      itemTabs.initialize(onTabButtonPress);

      // Clear out any current rows
      rowsContainer.DestroyChildren();
   }

   public void displayMyAuctions () {
      myAuctionsToggle.SetIsOnWithoutNotify(true);
      onMyAuctionsToggleValueChanged();
   }

   public void displayAllAuctions () {
      allToggle.SetIsOnWithoutNotify(true);
      onAllToggleValueChanged();
   }

   public void refreshPanel () {
      loadBlocker.SetActive(true);
      NubisDataFetcher.self.getAuctionList(_currentPage, ROWS_PER_PAGE, itemTabs.categoryFilters.ToArray(), _currentFilter);
   }

   public void receiveAuctionsFromServer (List<AuctionItemData> auctionList, Item.Category[] categoryFilters, ListFilter auctionFilter, int pageNumber, int totalAuctionCount, int userGold) {
      // Force the toggle values so that it corresponds to the received list
      switch (auctionFilter) {
         case ListFilter.AllActive:
            allToggle.SetIsOnWithoutNotify(true);
            break;
         case ListFilter.MyAuctions:
            myAuctionsToggle.SetIsOnWithoutNotify(true);
            break;
         case ListFilter.MyBids:
            myBidsToggle.SetIsOnWithoutNotify(true);
            break;
         default:
            break;
      }

      itemTabs.updateCategoryTabs(categoryFilters[0]);

      setLoadBlocker(false);

      // Update the page number and navigation buttons
      _currentPage = pageNumber;
      _maxPage = Mathf.CeilToInt((float) totalAuctionCount / ROWS_PER_PAGE);
      if (_maxPage == 0) {
         _maxPage = 1;
      }
      pageNumberText.text = "Page " + _currentPage.ToString() + " of " + _maxPage.ToString();
      updateNavigationButtons();

      // Clear out any current rows
      rowsContainer.DestroyChildren();

      foreach (AuctionItemData auction in auctionList) {
         AuctionRow row = Instantiate(auctionRowPrefab, rowsContainer.transform);
         row.setRowForAuction(auction);
      }

      Global.lastUserGold = userGold;
   }

   public void onTabButtonPress () {
      _currentPage = 1;
      refreshPanel();
   }

   public void onAllToggleValueChanged () {
      if (allToggle.isOn) {
         _currentPage = 1;
         _currentFilter = ListFilter.AllActive;
         refreshPanel();
      }
   }

   public void onMyAuctionsToggleValueChanged () {
      if (myAuctionsToggle.isOn) {
         _currentPage = 1;
         _currentFilter = ListFilter.MyAuctions;
         refreshPanel();
      }
   }

   public void onMyBidsToggleValueChanged () {
      if (myBidsToggle.isOn) {
         _currentPage = 1;
         _currentFilter = ListFilter.MyBids;
         refreshPanel();
      }
   }

   public void onReloadButtonPressed () {
      if (!loadBlocker.activeSelf) {
         refreshPanel();
      }
   }

   public void onAuctionItemButtonPressed () {
      auctionInfoPanel.showPanelForCreateAuction();
   }

   public void onAuctionRowPressed (AuctionItemData auction) {
      auctionInfoPanel.displayAuction(auction.auctionId);
   }

   public override void hide () {
      base.hide();
      auctionInfoPanel.hide();
   }

   public void setLoadBlocker (bool isOn) {
      loadBlocker.SetActive(isOn);
   }

   public void nextPage () {
      if (_currentPage < _maxPage) {
         _currentPage++;
         refreshPanel();
      }
   }

   public void previousPage () {
      if (_currentPage > 1) {
         _currentPage--;
         refreshPanel();
      }
   }

   private void updateNavigationButtons () {
      // Activate or deactivate the navigation buttons if we reached a limit
      previousPageButton.enabled = true;
      nextPageButton.enabled = true;

      if (_currentPage <= 1) {
         previousPageButton.enabled = false;
      }

      if (_currentPage >= _maxPage) {
         nextPageButton.enabled = false;
      }
   }

   #region Private Variables

   // The index of the current page
   private int _currentPage = 1;

   // The maximum page index (starting at 1)
   private int _maxPage = 1;

   // The main auction list filter
   private ListFilter _currentFilter = ListFilter.AllActive;

   #endregion
}
