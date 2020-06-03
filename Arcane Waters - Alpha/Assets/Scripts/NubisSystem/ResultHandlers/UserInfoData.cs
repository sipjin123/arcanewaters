using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Events;

namespace NubisDataHandling {
   [Serializable]
   public class NubisUserInfoEvent : UnityEvent<UserInfo> {
   }

   public static class UserInfoData {
      public static UserInfo processUserInfo (string contentData) {
         Dictionary<string, string> xmlPairCollection = new Dictionary<string, string>();

         // Grab the map data from the request
         string splitter = "[space]";
         string[] rawItemGroup = contentData.Split(new string[] { splitter }, StringSplitOptions.None);

         for (int i = 0; i < rawItemGroup.Length; i++) {
            string itemGroup = rawItemGroup[i];
            string subSplitter = ":";
            string[] dataGroup = itemGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);
            if (dataGroup.Length > 1) {
               xmlPairCollection.Add(dataGroup[0], dataGroup[1]);
            } else {
               xmlPairCollection.Add(dataGroup[0], "0");
            }
         }

         // Isolation of data parsing to detect future parsing errors
         int gold = int.Parse(xmlPairCollection["usrGold"]);
         int gem = int.Parse(xmlPairCollection["accGems"]);
         string userName = xmlPairCollection["usrName"];
         Gender.Type gender = (Gender.Type) int.Parse(xmlPairCollection["usrGender"]);
         BodyLayer.Type bodyType = (BodyLayer.Type) int.Parse(xmlPairCollection["bodyType"]);

         // Hair data setup
         HairLayer.Type hairType = (HairLayer.Type) int.Parse(xmlPairCollection["hairType"]);
         string hairPalette1 = PaletteDef.Hair.Yellow;
         string hairPalette2 = PaletteDef.Hair.Yellow;
         try {
            hairPalette1 = xmlPairCollection["hairPalette1"];
         } catch {
            D.debug("Hair palette 1 fetch Issue for user: " + userName);
         }
         try {
            hairPalette2 = xmlPairCollection["hairPalette2"];
         } catch {
            D.debug("Hair palette 2 fetch Issue for user: " + userName);
         }

         // Eyes data setup
         EyesLayer.Type eyesType = (EyesLayer.Type) int.Parse(xmlPairCollection["eyesType"]);
         string eyesPalette1 = PaletteDef.Eyes.Blue;
         string eyesPalette2 = PaletteDef.Eyes.Blue;
         try {
            eyesPalette1 = xmlPairCollection["eyesPalette1"];
         } catch {
            D.debug("Eye palette 1 fetch Issue for user: " + userName);
         }
         try {
            eyesPalette2 = xmlPairCollection["eyesPalette2"];
         } catch {
            D.debug("Eye palette 2 fetch Issue for user: " + userName);
         }

         // Equipped item setup
         int weaponId = int.Parse(xmlPairCollection["wpnId"]);
         int armorId = int.Parse(xmlPairCollection["armId"]);
         int hatId = 0;
         try {
            hatId = int.Parse(xmlPairCollection["helmId"]);
         } catch {
            D.debug("Hat fetch Issue for user: " + userName);
         }

         UserInfo newInfo = new UserInfo {
            gold = gold,
            gems = gem,
            gender = gender,
            username = userName,
            bodyType = bodyType,
            hairType = hairType,
            hairPalette1 = hairPalette1,
            hairPalette2 = hairPalette2,
            eyesType = eyesType,
            eyesPalette1 = eyesPalette1,
            eyesPalette2 = eyesPalette2,
            weaponId = weaponId,
            armorId = armorId,
            hatId = hatId
         };

         return newInfo;
      }
   }
}