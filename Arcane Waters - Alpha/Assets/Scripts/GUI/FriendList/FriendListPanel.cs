using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;

public class FriendListPanel : Panel
{
   #region Public Variables

   // The number of rows to display per page, when displaying friendship requests
   public static int ROWS_PER_PAGE_FOR_REQUESTS = 11;

   // The container of the friend rows
   public GameObject friendRowsContainer;

   // The container of the friend rows
   public GameObject friendRequestRowsContainer;

   // The container of steam friends
   public Transform steamFriendsContainer;

   // The prefab we use for creating friend rows
   public FriendListRow friendRowPrefab;

   // The prefab we use for creating received friend request rows
   public FriendshipRequestReceivedRow friendshipRequestReceivedRowPrefab;

   // The prefab we use for creating sent friend request rows
   public FriendshipRequestSentRow friendshipRequestSentRowPrefab;

   // The prefab for creating a steam friend row
   public SteamFriendRow steamFriendRowPrefab;

   // The object wrapping the list used to display friends
   public GameObject friendList;

   // The object wrapping the list used to display friendship requests
   public GameObject requestList;

   // The object wrapping the list used to display the search result
   public GameObject searchResultsList;

   // The object wrapping the list of steam friends
   public GameObject steamFriendsList;

   // The input field holding the name of the user to send a friendship invitation
   public InputField inviteInputField;

   // Both texts of the 'Friends' tab
   public Text friendsTabText;
   public Text friendsTabUnderText;

   // Both texts of the 'Pending' tab
   public Text requestReceivedTabText;
   public Text requestReceivedTabUnderText;

   // The page number text
   public Text pageNumberText;

   // The next page button
   public Button nextPageButton;

   // The previous page button
   public Button previousPageButton;

   // The tab canvas groups
   public CanvasGroup friendListTabCanvasGroup;
   public CanvasGroup requestReceivedTabCanvasGroup;
   public CanvasGroup requestSentTabCanvasGroup;
   public CanvasGroup searchResultsTabCanvasGroup;
   public CanvasGroup steamFriendsTabCanvasGroup;

   // The tab buttons
   public Button friendListTabButton;
   public Button requestReceivedTabButton;
   public Button requestSentTabButton;
   public Button searchResultsTabButton;
   public Button steamFriendsTabButton;

   // Reference to the load blocker
   public GameObject loadBlocker;

   // Self
   public static FriendListPanel self;

   [Header("Search")]
   // The prefab used to create search results
   public FriendSearchRow SearchResultRowPrefab;

   // Container for the search results
   public GameObject searchResultContainer;

   // The gameobject that contains the invite controls
   public GameObject sendInvitesSection;

   // The previous page button (Search)
   public Button searchPreviousPageButton;

   // The next page button (Search)
   public Button searchNextPageButton;

   // The text displaying the current page (Search)
   public Text searchCurrentPageIndicatorText;

   // The text that displays the search title
   public Text searchTitle;

   // The amount of entries displayed in each search results page
   public int searchResultsPerPage = 10;

   // The Panel tabs
   public enum FriendshipPanelTabs
   {
      // None
      None = 0,

      // Friends
      Friends = 1,

      // Invites - Sent
      InvitesSent = 2,

      // Invites - Received
      InvitesReceived = 3,

      // Search
      Search = 4,

      // Steam friends
      SteamFriends = 5
   }

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void refreshPanel (bool clearInputFields = false, FriendshipPanelTabs? desiredTab = null) {
      toggleBlocker();

      if (clearInputFields) {
         inviteInputField.text = "";
      }

      if (desiredTab != null && desiredTab.HasValue) {
         this._currentTab = desiredTab.Value;

         switch (desiredTab.Value) {
            case FriendshipPanelTabs.Friends:
               _friendshipStatusFilter = Friendship.Status.Friends;
               break;
            case FriendshipPanelTabs.InvitesSent:
               _friendshipStatusFilter = Friendship.Status.InviteSent;
               break;
            case FriendshipPanelTabs.InvitesReceived:
               _friendshipStatusFilter = Friendship.Status.InviteReceived;
               break;
         }
      }

      Global.player.rpc.Cmd_RequestFriendshipInfoFromServer(_currentPage, _rowsPerPage, _friendshipStatusFilter, _currentTab == FriendshipPanelTabs.SteamFriends);
   }

   public void updatePanelWithFriendshipInfo (List<FriendshipInfo> friendshipInfoList, Friendship.Status friendshipStatus,
      int pageNumber, int totalFriendInfoCount, int friendCount, int pendingRequestCount, bool isSteamFriendsTab) {
      _friendshipStatusFilter = friendshipStatus;
      FriendListManager.self.cachedFriendshipInfoList = friendshipInfoList;
      if (FriendListManager.self.cachedFriendshipInfoList == null) {
         FriendListManager.self.cachedFriendshipInfoList = new List<FriendshipInfo>();
      }

      if (_currentTab == FriendshipPanelTabs.None) {
         _currentTab = FriendshipPanelTabs.Friends;
      }

      // Check if the list is for friends or friendship requests
      if (friendshipStatus == Friendship.Status.Friends) {
         // Sort the list by online status
         friendshipInfoList = friendshipInfoList.OrderByDescending(f => f.isOnline).ToList();

         // Update the rows per page
         _rowsPerPage = Friendship.MAX_FRIENDS;
      } else {
         // Update the current page number
         _currentPage = pageNumber;

         // Update the rows per page
         _rowsPerPage = ROWS_PER_PAGE_FOR_REQUESTS;

         // Calculate the maximum page number
         _maxPage = Mathf.CeilToInt((float) totalFriendInfoCount / _rowsPerPage);
         if (_maxPage == 0) {
            _maxPage = 1;
         }

         // Update the current page text
         pageNumberText.text = "Page " + _currentPage.ToString() + " of " + _maxPage.ToString();

         // Update the navigation buttons
         updateNavigationButtons();
      }

      // Update the friends tab text
      friendsTabText.text = "Friends * " + friendCount.ToString() + "/" + Friendship.MAX_FRIENDS.ToString();
      friendsTabUnderText.text = friendsTabText.text;

      // Update the pending tab text
      requestReceivedTabText.text = "Pending * " + pendingRequestCount.ToString();
      requestReceivedTabUnderText.text = requestReceivedTabText.text;

      // Clear out any current items
      friendRowsContainer.DestroyChildren();
      friendRequestRowsContainer.DestroyChildren();
      steamFriendsContainer.gameObject.DestroyChildren();
      _steamFriendRows.Clear();

      disableAllTabs();
      hideAllContent();
      activateAllTabs();

      // Select the correct tab
      switch (_currentTab) {
         case FriendshipPanelTabs.Friends:
            friendListTabCanvasGroup.alpha = 1f;
            friendList.SetActive(true);
            break;
         case FriendshipPanelTabs.InvitesReceived:
            requestReceivedTabCanvasGroup.alpha = 1f;
            requestList.SetActive(true);
            break;
         case FriendshipPanelTabs.InvitesSent:
            requestSentTabCanvasGroup.alpha = 1f;
            requestList.SetActive(true);
            break;
         case FriendshipPanelTabs.Search:
            searchResultsTabCanvasGroup.alpha = 1f;
            searchResultsList.SetActive(true);
            break;
         case FriendshipPanelTabs.SteamFriends:
            steamFriendsTabCanvasGroup.alpha = 1f;
            steamFriendsList.SetActive(true);
            break;
         default:
            break;
      }

      if (isSteamFriendsTab) {
         List<SteamFriendData> datas = SteamFriendsManager.getSteamFriends();
         foreach (SteamFriendData data in datas) {
            SteamFriendRow row = Instantiate(steamFriendRowPrefab, steamFriendsContainer, false);
            _steamFriendRows.Add(data.steamId, row);
            row.setData(data);
         }
         SteamFriendsManager.requestFriendListImages(datas);
      } else {
         // Create the friend rows
         foreach (FriendshipInfo friend in friendshipInfoList) {
            // Use different prefabs and parameters for each tab
            switch (friendshipStatus) {
               case Friendship.Status.InviteSent:
                  // Instantiate and initialize the row
                  FriendshipRequestSentRow rowSent = Instantiate(friendshipRequestSentRowPrefab, friendRequestRowsContainer.transform, false);
                  rowSent.setRowForFriendshipInfo(friend);
                  break;
               case Friendship.Status.InviteReceived:
                  // Instantiate and initialize the row
                  FriendshipRequestReceivedRow rowReceived = Instantiate(friendshipRequestReceivedRowPrefab, friendRequestRowsContainer.transform, false);
                  rowReceived.setRowForFriendshipInfo(friend);
                  break;
               case Friendship.Status.Friends:
                  // Instantiate and initialize the row
                  FriendListRow rowFriend = Instantiate(friendRowPrefab, friendRowsContainer.transform, false);
                  rowFriend.setRowForFriendshipInfo(friend);
                  break;
               default:
                  break;
            }
         }
      }

      // Show the Search Tab only if search results are being displayed
      searchResultsTabButton.gameObject.SetActive(_currentTab == FriendshipPanelTabs.Search);

      // Update the pending friendship request notification
      BottomBar.self.setFriendshipRequestNotificationStatus(pendingRequestCount > 0);
      sendInvitesSection.SetActive(_currentTab != FriendshipPanelTabs.Search);
      toggleBlocker(show: false);
   }

   public void receiveSteamFriendAvatar (SteamFriendData friend, Sprite avatar) {
      if (_steamFriendRows.TryGetValue(friend.steamId, out SteamFriendRow row)) {
         row.setAvatarImage(avatar);
      }
   }

   #region UI Callbacks

   public void onFriendListTabButtonPress () {
      _currentTab = FriendshipPanelTabs.Friends;
      _friendshipStatusFilter = Friendship.Status.Friends;
      _currentPage = 1;
      _rowsPerPage = Friendship.MAX_FRIENDS;
      refreshPanel();
   }

   public void onInvitesReceivedTabButtonPress () {
      _currentTab = FriendshipPanelTabs.InvitesReceived;
      _friendshipStatusFilter = Friendship.Status.InviteReceived;
      _currentPage = 1;
      _rowsPerPage = ROWS_PER_PAGE_FOR_REQUESTS;
      refreshPanel();
   }

   public void onInvitesSentTabButtonPress () {
      _currentTab = FriendshipPanelTabs.InvitesSent;
      _friendshipStatusFilter = Friendship.Status.InviteSent;
      _currentPage = 1;
      _rowsPerPage = ROWS_PER_PAGE_FOR_REQUESTS;
      refreshPanel();
   }

   public void onSteamFriendsTabButtonPress () {
      _currentTab = FriendshipPanelTabs.SteamFriends;
      _friendshipStatusFilter = Friendship.Status.None;
      _currentPage = 1;
      _rowsPerPage = ROWS_PER_PAGE_FOR_REQUESTS;
      refreshPanel();
   }

   public void onSendInviteButtonPress () {
      if ("".Equals(inviteInputField.text)) {
         PanelManager.self.noticeScreen.show("Please enter a player name.");
      } else {
         FriendListManager.self.sendFriendshipInvite(inviteInputField.text);
      }
   }

   public void onChatButtonPress (int friendUserId, string friendName) {
      ChatPanel.self.sendWhisperTo(friendName);
      close();
   }

   public void onSendMessageButtonPress (string friendName) {
      // Enables the mail panel in mode 'write mail'
      ((MailPanel) PanelManager.self.get(Panel.Type.Mail)).composeMailTo(friendName);
   }

   public void onAcceptFriendshipRequestButtonPress (int friendUserId) {
      Global.player.rpc.Cmd_AcceptFriendshipInvite(friendUserId);
   }

   public void onDeleteFriendButtonPress (int friendUserId, string friendName) {
      // Make sure the value of the friend user id is captured
      int friendUserIdForButtonEvent = friendUserId;

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => FriendListManager.self.confirmDeleteFriend(friendUserIdForButtonEvent));

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to delete your friend " + friendName + "?");
   }

   public void onRejectFriendshipRequestButtonPress (int friendUserId, string friendName) {
      // Make sure the value of the friend user id is captured
      int friendUserIdForButtonEvent = friendUserId;

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => FriendListManager.self.confirmDeleteFriend(friendUserIdForButtonEvent));

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to reject the friendship invitation from " + friendName + "?");
   }

   public void onCancelFriendshipRequestButtonPress (int friendUserId, string friendName) {
      // Make sure the value of the friend user id is captured
      int friendUserIdForButtonEvent = friendUserId;

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => FriendListManager.self.confirmDeleteFriend(friendUserIdForButtonEvent));

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to cancel the friendship invitation to " + friendName + "?");
   }

   #endregion

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

   public void toggleBlocker (bool show = true) {
      if (loadBlocker) {
         loadBlocker.SetActive(show);
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

   private void hideAllContent () {
      friendList.SetActive(false);
      requestList.SetActive(false);
      searchResultsList.SetActive(false);
      steamFriendsList.SetActive(false);
   }

   private void disableAllTabs () {
      // Disables tabs
      friendListTabButton.interactable = false;
      requestReceivedTabButton.interactable = false;
      requestSentTabButton.interactable = false;
      searchResultsTabButton.interactable = false;
      steamFriendsTabButton.interactable = false;

      friendListTabCanvasGroup.alpha = 0.0f;
      requestReceivedTabCanvasGroup.alpha = 0.0f;
      requestSentTabCanvasGroup.alpha = 0.0f;
      searchResultsTabCanvasGroup.alpha = 0.0f;
      steamFriendsTabCanvasGroup.alpha = 0.0f;
   }

   private void activateAllTabs () {
      // Makes all tabs interactable
      friendListTabButton.interactable = true;
      requestReceivedTabButton.interactable = true;
      requestSentTabButton.interactable = true;
      searchResultsTabButton.interactable = true;
      steamFriendsTabButton.interactable = true;
   }

   #region User Search

   private void repopulateSearchResults (int page) {
      if (_resultCollection == null || _resultCollection.results == null || searchResultContainer == null) {
         return;
      }

      clearSearchResults();
      _currentSearchResultsPage = page;
      UserSearchResult[] results = Util.getArraySlice(_resultCollection.results, page, searchResultsPerPage);

      foreach (UserSearchResult result in results) {
         FriendSearchRow row = Instantiate(SearchResultRowPrefab, searchResultContainer.transform);
         row.populate(result);
      }
   }

   public void showSearchResults (UserSearchResultCollection resultCollection) {
      if (resultCollection == null || resultCollection.results == null || searchResultContainer == null) {
         return;
      }

      _resultCollection = resultCollection;
      _searchResultsTotalPages = Util.getArraySlicesCount(_resultCollection.results, searchResultsPerPage);
      _currentTab = FriendshipPanelTabs.Search;
      searchTitle.text = computeSearchTitle(resultCollection);
      repopulateSearchResults(page: 0);
      updateSearchNavigationControls();
      refreshPanel(clearInputFields: true);
   }

   private string computeSearchTitle (UserSearchResultCollection resultCollection) {
      if (resultCollection == null || resultCollection.searchInfo == null) {
         return "";
      }

      switch (resultCollection.searchInfo.filter) {
         case UserSearchInfo.FilteringMode.None:
            return "";
         case UserSearchInfo.FilteringMode.Name:
            return $"Search results for '/who is {resultCollection.searchInfo.input}'";
         case UserSearchInfo.FilteringMode.Biome:
            return $"Search results for '/who in {resultCollection.searchInfo.input}'";
         case UserSearchInfo.FilteringMode.Level:
            return $"Search results for '/who level {resultCollection.searchInfo.input}'";
         case UserSearchInfo.FilteringMode.SteamId:
            ulong.TryParse(resultCollection.searchInfo.input, out ulong steamId);
            return $"Viewing Characters of " + SteamFriendsManager.getFriendName(steamId);
      }

      return "";
   }

   private void clearSearchResults () {
      searchResultContainer.DestroyChildren();
   }

   public void onSearchTabButtonPress () {
      _currentTab = FriendshipPanelTabs.Search;
      refreshPanel();
   }

   public void updateSearchNavigationControls () {
      if (_resultCollection.searchInfo == null) {
         return;
      }

      int page = _currentSearchResultsPage + 1;
      searchPreviousPageButton.gameObject.SetActive(page > 1);
      searchNextPageButton.gameObject.SetActive(page < _searchResultsTotalPages);
      searchCurrentPageIndicatorText.text = $"Page {page} of {(_searchResultsTotalPages < 1 ? 1 : _searchResultsTotalPages)}";
   }

   public void nextSearchPage () {
      _currentSearchResultsPage = Mathf.Min(_currentSearchResultsPage + 1, _searchResultsTotalPages - 1);
      repopulateSearchResults(_currentSearchResultsPage);
      updateSearchNavigationControls();
   }

   public void previousSearchPage () {
      _currentSearchResultsPage = Mathf.Max(_currentSearchResultsPage - 1, 0);
      repopulateSearchResults(_currentSearchResultsPage);
      updateSearchNavigationControls();
   }

   #endregion

   #region Private Variables

   // The index of the current page
   private int _currentPage = 1;

   // The reference to the container that holds the set of results following a user search
   private UserSearchResultCollection _resultCollection;

   // The maximum page index (starting at 1)
   private int _maxPage = 1;

   // The number of rows per page
   private int _rowsPerPage = Friendship.MAX_FRIENDS;

   // The ID of the selected friend
   private int _selectedFriendUserId;

   // The friendship status filter
   private Friendship.Status _friendshipStatusFilter = Friendship.Status.Friends;

   // The current page
   private FriendshipPanelTabs _currentTab = FriendshipPanelTabs.None;

   // The index of the current search results page
   private int _currentSearchResultsPage = 0;

   // The number of pages computed from the search results collection
   private int _searchResultsTotalPages = 0;

   // Current steam friend rows
   private Dictionary<ulong, SteamFriendRow> _steamFriendRows = new Dictionary<ulong, SteamFriendRow>();

   #endregion
}
