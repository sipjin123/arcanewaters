using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using NubisDataHandling;

public class AuctionUserPanel : MonoBehaviour {
   #region Public Variables

   // Reference to the root panel
   public AuctionRootPanel rootPanel;

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
   public InputField itemPrice, buyoutPrice, itemQuantityInput;

   // Popup Buttons for posting auctioned item
   public Button postAuction, exitButton;

   // Filter buttons
   public Button allFilterButton, armorFilterButton, weaponFilterButton, hatFilterButton, ingredientFilterButton;

   // The current user inventory page
   public int currentPage = 0;

   // The total items the user has
   public int totalUserItemCount = 0;

   // The page navigation buttons
   public Button previousButton, nextButton;

   // The text displaying the page
   public Text pageStatusText;

   // Max inventory count per page
   public static int MAX_PAGE_COUNT = 30;

   // The cached category filter
   public Item.Category currentCategoryFilter;

   #endregion

   private void Awake () {
      nextButton.onClick.AddListener(() => goToNextPage());
      previousButton.onClick.AddListener(() => goToPreviousPage());

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

            if (selectedTemplate.itemCache.category == Item.Category.CraftingIngredients) {
               selectedTemplate.itemCache.count = int.Parse(itemQuantityInput.text);
            } else {
               selectedTemplate.itemCache.count = 1;
            }
            Global.player.rpc.Cmd_RequestPostBid(selectedTemplate.itemCache, currItemPrice, currBuyoutPrice);
         });
      });

      exitButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         NubisDataFetcher.self.checkAuctionMarket(rootPanel.marketPanel.currentPage, rootPanel.marketPanel.currentItemCategory, false);
      });

      allFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         currentCategoryFilter = Item.Category.None;
         NubisDataFetcher.self.requestUserItemsForAuction(currentPage, currentCategoryFilter);
      });
      armorFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         currentCategoryFilter = Item.Category.Armor;
         NubisDataFetcher.self.requestUserItemsForAuction(currentPage, currentCategoryFilter);
      });
      weaponFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         currentCategoryFilter = Item.Category.Weapon;
         NubisDataFetcher.self.requestUserItemsForAuction(currentPage, currentCategoryFilter);
      });
      ingredientFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         currentCategoryFilter = Item.Category.CraftingIngredients;
         NubisDataFetcher.self.requestUserItemsForAuction(currentPage, currentCategoryFilter);
      });
      hatFilterButton.onClick.AddListener(() => {
         setBlockers(true);
         currentCategoryFilter = Item.Category.Hats;
         NubisDataFetcher.self.requestUserItemsForAuction(currentPage, currentCategoryFilter);
      });
   }

   public void loadUserItemList (List<Item> userItemList, int totalItemCount) {
      pageStatusText.text = (currentPage + 1) + "/" + ((int) (totalItemCount / MAX_PAGE_COUNT) + 1);
      totalUserItemCount = totalItemCount;
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

   private void goToNextPage () {
      if (totalUserItemCount > MAX_PAGE_COUNT) {
         currentPage++;
         NubisDataFetcher.self.requestUserItemsForAuction(currentPage, currentCategoryFilter);
         pageStatusText.text = (currentPage + 1) + "/" + ((int) (totalUserItemCount / MAX_PAGE_COUNT) + 1);
      }
   }

   private void goToPreviousPage () {
      if (currentPage > 0) {
         currentPage--;
         NubisDataFetcher.self.requestUserItemsForAuction(currentPage, currentCategoryFilter);
         pageStatusText.text = (currentPage + 1) + "/" + ((int) (totalUserItemCount / MAX_PAGE_COUNT) + 1);
      }
   }

   public void selectItem (UserItemTemplate itemTemplate) {
      itemQuantityInput.gameObject.SetActive(itemTemplate.itemCache.category == Item.Category.CraftingIngredients);
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
