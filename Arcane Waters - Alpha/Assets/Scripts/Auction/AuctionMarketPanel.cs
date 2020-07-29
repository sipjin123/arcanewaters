using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AuctionMarketPanel : MonoBehaviour {
   #region Public Variables

   // The root panel
   public AuctionRootPanel rootPanel;

   // The prefab holder
   public Transform auctionItemHolder;

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

   // Max auction market item count
   public static int MAX_PAGE_COUNT = 10;

   // The page of the panel
   public int currentPage = 0;

   #endregion

   private void Awake () {
      closeMainPanelButton.onClick.AddListener(() => {
         rootPanel.hide();
      });

      auctionItemButton.onClick.AddListener(() => {
         rootPanel.userPanel.gameObject.SetActive(true);
         rootPanel.userPanel.setBlockers(true);
         Global.player.rpc.Cmd_RequestUserItemsForAuction(0, Item.Category.None);
      });

      allFilterButton.onClick.AddListener(() => {
         rootPanel.setBlockers(true);
         Global.player.rpc.Cmd_RequestAuctionItemData(currentPage, new Item.Category[4] { Item.Category.CraftingIngredients, Item.Category.Armor, Item.Category.Weapon, Item.Category.Hats });
      });
      armorFilterButton.onClick.AddListener(() => {
         rootPanel.setBlockers(true);
         Global.player.rpc.Cmd_RequestAuctionItemData(currentPage, new Item.Category[1] { Item.Category.Armor });
      });
      weaponFilterButton.onClick.AddListener(() => {
         rootPanel.setBlockers(true);
         Global.player.rpc.Cmd_RequestAuctionItemData(currentPage, new Item.Category[1] { Item.Category.Weapon });
      });
      ingredientFilterButton.onClick.AddListener(() => {
         rootPanel.setBlockers(true);
         Global.player.rpc.Cmd_RequestAuctionItemData(currentPage, new Item.Category[1] { Item.Category.CraftingIngredients });
      });
      hatFilterButton.onClick.AddListener(() => {
         rootPanel.setBlockers(true);
         Global.player.rpc.Cmd_RequestAuctionItemData(currentPage, new Item.Category[1] { Item.Category.Hats });
      }); 
   }

   public void loadAuctionItems (List<AuctionItemData> loadedItemList) {
      auctionItemHolder.gameObject.DestroyChildren();
      auctionDetailPanel.clearContent();

      foreach (AuctionItemData itemData in loadedItemList) {
         AuctionItemTemplate newTemplate = Instantiate(auctionItemPrefab, auctionItemHolder).GetComponent<AuctionItemTemplate>();
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

   private void clearItemHighlights () {
      foreach (AuctionItemTemplate itemData in auctionItemTemplateList) {
         itemData.toggleHighlight(false);
      }
   }

   #region Private Variables

   #endregion
}
