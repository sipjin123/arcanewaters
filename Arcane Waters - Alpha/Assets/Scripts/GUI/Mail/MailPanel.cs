using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using TMPro;
using UnityEngine.InputSystem;

public class MailPanel : Panel
{

   #region Public Variables

   // The number of mail rows to display per page
   public static int ROWS_PER_PAGE = 7;

   // The mode of the panel
   public enum Mode { None = 0, NoMailSelected = 1, ReadMail = 2, WriteMail = 3 }

   // The container for the mail rows
   public GameObject mailRowsContainer;

   // The container for the attached items when reading a mail
   public GameObject readAttachedItemContainer;

   // The container for the attached items when writing a mail
   public GameObject writeAttachedItemContainer;

   // The prefab we use for creating mail rows
   public MailRow mailRowPrefab;

   // The prefab we use for creating item cells
   public ItemCell itemCellPrefab;

   // The section displayed when reading a mail
   public GameObject readMailSection;

   // The section displayed when composing a mail
   public GameObject writeMailSection;

   // The section displayed when no mail is selected
   public GameObject noMailSelectedSection;

   // The mail reception date
   public Text receptionDateText;

   // The sender name
   public Text senderNameText;

   // The mail subject
   public Text mailSubjectText;

   // The mail message
   public TMP_InputField messageText;

   // The recipient's name input field
   public InputField recipientInput;

   // The subject input field
   public InputField subjectInput;

   // The message input field
   public TMP_InputField messageInput;

   // The word count of the message
   public Text messageWordCountText;

   // The button used to attach an item to a mail
   public Button addAttachedItemButton;

   // The button used to retrieve all the attached items from a mail
   public Button retrieveAttachedItemsButton;

   // The button used to delete the displayed mail
   public Button deleteMailButton;

   // The button used to reply to the displayed mail
   public Button replyMailButton;

   // The page number text
   public Text pageNumberText;

   // The next page button
   public Button nextPageButton;

   // The previous page button
   public Button previousPageButton;

   // The send (persistent) button
   public Button sendPersistentButton;

   // Self
   public static MailPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
      messageInput.characterLimit = MailManager.MAX_MESSAGE_LENGTH;
      updateMessageWordCount();
      displaySendPersistentButton();
   }

   public override void Update () {
      // UI Navigation
      if (Keyboard.current.tabKey.wasPressedThisFrame && isShowing()) {

         // Move focus down to subject text field. Trimming is required because tab is also added to input fields
         if (recipientInput.isFocused) {
            subjectInput.Select();
            subjectInput.text = subjectInput.text.Trim();
         }

         // Move focus down to message text field
         if (subjectInput.isFocused) {
            if (Keyboard.current.shiftKey.isPressed) {
               recipientInput.Select();
               recipientInput.text = recipientInput.text.Trim();
            } else {
               messageInput.Select();
               messageInput.text = messageInput.text.Trim();
            }
         }

         // Move focus up to subject text field
         if (messageInput.isFocused && Keyboard.current.shiftKey.isPressed) {
            subjectInput.Select();
            subjectInput.text = subjectInput.text.Trim();
         }
      }
   }

   private void displaySendPersistentButton () {
      // Show the send (persistent) button only if we are admins
      if (Global.player.isAdmin()) {
         if (sendPersistentButton != null) {
            sendPersistentButton.gameObject.SetActive(true);
         }
      }
   }

   public void refreshMailList () {
      Global.player.rpc.Cmd_RequestMailListFromServer(_currentPage, ROWS_PER_PAGE);
      Global.player.rpc.Cmd_ProcessOldMails(MailManager.MAX_MAIL_LIFETIME_DAYS);
   }

   public void displayMail (int mailId) {
      Global.player.rpc.Cmd_RequestSingleMailFromServer(mailId);
   }

   public void clearSelectedMail () {
      _displayedMailId = -1;
      configurePanelForMode(Mode.NoMailSelected);
   }

   public void confirmMailSent () {
      clearWriteMailSection();
      composeNewMail();
   }

   public void composeMailTo (string recipientName) {
      clearWriteMailSection();
      composeNewMail();
      recipientInput.text = recipientName;
      subjectInput.Select();
   }

   public void composeReplyTo (string recipientName, string originalSubject) {
      composeMailTo(recipientName);
      subjectInput.text = "RE: " + originalSubject;
   }

   public void updatePanelWithMailList (List<MailInfo> mailList, int pageNumber, int totalMailCount, List<bool> systemMailStatusList) {
      // Update the current page number
      _currentPage = pageNumber;

      // Calculate the maximum page number
      _maxPage = Mathf.CeilToInt((float) totalMailCount / ROWS_PER_PAGE);
      if (_maxPage == 0) {
         _maxPage = 1;
      }

      // Update the current page text
      pageNumberText.text = "Page " + _currentPage.ToString() + " of " + _maxPage.ToString();

      // Update the navigation buttons
      updateNavigationButtons();

      // Clear out any old info
      mailRowsContainer.DestroyChildren();

      // Instantiate the rows
      int mailIndex = 0;
      foreach (MailInfo mail in mailList) {
         MailRow row = Instantiate(mailRowPrefab, mailRowsContainer.transform, false);
         row.setRowForMail(mail, mail.mailId == _displayedMailId);

         if (systemMailStatusList[mailIndex]) {
            row.senderName.text = MailManager.SYSTEM_USERNAME;
         }

         mailIndex++;
      }

      // Set the panel mode if it has not been initialized yet
      if (_currentMode == Mode.None) {
         configurePanelForMode(Mode.NoMailSelected);
      }

   }

   public void updateWithSentMailList (List<MailInfo> mailList, int pageNumber, int totalMailCount) {

   }

   public void updatePanelWithSingleMail (MailInfo mail, List<Item> attachedItems, bool hasUnreadMail, bool isSystemMail) {
      _displayedMailId = mail.mailId;

      // Configure the panel
      configurePanelForMode(Mode.ReadMail);

      // Set the text fields
      receptionDateText.text = DateTime.FromBinary(mail.receptionDate).ToLocalTime().ToString("dddd, dd MMMM yyyy hh:mm tt");
      senderNameText.text = mail.senderUserName;
      mailSubjectText.text = mail.mailSubject;
      messageText.text = mail.message;

      // Configure the delete button
      int mailIdForDeleteButton = mail.mailId;
      deleteMailButton.onClick.RemoveAllListeners();
      deleteMailButton.onClick.AddListener(() => deleteMail(mailIdForDeleteButton));

      // Configure the reply button
      replyMailButton.onClick.RemoveAllListeners();
      replyMailButton.onClick.AddListener(() => composeReplyTo(mail.senderUserName, mail.mailSubject));

      // Configure the retrieve all items button
      retrieveAttachedItemsButton.onClick.RemoveAllListeners();
      if (attachedItems.Count <= 0) {
         retrieveAttachedItemsButton.interactable = false;
      } else {
         retrieveAttachedItemsButton.interactable = true;
      }

      // Clear existing attached items
      readAttachedItemContainer.DestroyChildren();

      // Add the attached items
      foreach (Item item in attachedItems) {
         // Get the casted item
         Item castedItem = item.getCastItem();

         // Instantiate and initalize the item cell
         ItemCell cell = Instantiate(itemCellPrefab, readAttachedItemContainer.transform, false);
         cell.setCellForItem(castedItem);

         // Make sure the correct value is captured for the click event
         int mailIdForCell = mail.mailId;

         // Set the cell click events
         cell.leftClickEvent.RemoveAllListeners();
         cell.rightClickEvent.RemoveAllListeners();
         cell.doubleClickEvent.RemoveAllListeners();
         cell.leftClickEvent.AddListener(() => pickUpAttachedItem(mailIdForCell, cell.getItem()));
         cell.rightClickEvent.AddListener(() => pickUpAttachedItem(mailIdForCell, cell.getItem()));
         retrieveAttachedItemsButton.onClick.AddListener(() => pickUpAttachedItem(mailIdForCell, cell.getItem()));
      }

      // Refresh the mail list
      refreshMailList();

      // Adjust UI for system mails
      replyMailButton.interactable = !isSystemMail;
      senderNameText.text = isSystemMail ? MailManager.SYSTEM_USERNAME : senderNameText.text;
   }

   public void composeNewMail () {
      // Configure the panel
      configurePanelForMode(Mode.WriteMail);

      // Clear the displayed mail id
      _displayedMailId = -1;

      // Refresh the mail list
      refreshMailList();
   }

   public void onAddAttachedItemButtonPress () {
      // Check if the maximum number of attached items has been reached
      if (_attachedItems.Count >= MailManager.MAX_ATTACHED_ITEMS) {
         return;
      }

      // Creates a list of attached item ids, to filter them in the item selection
      List<int> itemIdsToFilter = new List<int>();
      foreach (Item item in _attachedItems) {
         itemIdsToFilter.Add(item.id);
      }

      // Associate a new function with the select button
      PanelManager.self.itemSelectionScreen.selectButton.onClick.RemoveAllListeners();
      PanelManager.self.itemSelectionScreen.selectButton.onClick.AddListener(() => returnFromItemSelection());

      // Associate a new function with the cancel button
      PanelManager.self.itemSelectionScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.itemSelectionScreen.cancelButton.onClick.AddListener(() => PanelManager.self.itemSelectionScreen.hide());

      // Show the item selection screen
      PanelManager.self.itemSelectionScreen.show(itemIdsToFilter);
   }

   public void returnFromItemSelection () {
      // Hide item selection screen
      PanelManager.self.itemSelectionScreen.hide();

      // Get the selected item
      Item selectedItem = ItemSelectionScreen.selectedItem;

      // Set the number of items
      selectedItem.count = ItemSelectionScreen.selectedItemCount;

      // Verify that the item is not already attached
      foreach (Item item in _attachedItems) {
         if (item.id == selectedItem.id) {
            PanelManager.self.noticeScreen.show("This item is already attached to the mail!");
            return;
         }
      }

      // Add the item to the work list
      _attachedItems.AddLast(selectedItem);

      // Re-attach all the items
      refreshAttachedItems();
   }

   public void pickUpAttachedItem (int mailId, Item item) {
      Global.player.rpc.Cmd_PickUpAttachedItemFromMail(mailId, item.id);
   }

   public void removeAttachedItem (Item item) {
      // Remove the item from the work list
      _attachedItems.Remove(item);

      // Re-attach all the items
      refreshAttachedItems();
   }

   public void deleteMail (int mailId) {
      // Don't require delete confirmation if all items are collected
      if (!retrieveAttachedItemsButton.interactable) {
         confirmDeleteMail(mailId);
         return;
      }
   
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmDeleteMail(mailId));

      // Show a confirmation panel
      if (readAttachedItemContainer.transform.childCount > 0) {
         PanelManager.self.confirmScreen.show("Warning! The attached items will be lost. Are you sure you want to delete this mail?");
      } else {
         PanelManager.self.confirmScreen.show("Are you sure you want to delete this mail?");
      }
   }

   public void confirmDeleteMail (int mailId) {
      Global.player.rpc.Cmd_DeleteMail(mailId);
   }

   public bool checkComposedMail () {

      // The entered name is null
      if ("".Equals(recipientInput.text)) {
         PanelManager.self.noticeScreen.show("Enter a recipient name!");
         return false;
      }

      // The entered subject is null
      if ("".Equals(subjectInput.text)) {
         PanelManager.self.noticeScreen.show("Enter a subject!");
         return false;
      }

      // Remove whitespace
      recipientInput.text = recipientInput.text.Trim();
      subjectInput.text = subjectInput.text.Trim();

      return true;
   }

   public void sendMail () {
      if (!checkComposedMail()) {
         return;
      }

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmSendMail());

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Do you want to send this mail to " + recipientInput.text + "?", MailManager.getMailSendingCost(), "This mail will auto delete itself in " + MailManager.MAX_MAIL_LIFETIME_DAYS + " days.");
   }

   public void sendMailPersistent () {
      if (!checkComposedMail()) {
         return;
      }

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmSendPersistentMail());

      // Show a warning that tells the user, that the mail will persist and never be auto deleted
      // This mail is only sent by admins, so there is no need to pay for it
      PanelManager.self.confirmScreen.show("Do you want to send this mail to " + recipientInput.text + "?", 0, "This mail will * not * be auto deleted.");

   }

   private void prepareMail (bool autoDelete = true) {

      // Build the attached items ids and count lists
      List<int> attachedItemIds = new List<int>();
      List<int> attachedItemCounts = new List<int>();
      foreach (Item item in _attachedItems) {
         attachedItemIds.Add(item.id);
         attachedItemCounts.Add(item.count);
      }

      // Hide the confirm panel
      PanelManager.self.confirmScreen.hide();

      // Mails sent from admin account that contain attachment, must persist.
      if (Global.player.isAdmin() && attachedItemIds.Any()) {
         autoDelete = false;
      }

      // Clean the recipient
      string recipientName = recipientInput.text.Trim();

      // Create the mail
      Global.player.rpc.Cmd_CreateMail(recipientName, subjectInput.text, messageInput.text,
      attachedItemIds.ToArray(), attachedItemCounts.ToArray(), MailManager.getMailSendingCost(), autoDelete);
   }

   public void confirmSendMail () {
      prepareMail();
   }

   public void confirmSendPersistentMail () {
      prepareMail(false);
   }

   public void updateMessageWordCount () {
      int wordCount = messageInput.text.Length;
      messageWordCountText.text = wordCount.ToString() + "/" + MailManager.MAX_MESSAGE_LENGTH.ToString();
   }

   public bool isWritingMail () {
      return isShowing() && (recipientInput.isFocused || subjectInput.isFocused || messageInput.isFocused);
   }

   private void configurePanelForMode (Mode mode) {
      switch (mode) {
         case Mode.NoMailSelected:
            noMailSelectedSection.SetActive(true);
            readMailSection.SetActive(false);
            writeMailSection.SetActive(false);
            break;
         case Mode.ReadMail:
            noMailSelectedSection.SetActive(false);
            readMailSection.SetActive(true);
            writeMailSection.SetActive(false);
            break;
         case Mode.WriteMail:
            noMailSelectedSection.SetActive(false);
            readMailSection.SetActive(false);
            writeMailSection.SetActive(true);
            break;
         default:
            break;
      }
      _currentMode = mode;
   }

   public void clearWriteMailSection () {
      // Clear existing values for the write mail section
      recipientInput.text = "";
      subjectInput.text = "";
      messageInput.text = "";

      // Update the word count
      updateMessageWordCount();

      // Clear existing attached items in the write mail section
      writeAttachedItemContainer.DestroyChildren();
      _attachedItems.Clear();
      updateAddItemButton();
   }

   private void refreshAttachedItems () {
      // Clear out the attached items
      writeAttachedItemContainer.DestroyChildren();

      // Re-attach the items
      foreach (Item item in _attachedItems) {
         ItemCell cell = Instantiate(itemCellPrefab, writeAttachedItemContainer.transform, false);
         cell.setCellForItem(item);

         // Set the cell click events
         cell.leftClickEvent.RemoveAllListeners();
         cell.rightClickEvent.RemoveAllListeners();
         cell.doubleClickEvent.RemoveAllListeners();
         cell.leftClickEvent.AddListener(() => removeAttachedItem(item));
         cell.rightClickEvent.AddListener(() => removeAttachedItem(item));
      }

      // Disable the add button if the maximum number of attached items is reached
      updateAddItemButton();
   }

   private void updateAddItemButton () {
      // Disable the add button if the maximum number of attached items is reached
      if (_attachedItems.Count >= MailManager.MAX_ATTACHED_ITEMS) {
         addAttachedItemButton.interactable = false;
      } else {
         addAttachedItemButton.interactable = true;
      }
   }

   public void nextPage () {
      if (_currentPage < _maxPage) {
         _currentPage++;
         refreshMailList();
      }
   }

   public void previousPage () {
      if (_currentPage > 1) {
         _currentPage--;
         refreshMailList();
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

   // The id of the currently displayed mail
   private int _displayedMailId = -1;

   // The current panel mode
   private Mode _currentMode = Mode.None;

   // The list of attached items when writing a mail
   private LinkedList<Item> _attachedItems = new LinkedList<Item>();

   #endregion
}