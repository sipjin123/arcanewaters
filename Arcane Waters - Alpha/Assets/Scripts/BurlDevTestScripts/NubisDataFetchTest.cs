#define NUBIS
#if NUBIS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using UnityEngine;
using NubisDataHandling;
using UnityEngine.Analytics;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Nubis.Controllers {
   public class NubisDataFetchTest : MonoBehaviour {
      #region Public Variables

      // The crafting ingredients
      public List<Item> ingredients = new List<Item>();

      // Test item Id
      public string itemId = "";

      // The events that are triggered after fetching
      public NubisUserInfoEvent nubisUserInfoEvent = new NubisUserInfoEvent();
      public NubisInventoryEvent nubisInventoryEvent = new NubisInventoryEvent();
      public NubisCraftingIngredientEvent nubisCraftingIngredientEvent = new NubisCraftingIngredientEvent();
      public NubisEquippedItemEvent nubisEquippedItemEvent = new NubisEquippedItemEvent();
      public NubisCraftableItemEvent nubisCraftableItemEvent = new NubisCraftableItemEvent();

      // The directory
      public string webDirectory = "";

      #endregion

      private void Awake () {
         webDirectory = "http://" + Global.getAddress(MyNetworkManager.ServerType.AmazonVPC) + ":7900/";
      }

      private void Update () {
         int userID = 745;

         if (Input.GetKeyDown(KeyCode.Alpha1)) {
            StartCoroutine(CO_FetchUserData(userID));
         }
         if (Input.GetKeyDown(KeyCode.Alpha2)) {
            StartCoroutine(CO_FetchIngredients(userID));
         }
         if (Input.GetKeyDown(KeyCode.Alpha3)) {
            StartCoroutine(CO_FetchEquippedItems(userID));
         }
         if (Input.GetKeyDown(KeyCode.Alpha4)) {
            StartCoroutine(CO_FetchCraftableGroups(userID));
         }
         if (Input.GetKeyDown(KeyCode.Alpha5)) {
            StartCoroutine(CO_FetchSingleBlueprint(userID, 5998));
         }
         if (Input.GetKeyDown(KeyCode.Alpha6)) {
            StartCoroutine(CO_FetchInventoryData(userID));
         }
      }

      private IEnumerator CO_TestWebReq () {
         D.editorLog("Start");
         int userID = 745;

         //http://52.1.218.202:7900/fetch_craftable_armors_v4?usrId=745

         UnityWebRequest www = UnityWebRequest.Get(webDirectory + "fetch_craftable_armors_v4?usrId=" + userID);
         yield return www.SendWebRequest();

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            // Grab the map data from the request
            string rawData = www.downloadHandler.text;
            D.editorLog("RawData is: " + rawData, Color.cyan);
         }
      }

      protected IEnumerator CO_FetchSingleBlueprint (int userId, int itmId) {
         UnityWebRequest www = UnityWebRequest.Get(webDirectory + "fetch_single_blueprint_v4?usrId=" + userId + "&bpId=" + itmId);
         yield return www.SendWebRequest();

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            string rawData = www.downloadHandler.text;
            D.editorLog("RawData is: " + rawData, Color.cyan);

            List<CraftableItemData> craftable = CraftableItem.processCraftableGroups(rawData, ingredients);

            foreach (CraftableItemData data in craftable) {
               D.editorLog("Info is: " + data.craftableItem.id + " : " + data.craftingStatus + " : " + data.craftableItem.category + " : " + data.craftableItem.itemTypeId, Color.red);
            }
         }
      }

      protected IEnumerator CO_FetchInventoryData (int userId) {
         string weaponRawData = "";
         string armorRawData = "";
         UnityWebRequest weaponInventoryRequest = UnityWebRequest.Get(webDirectory + "fetch_user_inventory_v1?usrId=" + userId + "&itmType=" + (int)EquipmentType.Weapon);
         yield return weaponInventoryRequest.SendWebRequest();
         UnityWebRequest armorInventoryRequest = UnityWebRequest.Get(webDirectory + "fetch_user_inventory_v1?usrId=" + userId + "&itmType=" + (int) EquipmentType.Armor);
         yield return armorInventoryRequest.SendWebRequest();

         if (weaponInventoryRequest.isNetworkError || weaponInventoryRequest.isHttpError) {
            D.warning(weaponInventoryRequest.error);
         } else {
            weaponRawData = weaponInventoryRequest.downloadHandler.text;
         }
         if (armorInventoryRequest.isNetworkError || armorInventoryRequest.isHttpError) {
            D.warning(armorInventoryRequest.error);
         } else {
            armorRawData = armorInventoryRequest.downloadHandler.text;
         }

         D.editorLog("Fetching raw data weapon is: " + weaponRawData, Color.cyan);
         D.editorLog("Fetching raw data armor is: " + armorRawData, Color.cyan);

         List<Item> compiledData = new List<Item>();
         List<Item> weaponList = UserInventory.processUserInventory(weaponRawData, EquipmentType.Weapon);
         List<Item> armorList = UserInventory.processUserInventory(armorRawData, EquipmentType.Armor);

         D.editorLog("================ Fetch Weapon: " + weaponList.Count, Color.blue);

         foreach (Item weapon in weaponList) {
            WeaponStatData weaponData = WeaponStatData.getStatData(weapon.data, 2);
            compiledData.Add(weapon);
            D.editorLog("Weapon: " + weapon.category + " : " + weapon.itemTypeId + " : " + weaponData.equipmentName, Color.yellow);
         }

         D.editorLog("================ Fetch Armor: " + armorList.Count, Color.blue);

         foreach (Item armor in armorList) {
            ArmorStatData armorData = ArmorStatData.getStatData(armor.data, 2);
            compiledData.Add(armor);
            D.editorLog("Weapon: " + armor.category + " : " + armor.itemTypeId + " : " + armorData.equipmentName, Color.yellow);
         }

         nubisInventoryEvent.Invoke(compiledData);
      }

      protected IEnumerator CO_FetchUserData (int userId) {
         UnityWebRequest www = UnityWebRequest.Get(webDirectory + "user_data_v1?usrId=" + userId);
         yield return www.SendWebRequest();

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            string rawData = www.downloadHandler.text;
            D.editorLog("RawData is: " + rawData, Color.cyan);


            UserInfo userInfo = UserInfoData.processUserInfo(rawData);
            nubisUserInfoEvent.Invoke(userInfo);

            D.editorLog("Fetch: " + rawData, Color.blue);
            D.editorLog("UserInfo AccName: " + userInfo.username, Color.yellow);
            D.editorLog("UserInfo UsrGold: " + userInfo.gold, Color.yellow);
            D.editorLog("UserInfo WepId: " + userInfo.weaponId, Color.yellow);
            D.editorLog("UserInfo ArmId: " + userInfo.armorId, Color.yellow);
         }
      }

      protected IEnumerator CO_FetchIngredients (int userId) {
         UnityWebRequest www = UnityWebRequest.Get(webDirectory + "fetch_crafting_ingredients_v3?usrId=" + userId);
         yield return www.SendWebRequest();

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            string rawData = www.downloadHandler.text;
            D.editorLog("RawData is: " + rawData, Color.cyan);

            List<Item> craftingList = NubisDataHandling.CraftingIngredients.processCraftingIngredients(rawData);
            D.editorLog("Here are the crafting list: " + craftingList.Count, Color.cyan);

            foreach (Item data in craftingList) {
               D.editorLog("Data: " + data.category + " : " + data.itemTypeId, Color.yellow);
            }

            ingredients = craftingList;
         }
      }

      protected IEnumerator CO_FetchCraftableGroups (int userId) {
         UnityWebRequest www = UnityWebRequest.Get(webDirectory + "fetch_craftable_weapons_v4?usrId=" + userId);
         yield return www.SendWebRequest();
         UnityWebRequest www2 = UnityWebRequest.Get(webDirectory + "fetch_craftable_armors_v4?usrId=" + userId);
         yield return www2.SendWebRequest();

         string weaponFetch = "";
         string armorFetch = "";

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
            yield return null;
         } else {
            weaponFetch = www.downloadHandler.text;
         }

         if (www2.isNetworkError || www2.isHttpError) {
            D.warning(www2.error);
            yield return null;
         } else {
            armorFetch = www2.downloadHandler.text;
         }

         D.editorLog("Weapon Fetch: " + weaponFetch, Color.blue);
         D.editorLog("Armor Fetch: " + armorFetch, Color.blue);

         List<CraftableItemData> weaponCraftables = CraftableItem.processCraftableGroups(weaponFetch, ingredients);
         List<CraftableItemData> armorCraftables = CraftableItem.processCraftableGroups(armorFetch, ingredients);

         List<CraftableItemData> compiledCraftables = new List<CraftableItemData>();
         foreach (CraftableItemData weaponData in weaponCraftables) {
            compiledCraftables.Add(weaponData);
            D.editorLog("Weapon: " + weaponData.craftingStatus + " : " + weaponData.craftableItem.category + " : " + weaponData.craftableItem.itemTypeId);
         }
         foreach (CraftableItemData armorData in armorCraftables) {
            compiledCraftables.Add(armorData);
            D.editorLog("Armor: " + armorData.craftingStatus + " : " + armorData.craftableItem.category + " : " + armorData.craftableItem.itemTypeId);
         }

         D.editorLog("Total craftable equipment are: " + compiledCraftables.Count, Color.cyan);
      }

      protected IEnumerator CO_FetchEquippedItems (int userId) {
         UnityWebRequest www = UnityWebRequest.Get(webDirectory + "fetch_equipped_items_v3?usrId=" + userId);
         yield return www.SendWebRequest();

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            string rawData = www.downloadHandler.text;
            D.editorLog("RawData is: " + rawData, Color.cyan);

            EquippedItemData equippedItemData = EquippedItems.processEquippedItemData(rawData);

            D.editorLog("The equippedItem weapon is: " + equippedItemData.weaponItem.id + " : " + equippedItemData.weaponItem.category + " : " + equippedItemData.weaponItem.itemTypeId, Color.yellow);
            D.editorLog("The equippedItem armor is: " + equippedItemData.armorItem.id + " : " + equippedItemData.armorItem.category + " : " + equippedItemData.armorItem.itemTypeId, Color.yellow);
         }
      }

      #region Private Variables

      #endregion
   }
}
#endif