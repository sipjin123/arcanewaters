using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using NubisDataHandling;
using Newtonsoft.Json;

public class AuctionTestScript : MonoBehaviour {
   #region Public Variables

   // Self
   public static AuctionTestScript self;

   // Game Panels
   public GameObject[] auctionPanels;

   #endregion

   private void Awake () {
      self = this;
   }

   private void OnGUI () {

      if (GUILayout.Button("Fetch auction data")) {
         D.editorLog("Press");
         AuctionRootPanel panel = (AuctionRootPanel) PanelManager.self.get(Panel.Type.Auction);
         panel.show();
         panel.setBlockers(true);
         Global.player.rpc.Cmd_RequestAuctionItemData(0, new Item.Category[4] { Item.Category.Weapon, Item.Category.Armor, Item.Category.Hats, Item.Category.CraftingIngredients });
         /*
         return;
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<int> categoryFilter = new List<int>();
            categoryFilter.Add((int) Item.Category.Weapon);
            categoryFilter.Add((int) Item.Category.Armor);
            categoryFilter.Add((int) Item.Category.Hats);
            categoryFilter.Add((int) Item.Category.CraftingIngredients);

            string jsonFilter = JsonConvert.SerializeObject(categoryFilter);
            D.editorLog("The filter: " + jsonFilter, Color.cyan);
            string result = DB_Main.fetchAuctionData("745", "0", "5", jsonFilter);
            string returnCode = DB_Main.userInventory("745", "0", "0", "0", "0", "0");
            //int result = DB_Main.getLatestXmlVersion();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               D.editorLog("Thred: " + result);
               List<AuctionItemData> actualData = Util.xmlLoad<List<AuctionItemData>>(result);
               foreach(var newData in actualData) {
                  D.editorLog(newData.auctionId + " : " + newData.sellerId + " : " + newData.itemCategory + " : " + newData.itemTypeId, Color.magenta);
               }

               List<Item> itemList = UserInventory.processUserInventory(returnCode);
               D.editorLog("Item count was: " + itemList.Count);
               foreach (var newData in itemList) {
                //  D.editorLog(newData.category + " : " + newData.itemTypeId + " : " + newData.id, Color.yellow);
               }


               AuctionRootPanel panel = (AuctionRootPanel) PanelManager.self.get(Panel.Type.Auction);
               panel.marketPanel.loadAuctionItems(actualData);
               panel.show();
            });
         });*/
      }
   }

   #region Private Variables

   #endregion
}
