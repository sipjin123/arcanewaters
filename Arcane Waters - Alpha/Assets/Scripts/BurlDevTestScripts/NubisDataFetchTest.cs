using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NubisDataHandling;
using UnityEngine;

public class NubisDataFetchTest : MonoBehaviour
{
   #region Public Variables

   // Only run this script for this device
   public static string DEVICE_NAME1 = "DESKTOP-7UVTQ74";
   public static string DEVICE_NAME2 = "DESKTOP-N7K3SH1";

   public int testUserId = 745;

   #endregion

   public static bool isDevTestDevice () {
      if (SystemInfo.deviceName == DEVICE_NAME1 || SystemInfo.deviceName == DEVICE_NAME2) {
         return true;
      }
      return false;
   }

   private void Awake () {
   }

   private void OnGUI () {
      if (SystemInfo.deviceName == DEVICE_NAME1) {
         if (GUILayout.Button("Get XML Version Directly from Nubis")) {

            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string result = DB_Main.fetchXmlVersion("3");

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Thred: " + result);
               });
            });
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
               string result = DB_Main.getUserInfoJSON("745");

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
         if (GUILayout.Button("Fetch Map Directly from Nubis")) {
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string abilityContent = DB_Main.fetchMapData("burl_farm", "3");

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Result: " + abilityContent);
               });
            });
         }
      }
   }

   private void directFetchXmlData () {
      D.debug("Get Xml Bytes");
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         string returnCode = DB_Main.fetchZipRawData(((int) XmlSlotIndex.Default).ToString());

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            D.editorLog("Result: " + returnCode);
         });
      });
   }

   private async void nubisTotalItems () {
      List<Item.Category> newCategoryList = new List<Item.Category>();
      newCategoryList.Add(Item.Category.Weapon);
      newCategoryList.Add(Item.Category.Armor);
      newCategoryList.Add(Item.Category.CraftingIngredients);
      newCategoryList.Add(Item.Category.Blueprint);
      int[] args = Array.ConvertAll(newCategoryList.ToArray(), x => (int) x);
      string json = JsonConvert.SerializeObject(args);
      D.debug("Get Total items");
      var returnCode = await NubisClient.call(nameof(DB_Main.getItemCount), testUserId, json, "0", "0");
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisWeapons () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.fetchCraftableWeapons), testUserId); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisArmors () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.fetchCraftableArmors), testUserId); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisHats () {
      D.debug("Fetch Craftable Hats");
      var returnCode = await NubisClient.call(nameof(DB_Main.fetchCraftableHats), testUserId);
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisEquippedItems () {
      D.debug("Get Equipped Items");
      var returnCode = await NubisClient.call(nameof(DB_Main.fetchEquippedItems), testUserId); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisInventory () {
      D.debug("Get Inventory");
      var returnCode = await NubisClient.call(nameof(DB_Main.userInventory), testUserId, 0, 0, 0, 0, 0, InventoryPanel.ITEMS_PER_PAGE);
      List<Item> itemList = UserInventory.processUserInventory(returnCode);
      D.editorLog("Fetched a total inventory of: " + itemList.Count);
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisMap () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.fetchMapData), "burl_farm", 3); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisMap2 () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.fetchMapData), "Shroom Ruins", 1);
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisSingleBP () {
      D.debug("Fetching Single Bp");
      string itemId = "5767";
      var returnCode = await NubisClient.call(nameof(DB_Main.fetchSingleBlueprint), itemId, testUserId); 
      D.debug("Result: " + returnCode);
   }

   private async void nubisXmlVer () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.fetchXmlVersion), (int) XmlSlotIndex.Default); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisXMLBytes () {
      D.debug("Fetching xml zip");
      string returnCode = await NubisClient.call(nameof(DB_Main.fetchZipRawData), (int)XmlSlotIndex.Default); 
      D.debug("Result: " + returnCode.Length);
   }

   #region Private Variables

   #endregion
}
