using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class TradeHistoryPanel : Panel
{
   #region Public Variables

   // The prefab we use for creating rows
   public TradeHistoryRow rowPrefab;

   // The container for our rows
   public GameObject rowsContainer;

   // The page number text
   public Text pageNumber;

   // The next page button
   public Button nextPageButton;

   // The previous page button
   public Button previousPageButton;

   // Self
   public static TradeHistoryPanel self;

   // The number of rows per page
   public static int ROWS_PER_PAGE = 8;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public void updatePanelWithTrades (List<TradeHistoryInfo> tradeList, int pageIndex, int totalTradeCount) {
      // Clear out any old info
      rowsContainer.DestroyChildren();

      // Sets the current page
      _currentPage = pageIndex;

      for (int i = 0; i < tradeList.Count; i++) {
         // Look up the info for this row
         TradeHistoryInfo tradeInfo = tradeList[i];

         // Create a new row
         TradeHistoryRow row = Instantiate(rowPrefab, rowsContainer.transform, false);
         row.transform.SetParent(rowsContainer.transform, false);
         row.setRowForItem(tradeInfo);
      }

      // Calculate the maximum page index
      _maxPage = Mathf.FloorToInt((float) totalTradeCount / ROWS_PER_PAGE);

      // Update the current page text
      pageNumber.text = "Page " + (_currentPage + 1).ToString() + " of " + (_maxPage + 1).ToString();

      // Update the navigation buttons
      updateNavigationButtons();
   }

   public void nextPage () {
      if (_currentPage < _maxPage) {
         _currentPage++;
         Global.player.rpc.Cmd_RequestTradeHistoryInfoFromServer(_currentPage, ROWS_PER_PAGE);
      }
   }

   public void previousPage () {
      if (_currentPage > 0) {
         _currentPage--;
         Global.player.rpc.Cmd_RequestTradeHistoryInfoFromServer(_currentPage, ROWS_PER_PAGE);
      }
   }

   private void updateNavigationButtons () {
      // Activate or deactivate the navigation buttons if we reached a limit
      previousPageButton.enabled = true;
      nextPageButton.enabled = true;

      if (_currentPage <= 0) {
         previousPageButton.enabled = false;
      }

      if (_currentPage >= _maxPage) {
         nextPageButton.enabled = false;
      }
   }

   #region Private Variables

   // The index of the current page
   private int _currentPage = 0;

   // The maximum page index (starting at 0)
   private int _maxPage = 0;

   #endregion
}
