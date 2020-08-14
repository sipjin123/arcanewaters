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
      if (GUILayout.Button("Add player skill")) {
         giveProjectileAbilities();
      }

      if (GUILayout.Button("Create Account")) {
         DB_Main.createAccount("burlin1", "test", "burlin1@codecommode.com", 1);
      }

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
               D.editorLog("Thred: " + result);

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

      if (GUILayout.Button("Fetch User Info by ID JSON DBMain")) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string result = DB_Main.getUserInfoJSON("745");

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               D.editorLog("Thred: " + result);
            });
         });
      }
   }

   private void giveProjectileAbilities () {
      DB_Main.updateAbilitiesData(745, new AbilitySQLData {
         abilityID = 78,
         abilityLevel = 1,
         abilityType = AbilityType.Standard,
         description = "Piercing Attack",
         equipSlotIndex = -1,
         name = "Piercing Bullet"
      });
      DB_Main.updateAbilitiesData(745, new AbilitySQLData {
         abilityID = 79,
         abilityLevel = 1,
         abilityType = AbilityType.Standard,
         description = "Stone Bullet",
         equipSlotIndex = -1,
         name = "Stone Bullet"
      });
      DB_Main.updateAbilitiesData(745, new AbilitySQLData {
         abilityID = 80,
         abilityLevel = 1,
         abilityType = AbilityType.Standard,
         description = "Explosive Bullet",
         equipSlotIndex = -1,
         name = "Explosive Bullet"
      });
      DB_Main.updateAbilitiesData(745, new AbilitySQLData {
         abilityID = 81,
         abilityLevel = 1,
         abilityType = AbilityType.Standard,
         description = "Red Pellet",
         equipSlotIndex = -1,
         name = "Red Pellet"
      });
      DB_Main.updateAbilitiesData(745, new AbilitySQLData {
         abilityID = 27,
         abilityLevel = 1,
         abilityType = AbilityType.Standard,
         description = "Bullet Fire",
         equipSlotIndex = -1,
         name = "Bullet Fire"
      });
   }

   #region Private Variables

   #endregion
}
