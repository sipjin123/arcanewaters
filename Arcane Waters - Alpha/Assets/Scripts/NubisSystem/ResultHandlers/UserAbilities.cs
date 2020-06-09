using UnityEngine;
using System.Collections.Generic;
using System;

namespace NubisDataHandling
{
   public static class UserAbilities {
      public static List<AbilitySQLData> processUserAbilities (string contentData) {
         string splitter = "_space_";
         string[] rawItemGroup = contentData.Split(new string[] { splitter }, StringSplitOptions.None);
         
         List<AbilitySQLData> abilityDataList = new List<AbilitySQLData>();
         List<string> jsonContentList = new List<string>();

         // Filter valid json files
         foreach (string abilityJsonData in rawItemGroup) {
            if (abilityJsonData.Length > 10) {
               jsonContentList.Add(abilityJsonData);
            }
         }

         // Translate date into class
         foreach (AbilitySQLData abilityEntry in Util.unserialize<AbilitySQLData>(jsonContentList.ToArray())) {
            abilityDataList.Add(abilityEntry);
         }
         return abilityDataList;
      }
   }
}