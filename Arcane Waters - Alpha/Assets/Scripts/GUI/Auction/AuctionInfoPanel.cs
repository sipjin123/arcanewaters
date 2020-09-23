using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;
using System;

public class AuctionInfoPanel : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The button allowing to select the item to auction
   public Button itemCellButton;

   // The prefab we use for instantiating item cells;
   public ItemCell itemCellPrefab;

   // The item name
   public Text itemName;

   // The rarity stars
   public Image star1Image;
   public Image star2Image;
   public Image star3Image;

   // The item description
   public TMP_InputField itemDescription;

   // The different section containers
   public GameObject consultSection;
   public GameObject createSection;
   public GameObject bidSection;

   // The consult section fields
   public Text auctionSellerConsult;
   public Text auctionTimeLeftConsult;
   public Text highestBidConsult;
   public Text buyoutPriceConsult;
   public GameObject cancelAuctionContainer;

   // The create section fields
   public Toggle duration1hToggle;
   public Toggle duration3hToggle;
   public Toggle duration1dToggle;
   public Toggle duration3dToggle;
   public Toggle duration7dToggle;
   public InputField startingBidCreate;
   public InputField buyoutPriceCreate;

   // The bid section fields
   public Text auctionSellerBid;
   public Text auctionTimeLeftBid;
   public Text highestBidBid;
   public Text buyoutPriceBid;
   public InputField newBidInput;

   #endregion

   public void displayAuction (int auctionId) {
      Global.player.rpc.Cmd_GetAuction(auctionId);
   }

   public void receiveAuctionFromServer (AuctionItemData auction) {
      TimeSpan timeLeft = auction.getTimeLeftUntilExpiry();

      if (timeLeft.Ticks < 0) {
         showPanelForConsultAuction(auction, true);
      } else {
         // Check if the local user created the auction
         if (Global.player != null && Global.player.userId == auction.sellerId) {
            showPanelForCancelAuction(auction);
         } else {
            showPanelForBidOnAuction(auction);
         }
      }
   }

   public void showPanelForCreateAuction () {
      createSection.SetActive(true);
      consultSection.SetActive(false);
      bidSection.SetActive(false);
      itemCellButton.interactable = true;

      // Clear the item info
      itemCellButton.gameObject.DestroyChildren();
      _selectedItem = null;
      itemName.text = "";
      itemDescription.text = "";

      // Clear the rarity stars
      Sprite[] rarityStars = Rarity.getRarityStars(Rarity.Type.None);
      star1Image.sprite = rarityStars[0];
      star2Image.sprite = rarityStars[1];
      star3Image.sprite = rarityStars[2];

      // Clear the bid fields
      startingBidCreate.text = "";
      buyoutPriceCreate.text = "";

      // Set the default duration
      duration1hToggle.SetIsOnWithoutNotify(true);
      duration3hToggle.SetIsOnWithoutNotify(false);
      duration1dToggle.SetIsOnWithoutNotify(false);
      duration3dToggle.SetIsOnWithoutNotify(false);
      duration7dToggle.SetIsOnWithoutNotify(false);

      show();
   }

   public void showPanelForBidOnAuction (AuctionItemData auction) {
      _auction = auction;

      createSection.SetActive(false);
      consultSection.SetActive(false);
      bidSection.SetActive(true);
      itemCellButton.interactable = false;

      // Set the item info
      setItem(auction.item);

      // Set the auction info
      auctionSellerBid.text = auction.sellerName;
      highestBidBid.text = string.Format("{0:n0}", auction.highestBidPrice);
      buyoutPriceBid.text = string.Format("{0:n0}", auction.buyoutPrice);

      // Display an estimated time left - to avoid last minute bids
      auctionTimeLeftBid.text = auction.getEstimatedTimeLeftUntilExpiry();

      // Set the new bid to the current highest + 1
      newBidInput.text = (auction.highestBidPrice + 1).ToString();

      show();
   }

   public void showPanelForConsultAuction (AuctionItemData auction, bool isExpired) {
      _auction = auction;

      createSection.SetActive(false);
      consultSection.SetActive(true);
      bidSection.SetActive(false);
      itemCellButton.interactable = false;

      if (isExpired) {
         // Clear the current item
         itemCellButton.gameObject.DestroyChildren();

         // Clear the rarity stars
         Sprite[] rarityStars = Rarity.getRarityStars(Rarity.Type.None);
         star1Image.sprite = rarityStars[0];
         star2Image.sprite = rarityStars[1];
         star3Image.sprite = rarityStars[2];

         // Set the item name stored in the auction data
         itemName.text = auction.itemName;
      } else {
         // Since the item is still available, set its full info
         setItem(auction.item);
      }

      // Set the auction info
      auctionSellerConsult.text = auction.sellerName;
      highestBidConsult.text = string.Format("{0:n0}", auction.highestBidPrice);
      buyoutPriceConsult.text = string.Format("{0:n0}", auction.buyoutPrice);

      // Display an estimated time left
      auctionTimeLeftConsult.text = auction.getEstimatedTimeLeftUntilExpiry();

      // Hide the cancel button
      cancelAuctionContainer.SetActive(false);

      show();
   }

   public void showPanelForCancelAuction (AuctionItemData auction) {
      // Set the same config than the consult, but with a cancel button
      showPanelForConsultAuction(auction, false);
      cancelAuctionContainer.SetActive(true);
   }

   private void setItem (Item item) {
      // Clear the current item
      itemCellButton.gameObject.DestroyChildren();

      // Get the casted item
      _selectedItem = item.getCastItem();
      
      // Instantiate and initalize the item cell
      ItemCell cell = Instantiate(itemCellPrefab, itemCellButton.transform, false);
      cell.setCellForItem(_selectedItem);
      cell.disablePointerEvents();
      cell.hideBackground();

      // Set the item info
      itemName.text = EquipmentXMLManager.self.getItemName(_selectedItem);
      itemDescription.text = cell.getItem().getTooltip();// EquipmentXMLManager.self.getItemDescription(item);

      if (_selectedItem.category == Item.Category.Weapon ||
            _selectedItem.category == Item.Category.Armor ||
            _selectedItem.category == Item.Category.Hats) {
         Sprite[] rarityStars = Rarity.getRarityStars(item.getRarity());
         star1Image.sprite = rarityStars[0];
         star2Image.sprite = rarityStars[1];
         star3Image.sprite = rarityStars[2];
         star1Image.enabled = true;
         star2Image.enabled = true;
         star3Image.enabled = true;
      } else {
         star1Image.enabled = false;
         star2Image.enabled = false;
         star3Image.enabled = false;
      }
   }

   public void onItemCellPressed () {
      // Associate a new function with the select button
      PanelManager.self.itemSelectionScreen.selectButton.onClick.RemoveAllListeners();
      PanelManager.self.itemSelectionScreen.selectButton.onClick.AddListener(() => returnFromItemSelection());

      // Associate a new function with the cancel button
      PanelManager.self.itemSelectionScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.itemSelectionScreen.cancelButton.onClick.AddListener(() => PanelManager.self.itemSelectionScreen.hide());

      // Show the item selection screen
      if (_selectedItem != null) {
         PanelManager.self.itemSelectionScreen.show(new List<int> { _selectedItem.id });
      } else {
         PanelManager.self.itemSelectionScreen.show();
      }
   }

   public void returnFromItemSelection () {
      // Hide item selection screen
      PanelManager.self.itemSelectionScreen.hide();

      // Get the selected item
      Item selectedItem = ItemSelectionScreen.selectedItem;
      selectedItem.count = ItemSelectionScreen.selectedItemCount;

      // Set the item in the panel
      setItem(selectedItem);
   }

   public TimeSpan getSelectedAuctionDuration () {
      if (duration7dToggle.isOn) {
         return new TimeSpan(7, 0, 0, 0);
      }

      if (duration3dToggle.isOn) {
         return new TimeSpan(3, 0, 0, 0);
      }

      if (duration1dToggle.isOn) {
         return new TimeSpan(1, 0, 0, 0);
      }

      if (duration3hToggle.isOn) {
         return new TimeSpan(3, 0, 0);
      }

      return new TimeSpan(1, 0, 0);
   }

   public void onCancelButtonPressed () {
      hide();
   }

   public void onCreateButtonPressed () {
      if (_selectedItem == null) {
         PanelManager.self.noticeScreen.show("Please select an item to auction.");
         return;
      }

      if (!int.TryParse(startingBidCreate.text, out int bid) || bid <= 0) {
         PanelManager.self.noticeScreen.show("The starting bid must be higher than 0.");
         return;
      }

      if (!int.TryParse(buyoutPriceCreate.text, out int buyout) || bid <= 0) {
         PanelManager.self.noticeScreen.show("The buyout price must be higher than 0.");
         return;
      }

      PanelManager.self.showConfirmationPanel("Are you sure you want to auction your " + EquipmentXMLManager.self.getItemName(_selectedItem) + "?",
         () => {
            bid = int.Parse(startingBidCreate.text);
            buyout = int.Parse(buyoutPriceCreate.text);
            TimeSpan duration = getSelectedAuctionDuration();
            Global.player.rpc.Cmd_CreateAuction(_selectedItem, bid, buyout, (DateTime.UtcNow + duration).ToBinary());
         });
   }

   public void onBidButtonPressed () {
      int bid = int.Parse(newBidInput.text);

      if (bid > _auction.buyoutPrice) {
         PanelManager.self.noticeScreen.show("Your bid is higher than the buyout price. You will automatically buyout the item.");
         newBidInput.text = _auction.buyoutPrice.ToString();
         return;
      }

      PanelManager.self.showConfirmationPanel("Are you sure you want to bid " + string.Format("{0:n0}", newBidInput.text) + " gold?",
         () => {
            int finalBid = int.Parse(newBidInput.text);
            Global.player.rpc.Cmd_BidOnAuction(_auction.auctionId, finalBid);
         });
   }

   public void onBuyoutButtonPressed () {
      PanelManager.self.showConfirmationPanel("Are you sure you want to buyout for " + string.Format("{0:n0}", _auction.buyoutPrice) + " gold?",
         () => {
            Global.player.rpc.Cmd_BidOnAuction(_auction.auctionId, _auction.buyoutPrice);
         });
   }

   public void onCancelAuctionButtonPressed () {
      PanelManager.self.showConfirmationPanel("Are you sure you want to cancel your auction for " + _auction.itemName + "?",
         () => {
            Global.player.rpc.Cmd_CancelAuction(_auction.auctionId);
         });
   }

   public void show () {
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   #region Private Variables

   // The selected item when creating an auction
   private Item _selectedItem = null;

   // The displayed auction when consulting or bidding
   private AuctionItemData _auction = null;

   #endregion
}