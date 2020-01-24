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

public class TutorialToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the main scene for the data templates
   public TutorialToolScene toolScene;

   // Self
   public static TutorialToolManager self;

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

   public void saveXMLData (TutorialData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateTutorialXML(longString, data.tutorialName, data.stepOrder);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void overwriteData (TutorialData data, string nameToDelete) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateTutorialXML(longString, data.tutorialName, data.stepOrder);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            deleteTutorialDataFile(new TutorialData { tutorialName = nameToDelete });
         });
      });
   }

   public void deleteTutorialDataFile (TutorialData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteTutorialXML(data.tutorialName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (TutorialData data) {
      data.tutorialName += "_copy";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateTutorialXML(longString, data.tutorialName, data.stepOrder);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      _tutorialDataList = new Dictionary<string, TutorialData>();

      XmlLoadingPanel.self.startLoading();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getTutorialXML();
         _userIdData = DB_Main.getSQLDataByName(EditorSQLManager.EditorToolType.Tutorial);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               TutorialData tutorialData = Util.xmlLoad<TutorialData>(newTextAsset);

               // Save the data in the memory cache
               if (!_tutorialDataList.ContainsKey(tutorialData.tutorialName)) {
                  _tutorialDataList.Add(tutorialData.tutorialName, tutorialData);
               }
            }
            toolScene.loadData(_tutorialDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Holds the list of tutorial data
   private Dictionary<string, TutorialData> _tutorialDataList = new Dictionary<string, TutorialData>();

   #endregion
}