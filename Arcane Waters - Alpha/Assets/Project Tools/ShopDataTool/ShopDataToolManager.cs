using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System;
using System.Linq;
public class ShopDataToolManager : XmlDataToolManager {
   #region Public Variables

   // Holds the main scene for the shop data
   public ShopToolScene shopScene;

   public class ShopDataGroup {
      // Id of the entry
      public int xmlId;

      // The owner of the entry
      public int creatorUserId;

      // If the entry is active in the database
      public bool isActive;
      
      // The info of the shop
      public ShopData shopData;
   }

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      // Initialize equipment data first
      Invoke("initializeEquipmentData", MasterToolScene.loadDelay);
      XmlLoadingPanel.self.startLoading();
   }

   private void initializeEquipmentData () {
      // Initialize all craftable item data after equipment data is setup
      EquipmentXMLManager.self.finishedDataSetup.AddListener(() => {
         loadXMLData();
      });

      EquipmentXMLManager.self.initializeDataCache();
   }

   public void saveXMLData (ShopData data, int xmlId) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateShopXML(longString, data.shopName, xmlId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteDataFile (int xmlId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteShopXML(xmlId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (ShopData data) {
      data.shopName = MasterToolScene.UNDEFINED;
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateShopXML(longString, data.shopName, -1);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      XmlLoadingPanel.self.startLoading();
      _shopDataList = new Dictionary<int, ShopDataGroup>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getShopXML();
         userNameData = DB_Main.getSQLDataByName(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               ShopData shopData = Util.xmlLoad<ShopData>(newTextAsset);
               shopData.shopId = xmlPair.xmlId;

               // Save the Ship data in the memory cache
               if (!_shopDataList.ContainsKey(xmlPair.xmlId)) {
                  _shopDataList.Add(xmlPair.xmlId, new ShopDataGroup {
                       xmlId = xmlPair.xmlId,
                       creatorUserId = xmlPair.xmlOwnerId,
                       isActive = true,
                       shopData = shopData
                  });
               }
            }

            shopScene.loadShopData(_shopDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Holds the list of ship data
   private Dictionary<int, ShopDataGroup> _shopDataList = new Dictionary<int, ShopDataGroup>();

   #endregion
}
