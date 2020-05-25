using UnityEngine;

public class NubisDataFetchTest : MonoBehaviour
{
   #region Public Variables

   #endregion

   private void Awake () {
      string testmap = "Shroom+Ruins";
      testmap = testmap.Replace("+", " ");
      D.debug(testmap);
   }

   private void Update () {
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

      if (Input.GetKeyDown(KeyCode.X)) {
         nubisMap2();
      }
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
      var returnCode = await NubisClient.call(nameof(DB_Main.nubisFetchInventory), "745_space_1"); 
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
