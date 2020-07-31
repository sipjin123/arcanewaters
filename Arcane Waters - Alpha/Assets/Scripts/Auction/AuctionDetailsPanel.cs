using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AuctionDetailsPanel : MonoBehaviour {
   #region Public Variables

   // The market panel
   public AuctionMarketPanel marketPanel;

   // Cached item data
   public AuctionItemData auctionItemData;

   // Item data display
   public Text itemPrice;
   public Text auctionId;
   public Text itemName;
   public Image itemIcon;
   public Text sellerName;
   public Text highestBid;
   public Text buyoutPrice;
   public InputField userBid;
   public Text itemCount;

   // Date info
   public Text datePosted;
   public Text dateExpiry;

   // Transaction Button
   public Button bidButton, buyoutButton;

   #endregion

   private void Awake () {
      bidButton.onClick.AddListener(() => {
         if (auctionItemData != null) {
            int userBidPrice = int.Parse(userBid.text);
            if (userBidPrice < auctionItemData.itemPrice) {
               PanelManager.self.noticeScreen.confirmButton.onClick.RemoveAllListeners();
               PanelManager.self.noticeScreen.show("Price must be higher than the initial bid price");
               PanelManager.self.noticeScreen.confirmButton.onClick.AddListener(() => PanelManager.self.noticeScreen.hide());
               return;
            }

            if (userBidPrice < auctionItemData.highestBidPrice) {
               PanelManager.self.noticeScreen.confirmButton.onClick.RemoveAllListeners();
               PanelManager.self.noticeScreen.show("Price must be higher than the current highest bid price");
               PanelManager.self.noticeScreen.confirmButton.onClick.AddListener(() => PanelManager.self.noticeScreen.hide());
               return;
            }

            Global.player.rpc.Cmd_RequestItemBid(auctionItemData.auctionId, userBidPrice);
         } else {
            PanelManager.self.noticeScreen.confirmButton.onClick.RemoveAllListeners();
            PanelManager.self.noticeScreen.show("Please select an item to bid on");
            PanelManager.self.noticeScreen.confirmButton.onClick.AddListener(() => PanelManager.self.noticeScreen.hide());
         }
      });

      buyoutButton.onClick.AddListener(() => {
         if (auctionItemData != null) {
            PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
            PanelManager.self.confirmScreen.show();
            PanelManager.self.confirmScreen.showYesNo("Buy out item for: " + auctionItemData.itembuyOutPrice + "?");

            PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => {
               Global.player.rpc.Cmd_RequestItemBid(auctionItemData.auctionId, auctionItemData.itembuyOutPrice);
               PanelManager.self.confirmScreen.hide();
            });
         } else {
            PanelManager.self.noticeScreen.confirmButton.onClick.RemoveAllListeners();
            PanelManager.self.noticeScreen.show("Please select an item to buyout");
            PanelManager.self.noticeScreen.confirmButton.onClick.AddListener(() => PanelManager.self.noticeScreen.hide());
         }
      });
   }

   public void loadItemData (AuctionItemData data) {
      auctionItemData = data;

      Item newItem = new Item {
         category = (Item.Category) data.itemCategory,
         itemTypeId = data.itemTypeId,
         id = data.itemId
      };
      itemName.text = EquipmentXMLManager.self.getItemName(newItem);
      itemIcon.sprite = ImageManager.getSprite(EquipmentXMLManager.self.getItemIconPath(newItem));
      itemPrice.text = data.itemPrice.ToString();
      datePosted.text = data.auctionDateCreated;
      sellerName.text = data.sellerName;
      highestBid.text = data.highestBidPrice.ToString();
      buyoutPrice.text = data.itembuyOutPrice.ToString();
      itemCount.text = data.itemCount.ToString();
   }

   public void clearContent () {
      itemName.text = "";
      itemIcon.sprite = ImageManager.self.blankSprite;
      itemPrice.text = "";
      datePosted.text = "";
      sellerName.text = "";
      highestBid.text = "";
      buyoutPrice.text = "";
      itemCount.text = "";
   }

   #region Private Variables
      
   #endregion
}
