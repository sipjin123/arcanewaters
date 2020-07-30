using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using NubisDataHandling;

public class AuctionMarketPanel : MonoBehaviour {
   #region Public Variables

   // The root panel
   public AuctionRootPanel rootPanel;

   // The prefab holder
   public Transform auctionItemHolder, userAuctionedItemHolder;

   // The item prefab
   public AuctionItemTemplate auctionItemPrefab;

   // The detail panel
   public AuctionDetailsPanel auctionDetailPanel;

   // The selected template
   public AuctionItemTemplate selectiedItemTemplate;

   // The item list generated
   public List<AuctionItemTemplate> auctionItemTemplateList;

   // Filter buttons
   public Button allFilterButton, armorFilterButton, weaponFilterButton, hatFilterButton, ingredientFilterButton;

   // Button that opens the user auction panel
   public Button auctionItemButton;

   // Closes the main panel
   public Button closeMainPanelButton;

   // Button that leads the user to the current auctioned items
   public Button checkAuctionedItemsButton;

   // Panel containing the user auctioned Items
   public GameObject auctionedItemPanel;

   // Max auction market item count
   public static int MAX_PAGE_COUNT = 10;

   // The page of the panel
   public int currentPage = 0;

   // Loading blockers while user items are being fetched
   public GameObject[] userAuctionedItemBlockers;

   // Loading blockers while user auction history are being fetched
   public GameObject[] userAuctionHistoryBlockers;

   // Closes the user auction panel
   public Button closeAuctionedItemsButton;

   // Triggers the auction history panel
   public Button checkAuctionHistoryButton;

   // Navigates the panel back to the auctioned items panel
   public Button checkUserAuctionItemButton;

   // The panel containing the user auction history
   public GameObject auctionHistoryPanel;

   // The prefab holders for the auction history templates
   public Transform auctionHistoryItemHolder;

   #endregion

   private void Awake () {
      checkAuctionHistoryButton.onClick.AddListener(() => {
         foreach (GameObject obj in userAuctionHistoryBlockers) {
            obj.SetActive(true);
         }
         auctionHistoryPanel.SetActive(true);
      });
      checkUserAuctionItemButton.onClick.AddListener(() => {
         auctionHistoryPanel.SetActive(false);
      });

      closeAuctionedItemsButton.onClick.AddListener(() => {
         auctionedItemPanel.SetActive(false);
      });

      closeMainPanelButton.onClick.AddListener(() => {
         rootPanel.hide();
      });

      checkAuctionedItemsButton.onClick.AddListener(() => {
         auctionedItemPanel.SetActive(true);
         toggleAuctionedUserItemLoader(true);
         NubisDataFetcher.self.checkAuctionMarket(currentPage, new Item.Category[4] { Item.Category.CraftingIngredients, Item.Category.Armor, Item.Category.Weapon, Item.Category.Hats }, true);
      });

      auctionItemButton.onClick.AddListener(() => {
         rootPanel.userPanel.gameObject.SetActive(true);
         rootPanel.userPanel.setBlockers(true);
         NubisDataFetcher.self.requestUserItemsForAuction(0, Item.Category.None);
      });

      allFilterButton.onClick.AddListener(() => {
         rootPanel.setBlockers(true);
         NubisDataFetcher.self.checkAuctionMarket(currentPage, new Item.Category[4] { Item.Category.CraftingIngredients, Item.Category.Armor, Item.Category.Weapon, Item.Category.Hats }, false);
      });
      armorFilterButton.onClick.AddListener(() => {
         rootPanel.setBlockers(true);
         NubisDataFetcher.self.checkAuctionMarket(currentPage, new Item.Category[1] { Item.Category.Armor }, false);
      });
      weaponFilterButton.onClick.AddListener(() => {
         rootPanel.setBlockers(true);
         NubisDataFetcher.self.checkAuctionMarket(currentPage, new Item.Category[1] { Item.Category.Weapon }, false);
      });
      ingredientFilterButton.onClick.AddListener(() => {
         rootPanel.setBlockers(true);
         NubisDataFetcher.self.checkAuctionMarket(currentPage, new Item.Category[1] { Item.Category.CraftingIngredients }, false);
      });
      hatFilterButton.onClick.AddListener(() => {
         rootPanel.setBlockers(true);
         NubisDataFetcher.self.checkAuctionMarket(currentPage, new Item.Category[1] { Item.Category.Hats }, false);
      }); 
   }

   private void toggleAuctionedUserItemLoader (bool isActive) {
      foreach (GameObject obj in userAuctionedItemBlockers) {
         obj.SetActive(isActive);
      }
   }

   public void loadAuctionItems (List<AuctionItemData> loadedItemList, bool isUserData) {
      Transform currentParent = transform;

      if (isUserData) {
         currentParent = userAuctionedItemHolder;
         toggleAuctionedUserItemLoader(false);
      } else {
         currentParent = auctionItemHolder;
      }

      currentParent.gameObject.DestroyChildren();
      auctionDetailPanel.clearContent();
      auctionItemTemplateList = new List<AuctionItemTemplate>();
      foreach (AuctionItemData itemData in loadedItemList) {
         AuctionItemTemplate newTemplate = Instantiate(auctionItemPrefab, currentParent).GetComponent<AuctionItemTemplate>();
         newTemplate.setTemplate(itemData);
         newTemplate.selectTemplateButton.onClick.AddListener(() => {
            clearItemHighlights();
            selectiedItemTemplate = newTemplate;
            auctionDetailPanel.loadItemData(newTemplate.auctionItemData);
            selectiedItemTemplate.toggleHighlight(true);
         });
         auctionItemTemplateList.Add(newTemplate);
      }
   }

   public void loadAuctionHistory (List<AuctionItemData> loadedItemList) {
      foreach (GameObject obj in userAuctionHistoryBlockers) {
         obj.SetActive(false);
      }
      auctionHistoryItemHolder.gameObject.DestroyChildren();
      foreach (AuctionItemData itemData in loadedItemList) {
         AuctionItemTemplate newTemplate = Instantiate(auctionItemPrefab, auctionHistoryItemHolder).GetComponent<AuctionItemTemplate>();
         newTemplate.setTemplate(itemData);
         newTemplate.selectTemplateButton.onClick.AddListener(() => {
            clearItemHighlights();
            selectiedItemTemplate = newTemplate;
            auctionDetailPanel.loadItemData(newTemplate.auctionItemData);
            selectiedItemTemplate.toggleHighlight(true);
         });
      }
   }

   private void clearItemHighlights () {
      foreach (AuctionItemTemplate itemData in auctionItemTemplateList) {
         itemData.toggleHighlight(false);
      }
   }

   #region Private Variables

   #endregion
}
