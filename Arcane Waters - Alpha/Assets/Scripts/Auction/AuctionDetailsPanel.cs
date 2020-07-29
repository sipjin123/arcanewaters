using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AuctionDetailsPanel : MonoBehaviour {
   #region Public Variables

   // Cached item data
   public AuctionItemData auctionItemData;

   // Item data display
   public Text itemPrice;
   public Text auctionId;
   public Text itemName;
   public Image itemIcon;
   public Text sellerName;

   // Date info
   public Text datePosted;
   public Text dateExpiry;

   #endregion
   
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
   }

   public void clearContent () {
      itemName.text = "";
      itemIcon.sprite = ImageManager.self.blankSprite;
      itemPrice.text = "";
      datePosted.text = "";
      sellerName.text = "";
   }

   #region Private Variables
      
   #endregion
}
