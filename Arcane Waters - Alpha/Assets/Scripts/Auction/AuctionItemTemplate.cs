using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AuctionItemTemplate : MonoBehaviour {
   #region Public Variables

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

   // Cancel auction button
   public Button cancelAuctionButton;

   // Date info
   public Text datePosted;
   public Text dateExpiry;

   // Select Template Button
   public Button selectTemplateButton;

   // Item that highlights this template
   public GameObject highlightItem;

   // Optional tabs depending if the fetched data is the user's or other users
   public GameObject ownerNameTab, cancelTab;

   // The number of item being auctioned
   public Text itemCount;

   #endregion

   public void toggleHighlight (bool isOn) {
      highlightItem.SetActive(isOn);
   }

   public void setTemplate (AuctionItemData data, bool hasDeleteButton = false) {
      cancelAuctionButton.gameObject.SetActive(hasDeleteButton);
      cancelTab.SetActive(hasDeleteButton);
      ownerNameTab.SetActive(!hasDeleteButton);
      selectTemplateButton.gameObject.SetActive(!hasDeleteButton);

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

   #region Private Variables
      
   #endregion
}
