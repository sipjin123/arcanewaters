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
         ColorType hairColor1 = (ColorType) int.Parse(xmlPairCollection["hairColor1"]);
         ColorType hairColor2 = (ColorType) int.Parse(xmlPairCollection["hairColor2"]);
         EyesLayer.Type eyesType = (EyesLayer.Type) int.Parse(xmlPairCollection["eyesType"]);
         ColorType eyesColor1 = (ColorType) int.Parse(xmlPairCollection["eyesColor1"]);
         ColorType eyesColor2 = (ColorType) int.Parse(xmlPairCollection["eyesColor2"]);
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
            hairColor1 = hairColor1,
            hairColor2 = hairColor2,
            eyesType = eyesType,
            eyesColor1 = eyesColor1,
            eyesColor2 = eyesColor2,
            weaponId = weaponId,
            armorId = armorId
         };

         return newInfo;
      }
   }
}