﻿using UnityEngine;
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
public class ShopDataToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the main scene for the shop data
   public ShopToolScene shopScene;

   // Self
   public static ShopDataToolManager self;

   // Holds the collection of user id that created the data entry
   public List<SQLEntryNameClass> _userIdData = new List<SQLEntryNameClass>();

   #endregion

   private void Awake () {
      self = this;
   }

   public bool didUserCreateData (string entryName) {
      SQLEntryNameClass sqlEntry = _userIdData.Find(_ => _.dataName == entryName);
      if (sqlEntry != null) {
         if (sqlEntry.ownerID == MasterToolAccountManager.self.currentAccountID) {
            return true;
         }
      } else {
         Debug.LogWarning("Entry does not exist: " + entryName);
      }

      return false;
   }

   private void Start () {
      Invoke("loadXMLData", MasterToolScene.loadDelay);
   }

   public void saveXMLData (ShopData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateShopXML(longString, data.shopName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteDataFile (ShopData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteShopXML(data.shopName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (ShopData data) {
      data.shopName += "_copy";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateShopXML(longString, data.shopName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      XmlLoadingPanel.self.startLoading();
      _shopDataList = new Dictionary<string, ShopData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getShopXML();
         _userIdData = DB_Main.getSQLDataByName(EditorSQLManager.EditorToolType.Shop);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               ShopData shopData = Util.xmlLoad<ShopData>(newTextAsset);

               // Save the Ship data in the memory cache
               if (!_shopDataList.ContainsKey(shopData.shopName)) {
                  _shopDataList.Add(shopData.shopName, shopData);
               }
            }

            shopScene.loadShopData(_shopDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Holds the list of ship data
   private Dictionary<string, ShopData> _shopDataList = new Dictionary<string, ShopData>();

   #endregion
}
