using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AuctionMarketPanel : MonoBehaviour {
   #region Public Variables

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

   #endregion

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
