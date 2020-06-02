using System;
using NubisDataHandling;
using NubisTranslator;
using UnityEngine;

public class NubisDataFetchTest : MonoBehaviour
{
   #region Public Variables

   // Only run this script for this device
   public static string DEVICE_NAME = "DESKTOP-7UVTQ74";

   #endregion

   private void Awake () {
   }

   private void Update () {
      if (SystemInfo.deviceName == DEVICE_NAME) {
         if (Input.GetKeyDown(KeyCode.Alpha1)) {
            nubisUserData();
         }
         if (Input.GetKeyDown(KeyCode.Alpha2)) {
            nubisWeapons();
         }
         if (Input.GetKeyDown(KeyCode.Alpha3)) {
            nubisArmors();
         }
         if (Input.GetKeyDown(KeyCode.Alpha4)) {
            nubisIngredients();
         }
         if (Input.GetKeyDown(KeyCode.Alpha5)) {
            nubisEquippedItems();
         }
         if (Input.GetKeyDown(KeyCode.Alpha6)) {
            nubisInventory();
         }
         if (Input.GetKeyDown(KeyCode.Alpha7)) {
            nubisMap();
         }
         if (Input.GetKeyDown(KeyCode.Alpha8)) {
            nubisSingleBP();
         }
         if (Input.GetKeyDown(KeyCode.Alpha9)) {
            nubisXMLBytes();
         }
         if (Input.GetKeyDown(KeyCode.Alpha0)) {
            nubisXmlVer();
         }

         if (Input.GetKeyDown(KeyCode.B)) {
            D.editorLog("Fetch cxml");
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string result = DB_Main.nubisFetchXmlVersion("777");

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Thred: " + result);
               });
            });
            
            //nubisMap2();
         }
         if (Input.GetKeyDown(KeyCode.Z)) {
            string call = "745" + NubisDataFetcher.SPACER + "0" + NubisDataFetcher.SPACER + "0";
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string result = DB_Main.nubisFetchInventory(call);

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Thred: " + result);
               });
            });
         }
         if (Input.GetKeyDown(KeyCode.X)) {
            string call = "745" + NubisDataFetcher.SPACER + "1" + NubisDataFetcher.SPACER + "0";
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string result = DB_Main.nubisFetchInventory(call);

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Thred: " + result);
               });
            });
         }
         if (Input.GetKeyDown(KeyCode.C)) {
            string call = "745" + NubisDataFetcher.SPACER + "2" + NubisDataFetcher.SPACER + "0";
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string result = DB_Main.nubisFetchInventory(call);

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Thred: " + result);
               });
            });
         }
         if (Input.GetKeyDown(KeyCode.V)) {
            D.editorLog("Fetching Helm", Color.green);
            string call = "745" + NubisDataFetcher.SPACER + "3" + NubisDataFetcher.SPACER + "0";
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string result = NubisTranslator.Fetch_Inventory_v1Controller.userInventory(745, 0, 3, 0, 0);

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Thred: " + result);
               });
            });
         }
         if (Input.GetKeyDown(KeyCode.K)) {
            nubisHelm();
         }
         if (Input.GetKeyDown(KeyCode.M)) {
            nubisTotalItems();
         }
      }
   }

   private async void nubisHelm () {
      D.debug("ASync start");
      string call = "745" + NubisDataFetcher.SPACER + "0" + NubisDataFetcher.SPACER + "3" + NubisDataFetcher.SPACER + "0" + NubisDataFetcher.SPACER + "0";
      D.editorLog("The call is: " + call, Color.green);
      DB_Main.nubisFetchInventory(call);
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchInventory), call);

      D.debug("ASync start: " + returnCode);
   }

   private async void nubisTotalItems () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchInventoryCount), "745"+NubisDataFetcher.SPACER+"0");
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisUserData () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchUserData), "745"); 
      D.debug("ASync start: "+ returnCode); 
   }

   private async void nubisWeapons () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchCraftableWeapons), "745"); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisArmors () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchCraftableArmors), "745"); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisIngredients () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchCraftingIngredients), "745"); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisEquippedItems () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchEquippedItems), "745"); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisInventory () {
      D.debug("ASync start");
      string call = "745_space_1_0";
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchInventory), call);

      D.debug("ASync start: " + returnCode);
   }

   private async void nubisMap () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchMapData), "burl_farm"); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisMap2 () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchMapData), "Shroom Ruins");
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisSingleBP () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchSingleBlueprint), "5998_space_745"); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisXmlVer () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchXmlVersion), "745"); 
      D.debug("ASync start: " + returnCode);
   }

   private async void nubisXMLBytes () {
      D.debug("ASync start");
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchXmlZipBytes), "745"); 
      D.debug("ASync start: " + returnCode);
   }

   #region Private Variables

   #endregion
}
