using System;
using System.Collections.Generic;
using NubisDataHandling;
using NubisTranslator;
using UnityEngine;
using static NubisRequestHandler;

public class NubisDataFetchTest : MonoBehaviour
{
   #region Public Variables

   // Only run this script for this device
   public static string DEVICE_NAME = "DESKTOP-7UVTQ74";

   #endregion

   private void Awake () {
   }

   private void OnGUI () {
      if (SystemInfo.deviceName == DEVICE_NAME) {
         if (GUILayout.Button("User Data")) {
            nubisUserData();
         }
         if (GUILayout.Button("Fetch DB_Main")) {
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               var result = DB_Main.getArmor(745);

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Thred: " + result);
               });
            });
         }
         if (GUILayout.Button("User Data Directly")) {
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string result = User_Data_v1Controller.userData(745);

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Thred: " + result);
               });
            });
         }
         if (GUILayout.Button("Equipped Items")) {
            nubisEquippedItems();
         }
         if (GUILayout.Button("Inventory Count")) {
            nubisTotalItems();
         }
         if (GUILayout.Button("Inventory Data")) {
            nubisInventory();
         }
         if (GUILayout.Button("Fetch Single Blueprint")) {
            nubisSingleBP();
         }
         if (GUILayout.Button("Fetch Craftable Hats")) {
            nubisHats();
         }
         if (GUILayout.Button("Fetch Xml Zip Data")) {
            nubisXMLBytes();
         }
         if (GUILayout.Button("TestCall")) {
            testNubisCall();
         }
         if (GUILayout.Button("Get DB_Main get monster data")) {
            D.editorLog("Fetching Land Monster Xml");
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               List<XMLPair> landMonsterData = DB_Main.getLandMonsterXML();

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Thred: " + landMonsterData.Count);
               });
            });
         }
         if (GUILayout.Button("Force update player pref xml version")) {
            int forcedVersion = 178;
            PlayerPrefs.SetInt(XmlVersionManagerClient.XML_VERSION, forcedVersion);
            D.editorLog("Finished");
         }
         if (GUILayout.Button("Directly Fetch Xml Data")) {
            directFetchXmlData();
         }
         if (GUILayout.Button("Fetch Abilities")) {
            //nubisFetchAbilities();
            
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string abilityContent = Fetch_Abilities_v1Controller.userAbilities(745);

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  List<AbilitySQLData> abilityList = UserAbilities.processUserAbilities(abilityContent);
                  D.editorLog("Result: " + abilityContent);
               });
            });
         }

         if (GUILayout.Button("Fetch Map Directly from Nubis")) {
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string abilityContent = Fetch_Map_Data_v1Controller.fetchMapData("burl_farm", 3);

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Result: " + abilityContent);
               });
            });
         }

         if (GUILayout.Button("Fetch Treasure Drops direct from DBMain")) {
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string rawData = Fetch_Treasure_Drops_v1Controller.fetchTreasureDrops();

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  Dictionary<Biome.Type, LootGroupData> treasureDataCollection = TreasureDrops.processTreasureDrops(rawData);
                  D.editorLog("Result: " + rawData);

                  foreach (KeyValuePair<Biome.Type, LootGroupData> keyPair in treasureDataCollection) {
                     D.editorLog(keyPair.Key + " : " + keyPair.Value.treasureDropsCollection.Count, Color.magenta);
                     foreach (TreasureDropsData dropsData in keyPair.Value.treasureDropsCollection) {
                        D.editorLog(dropsData.item.category + " : " + dropsData.item.itemTypeId + " : " + dropsData.item.itemName, Color.gray);
                     }
                  }
               });
            });
         }
      }
   }

   private async void nubisFetchAbilities () {
      D.debug("Nubis Call Start");
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchUserAbilities), "745");
      D.debug("Result: " + returnCode);
   }

   private async void testNubisCall () {
      D.debug("Nubis Call Start");
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisTestFetch), "message1", "message2");
      D.debug("Result: " + returnCode);
   }

   private void directFetchXmlData () {
      D.debug("Get Xml Bytes");
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         string returnCode = Fetch_XmlZip_Bytes_v1Controller.fetchZipRawData((int) XmlSlotIndex.Default);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            D.editorLog("Result: " + returnCode);
         });
      });
   }

   private async void nubisHat () {
      D.debug("ASync start");
      string call = "745" + NubisDataFetcher.SPACER + "0" + NubisDataFetcher.SPACER + "3" + NubisDataFetcher.SPACER + "0" + NubisDataFetcher.SPACER + "0" + NubisDataFetcher.SPACER + "0";
      D.editorLog("The call is: " + call, Color.green);
      NubisRequestHandler.nubisFetchInventory(call);
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchInventory), call);

      D.debug("ASync start: " + returnCode);
   }

   private async void nubisTotalItems () {
      D.debug("Get Total items");
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchInventoryCount), "745"+NubisDataFetcher.SPACER+"0");
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisUserData () {
      D.debug("Getting UserData");
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchUserData), "745"); 
      D.debug("ASync start: "+ returnCode); 
   }

   private async void nubisWeapons () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchCraftableWeapons), "745"); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisArmors () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchCraftableArmors), "745"); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisHats () {
      D.debug("Fetch Craftable Hats");
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchCraftableHats), "745");
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisIngredients () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchCraftingIngredients), "745"); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisEquippedItems () {
      D.debug("Get Equipped Items");
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchEquippedItems), "745"); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisInventory () {
      D.debug("Get Inventory");
      string call = "745_space_0_space_0_space_0_space_0_space_0";
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchInventory), call);
      List<Item> itemList = UserInventory.processUserInventory(returnCode);
      D.editorLog("Fetched a total inventory of: " + itemList.Count);
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisMap () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchMapData), "burl_farm", "3"); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisMap2 () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchMapData), "Shroom Ruins", "1");
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisSingleBP () {
      D.debug("Fetching Single Bp");
      string itemId = "5767";
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchSingleBlueprint), itemId+"_space_745"); 
      D.debug("Result: " + returnCode);
   }

   private async void nubisXmlVer () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchXmlVersion), ((int) XmlSlotIndex.Default).ToString()); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisXMLBytes () {
      D.debug("Fetching xml zip");
      string returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchXmlZipBytes), ((int)XmlSlotIndex.Default).ToString()); 
      D.debug("Result: " + returnCode.Length);
   }

   #region Private Variables

   #endregion
}
