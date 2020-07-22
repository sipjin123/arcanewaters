using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class CropsDataToolManager : XmlDataToolManager {
   #region Public Variables

   // The crops tool main scene
   public CropsDataScene cropsScene;

   public class CropsDataGroup
   {
      // The unique id registered in the sql database
      public int xmlId;

      // The owner of the content
      public int creatorId;

      // The data of the crops
      public CropsData cropsData;

      // Determines if the entry is enabled in the database
      public bool isEnabled;
   }

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   private void Start () {
      Invoke("loadXMLData", MasterToolScene.loadDelay);
   }

   public void deleteDataFile (int xml_id) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteCropsXML(xml_id);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (CropsData data) {
      data.xmlName = MasterToolScene.UNDEFINED;
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateCropsXML(longString, -1, data.cropsType, false, data.xmlName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      XmlLoadingPanel.self.startLoading();
      _cropsDataList = new List<CropsDataGroup>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getCropsXML();
         userIdData = DB_Main.getSQLDataByID(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair rawXml in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawXml.rawXmlData);
               CropsData cropsData = Util.xmlLoad<CropsData>(newTextAsset);

               CropsDataGroup newDataGroup = new CropsDataGroup { 
                  creatorId = rawXml.xmlOwnerId, 
                  cropsData = cropsData,
                  xmlId = rawXml.xmlId,
                  isEnabled = rawXml.isEnabled
               };
               _cropsDataList.Add(newDataGroup);
            }

            cropsScene.loadData(_cropsDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void saveXMLData (CropsData data, int xml_id, bool isActive) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateCropsXML(longString, xml_id, data.cropsType, data.isEnabled, data.xmlName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   #region Private Variables

   // The collection of crops data
   protected List<CropsDataGroup> _cropsDataList;

   #endregion
}
