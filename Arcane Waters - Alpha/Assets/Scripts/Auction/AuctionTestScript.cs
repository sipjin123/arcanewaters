using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using NubisDataHandling;

public class AuctionTestScript : MonoBehaviour {
   #region Public Variables

   // Game Panels
   public GameObject[] auctionPanels;

   #endregion

   private void OnGUI () {
      if (GUILayout.Button("OpenPanel")) {
         AuctionRootPanel panel = (AuctionRootPanel) PanelManager.self.get(Panel.Type.Auction);
         //PanelManager.self.popPanel();
         panel.show();
      }

      if (GUILayout.Button("Fetch auction data")) {
         D.editorLog("Press");
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string result = DB_Main.fetchAuctionData("745", "0", "5", "");
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
                  D.editorLog(newData.category + " : " + newData.itemTypeId + " : " + newData.id, Color.yellow);
               }


               AuctionRootPanel panel = (AuctionRootPanel) PanelManager.self.get(Panel.Type.Auction);
               panel.marketPanel.loadAuctionItems(actualData);
               panel.show();
            });
         });
      }
   }

   #region Private Variables

   #endregion
}
