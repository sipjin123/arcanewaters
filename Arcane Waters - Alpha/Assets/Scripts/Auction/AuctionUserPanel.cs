using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using NubisDataHandling;

public class AuctionUserPanel : MonoBehaviour {
   #region Public Variables

   // The loading blockers
   public GameObject[] loadBlockers;

   // The item holder
   public Transform itemParent;

   // The item prefab
   public UserItemTemplate itemPrefab;

   // Basic item info display
   public Image itemImage;
   public Text itemName;
   public Text itemDescription;
   public Text itemRarity;
   public Text itemQuantity;
   public Text itemCategory;

   // The list of created templates
   public List<UserItemTemplate> userItemTemplateList = new List<UserItemTemplate>();

   // The selected item template
   public UserItemTemplate selectedTemplate;

   // The item price
   public InputField itemPrice, buyoutPrice;

   // Popup Buttons for posting auctioned item
   public Button postAuction, exitButton;

   // Filter buttons
   public Button allFilterButton, armorFilterButton, weaponFilterButton, hatFilterButton, ingredientFilterButton;

   // The current user inventory page
   public int currentPage = 0;

   #endregion

   private void Awake () {
      postAuction.onClick.AddListener(() => {
         PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
         PanelManager.self.confirmScreen.show();
         PanelManager.self.confirmScreen.showYesNo("Auction item?");

         PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => {
            int currItemPrice = int.Parse(itemPrice.text);
            int currBuyoutPrice = int.Parse(buyoutPrice.text);

            if (currItemPrice < 0) {
               PanelManager.self.noticeScreen.show("Price must be more than 0!");
               PanelManager.self.noticeScreen.confirmButton.onClick.RemoveAllListeners();
               PanelManager.self.noticeScreen.confirmButton.onClick.AddListener(() => PanelManager.self.noticeScreen.hide());
               return;
            }

            if (currBuyoutPrice < currItemPrice) {
               PanelManager.self.noticeScreen.show("Buyout price must be more than the current price!");
               PanelManager.self.noticeScreen.confirmButton.onClick.RemoveAllListeners();
               PanelManager.self.noticeScreen.confirmButton.onClick.AddListener(() => PanelManager.self.noticeScreen.hide());
               return;
            }

            PanelManager.self.confirmScreen.hide();
            gameObject.SetActive(false);

            Global.player.rpc.Cmd_RequestPostBid(selectedTemplate.itemCache, currItemPrice, currBuyoutPrice);
         });
      });

      exitButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
      });

      allFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         NubisDataFetcher.self.requestUserItemsForAuction(currentPage, Item.Category.None);
      });
      armorFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         NubisDataFetcher.self.requestUserItemsForAuction(currentPage, Item.Category.Armor);
      });
      weaponFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         NubisDataFetcher.self.requestUserItemsForAuction(currentPage, Item.Category.Weapon);
      });
      ingredientFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         NubisDataFetcher.self.requestUserItemsForAuction(currentPage, Item.Category.CraftingIngredients);
      });
      hatFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         NubisDataFetcher.self.requestUserItemsForAuction(currentPage, Item.Category.Hats);
      });
   }

   public void loadUserItemList (List<Item> userItemList) {
      itemImage.sprite = ImageManager.self.blankSprite;
      itemName.text = "";
      itemDescription.text = "";
      itemRarity.text = "";
      itemQuantity.text = "";
      itemCategory.text = "";

      setBlockers(false);
      itemParent.gameObject.DestroyChildren();
      userItemTemplateList = new List<UserItemTemplate>();
      foreach (Item item in userItemList) {
         UserItemTemplate itemInventoryTemplate = Instantiate(itemPrefab, itemParent).GetComponent<UserItemTemplate>();
         itemInventoryTemplate.setCellForItem(item);
         itemInventoryTemplate.setPanel(this);

         userItemTemplateList.Add(itemInventoryTemplate);
      }
   }

   public void selectItem (UserItemTemplate itemTemplate) {
      selectedTemplate = itemTemplate;

      foreach (UserItemTemplate template in userItemTemplateList) {
         template.setObjHighlight(false);
      }
      selectedTemplate.setObjHighlight(true);

      itemImage.sprite = ImageManager.getSprite(EquipmentXMLManager.self.getItemIconPath(selectedTemplate.itemCache));
      itemName.text = EquipmentXMLManager.self.getItemName(selectedTemplate.itemCache);
      itemDescription.text = EquipmentXMLManager.self.getItemDescription(selectedTemplate.itemCache);
      itemRarity.text = "Undefined";
      itemQuantity.text = "Undefined";
      itemCategory.text = selectedTemplate.itemCache.category.ToString();
   }

   public void setBlockers (bool isOn) {
      foreach (GameObject obj in loadBlockers) {
         obj.SetActive(isOn);
      }
   }

   #region Private Variables

   #endregion
}
