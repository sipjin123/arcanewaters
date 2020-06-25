using System.Collections.Generic;
using System;

namespace NubisDataHandling {
   public class TreasureDrops {
      public static Dictionary<Biome.Type, TreasureDropsCollection> processTreasureDrops (string contentData) {
         Dictionary<Biome.Type, TreasureDropsCollection> newTreasureDropsCollection = new Dictionary<Biome.Type, TreasureDropsCollection>();
         string splitter = "[next]";
         string[] rawItemGroup = contentData.Split(new string[] { splitter }, StringSplitOptions.None);

         for (int i = 0; i < rawItemGroup.Length; i++) {
            string itemGroup = rawItemGroup[i];
            string subSplitter = "[space]";
            string[] dataGroup = itemGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);

            if (dataGroup.Length > 1) {
               try {
                  Biome.Type biomeType = (Biome.Type) int.Parse(dataGroup[0]);
                  TreasureDropsCollection treasureCollectionData = Util.xmlLoad<TreasureDropsCollection>(dataGroup[1]);
                  newTreasureDropsCollection.Add(biomeType, treasureCollectionData);
               } catch {
                  D.editorLog("Error: " + dataGroup[0]);
                  D.editorLog("Error: " + dataGroup[1]);
               }
            }
         }

         return newTreasureDropsCollection;
      }
   }
}