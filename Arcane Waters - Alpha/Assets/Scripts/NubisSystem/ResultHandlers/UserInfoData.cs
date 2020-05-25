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
         Gender.Type gender = (Gender.Type) int.Parse(xmlPairCollection["usrGender"]);
         BodyLayer.Type bodyType = (BodyLayer.Type) int.Parse(xmlPairCollection["bodyType"]);
         HairLayer.Type hairType = (HairLayer.Type) int.Parse(xmlPairCollection["hairType"]);
         string hairPalette1 = xmlPairCollection["hairPalette1"];
         string hairPalette2 = xmlPairCollection["hairPalette2"];
         EyesLayer.Type eyesType = (EyesLayer.Type) int.Parse(xmlPairCollection["eyesType"]);
         string eyesPalette1 = xmlPairCollection["eyesPalette1"];
         string eyesPalette2 = xmlPairCollection["eyesPalette2"];
         string userName = xmlPairCollection["usrName"];
         int weaponId = int.Parse(xmlPairCollection["wpnId"]);
         int armorId = int.Parse(xmlPairCollection["armId"]);

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
            armorId = armorId
         };

         return newInfo;
      }
   }
}