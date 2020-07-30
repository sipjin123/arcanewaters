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

   // Date info
   public Text datePosted;
   public Text dateExpiry;

   // Select Template Button
   public Button selectTemplateButton;

   // Item that highlights this template
   public GameObject highlightItem;

   #endregion

   public void toggleHighlight (bool isOn) {
      highlightItem.SetActive(isOn);
   }

   public void setTemplate (AuctionItemData data) {
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
   }

   #region Private Variables
      
   #endregion
}
