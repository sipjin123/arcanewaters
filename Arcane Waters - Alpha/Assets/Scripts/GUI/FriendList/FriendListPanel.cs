using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class FriendListPanel : Panel
{

   #region Public Variables

   // The number of rows to display per page
   public static int ROWS_PER_PAGE = 9;

   // The container of the friend rows
   public GameObject friendRowsContainer;

   // The prefab we use for creating friend rows
   public FriendListRow friendRowPrefab;

   // The prefab we use for creating received friend request rows
   public FriendshipRequestReceivedRow friendshipRequestReceivedRowPrefab;

   // The prefab we use for creating sent friend request rows
   public FriendshipRequestSentRow friendshipRequestSentRowPrefab;

   // The input field holding the name of the user to send a friendship invitation
   public InputField inviteInputField;

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

   // The tab buttons
   public Button friendListTabButton;
   public Button requestReceivedTabButton;
   public Button requestSentTabButton;

   // Self
   public static FriendListPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void refreshPanel () {
      Global.player.rpc.Cmd_RequestFriendshipInfoFromServer(_currentPage, ROWS_PER_PAGE, _friendshipStatusFilter);
   }

   public void updatePanelWithFriendshipInfo (List<FriendshipInfo> friendshipInfoList, Friendship.Status friendshipStatus,
      int pageNumber, int totalFriendInfoCount, int pendingRequestCount) {
      _friendshipStatusFilter = friendshipStatus;

      // Update the current page number
      _currentPage = pageNumber;

      // Calculate the maximum page number
      _maxPage = Mathf.CeilToInt((float) totalFriendInfoCount / ROWS_PER_PAGE);
      if (_maxPage == 0) {
         _maxPage = 1;
      }

      // Update the current page text
      pageNumberText.text = "Page " + _currentPage.ToString() + " of " + _maxPage.ToString();

      // Update the pending tab text
      requestReceivedTabText.text = "Pending (" + pendingRequestCount.ToString() + ")";
      requestReceivedTabUnderText.text = requestReceivedTabText.text;

      // Update the navigation buttons
      updateNavigationButtons();

      // Clear out any current items
      friendRowsContainer.DestroyChildren();

      // Create the friend rows
      foreach (FriendshipInfo friend in friendshipInfoList) {
         // Use different prefabs and parameters for each tab
         switch (friendshipStatus) {
            case Friendship.Status.InviteSent:
               // Instantiate and initialize the row
               FriendshipRequestSentRow rowSent = Instantiate(friendshipRequestSentRowPrefab, friendRowsContainer.transform, false);
               rowSent.setRowForFriendshipInfo(friend);
               break;
            case Friendship.Status.InviteReceived:
               // Instantiate and initialize the row
               FriendshipRequestReceivedRow rowReceived = Instantiate(friendshipRequestReceivedRowPrefab, friendRowsContainer.transform, false);
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

      // Select the correct tab
      switch (friendshipStatus) {
         case Friendship.Status.InviteSent:
            friendListTabCanvasGroup.alpha = 0f;
            requestReceivedTabCanvasGroup.alpha = 0f;
            requestSentTabCanvasGroup.alpha = 1f;
            friendListTabButton.interactable = true;
            requestReceivedTabButton.interactable = true;
            requestSentTabButton.interactable = false;
            break;
         case Friendship.Status.InviteReceived:
            friendListTabCanvasGroup.alpha = 0f;
            requestReceivedTabCanvasGroup.alpha = 1f;
            requestSentTabCanvasGroup.alpha = 0f;
            friendListTabButton.interactable = true;
            requestReceivedTabButton.interactable = false;
            requestSentTabButton.interactable = true;
            break;
         case Friendship.Status.Friends:
            friendListTabCanvasGroup.alpha = 1f;
            requestReceivedTabCanvasGroup.alpha = 0f;
            requestSentTabCanvasGroup.alpha = 0f;
            friendListTabButton.interactable = false;
            requestReceivedTabButton.interactable = true;
            requestSentTabButton.interactable = true;
            break;
         default:
            break;
      }
   }

   public void receiveUserIdForFriendshipInvite (int friendUserId, string friendName) {
      _selectedFriendUserId = friendUserId;

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmSendFriendshipInvite());

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to ask " + friendName + " to be your friend?");
   }

   public void onFriendListTabButtonPress () {
      _friendshipStatusFilter = Friendship.Status.Friends;
      _currentPage = 1;
      refreshPanel();
   }

   public void onInvitesReceivedTabButtonPress () {
      _friendshipStatusFilter = Friendship.Status.InviteReceived;
      _currentPage = 1;
      refreshPanel();
   }

   public void onInvitesSentTabButtonPress () {
      _friendshipStatusFilter = Friendship.Status.InviteSent;
      _currentPage = 1;
      refreshPanel();
   }

   public void onSendInviteButtonPress () {
      if ("".Equals(inviteInputField.text)) {
         PanelManager.self.noticeScreen.show("Please enter a player name.");
      } else {
         Global.player.rpc.Cmd_RequestUserIdForFriendshipInvite(inviteInputField.text);
      }
   }

   public void confirmSendFriendshipInvite () {
      Global.player.rpc.Cmd_SendFriendshipInvite(_selectedFriendUserId);
   }

   public void onChatButtonPress (int friendUserId, string friendName) {

   }

   public void onSendMessageButtonPress (int friendUserId, string friendName) {

   }

   public void onAcceptFriendshipRequestButtonPress (int friendUserId) {
      Global.player.rpc.Cmd_AcceptFriendshipInvite(friendUserId);
   }

   public void onDeleteFriendButtonPress (int friendUserId, string friendName) {
      _selectedFriendUserId = friendUserId;

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmDeleteFriend());

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to delete your friend " + friendName + "?");
   }

   public void onRejectFriendshipRequestButtonPress (int friendUserId, string friendName) {
      _selectedFriendUserId = friendUserId;

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmDeleteFriend());

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to reject the friendship invitation from " + friendName + "?");
   }

   public void onCancelFriendshipRequestButtonPress (int friendUserId, string friendName) {
      _selectedFriendUserId = friendUserId;

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmDeleteFriend());

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to cancel the friendship invitation to " + friendName + "?");
   }

   public void confirmDeleteFriend () {
      Global.player.rpc.Cmd_DeleteFriendship(_selectedFriendUserId);
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

   // The ID of the selected friend
   private int _selectedFriendUserId;

   // The friendship status filter
   private Friendship.Status _friendshipStatusFilter = Friendship.Status.Friends;

   #endregion
}
