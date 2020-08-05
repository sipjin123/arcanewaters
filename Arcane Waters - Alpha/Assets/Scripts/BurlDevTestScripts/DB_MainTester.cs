using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using Newtonsoft.Json;

public class DB_MainTester : MonoBehaviour {
   #region Public Variables

   #endregion

   private void OnGUI () {
      if (GUILayout.Button("Fetch armor")) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            var result = DB_Main.getArmor(745);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               D.editorLog("Thred: " + result.id+" : "+result.itemTypeId);
            });
         });
      }
      if (GUILayout.Button("Fetch desert town lite")) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            var result = DB_Main.getMapInfo("desert town lite");

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               D.editorLog("Thred: " + result + " : " + result.gameData);

            });
         });
      }

      if (GUILayout.Button("Fetch User Inventory")) {
         List<Item.Category> newcategoryList = new List<Item.Category>();
         int[] categoryInt = Array.ConvertAll(newcategoryList.ToArray(), x => (int) x);
         string categoryJson = JsonConvert.SerializeObject(categoryInt);
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            var result = DB_Main.userInventory("745", "0", ((int)Item.Category.Weapon).ToString(), "0", "0", "0", "30");

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               D.editorLog("Thred: " + result);

            });
         });
      }


      if (GUILayout.Button("Fetch UserInfo JSON")) {
         testAsync();
      }
   }

   private async void testAsync () {
      D.editorLog("Test fetch");
      UserInfo hatFetch = await NubisClient.call<UserInfo>(nameof(DB_Main.getUserInfoNubisTest), "745");
      D.editorLog("Return string is: " + hatFetch);
   }

   #region Private Variables

   #endregion
}
