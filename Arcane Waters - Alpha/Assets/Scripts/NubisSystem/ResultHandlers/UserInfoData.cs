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

         UserInfo newInfo = new UserInfo {
            gold = int.Parse(xmlPairCollection["usrGold"]),
            gems = int.Parse(xmlPairCollection["accGems"]),
            gender = (Gender.Type) int.Parse(xmlPairCollection["usrGender"]),
            username = xmlPairCollection["usrName"],
            bodyType = (BodyLayer.Type) int.Parse(xmlPairCollection["bodyType"]),
            hairType = (HairLayer.Type) int.Parse(xmlPairCollection["hairType"]),
            hairColor1 = (ColorType) int.Parse(xmlPairCollection["hairColor1"]),
            hairColor2 = (ColorType) int.Parse(xmlPairCollection["hairColor2"]),
            eyesType = (EyesLayer.Type) int.Parse(xmlPairCollection["eyesType"]),
            eyesColor1 = (ColorType) int.Parse(xmlPairCollection["eyesColor1"]),
            eyesColor2 = (ColorType) int.Parse(xmlPairCollection["eyesColor2"]),
            weaponId = int.Parse(xmlPairCollection["wpnId"]),
            armorId = int.Parse(xmlPairCollection["armId"])
         };

         return newInfo;
      }
   }
}