using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class NewTutorialData {

   #region Public Variables

   // The id of the tutorial
   public int tutorialId;

   // The name of the tutorial
   public string tutorialName;

   // The description of the tutorial
   public string tutorialDescription;

   // The url of the image of the tutorial
   public string tutorialImageUrl;

   // The area key where the tutorial will be played
   public string tutorialAreaKey;

   // Wether the tutorial is active
   public bool tutorialIsActive;

   // The list of steps for this tutorial
   public List<TutorialStepData> tutorialStepList = new List<TutorialStepData>();

   #endregion

   public NewTutorialData () { }

   public NewTutorialData (int tutorialId, string tutorialName, string tutorialImageUrl, string tutorialAreaKey, bool tutorialIsActive) {
      this.tutorialId = tutorialId;
      this.tutorialName = tutorialName;
      this.tutorialImageUrl = tutorialImageUrl;
      this.tutorialAreaKey = tutorialAreaKey;
      this.tutorialIsActive = tutorialIsActive;
   }

#if IS_SERVER_BUILD

   public NewTutorialData (MySqlDataReader dataReader) {
      this.tutorialId = dataReader.GetInt32("tutorialId");
      this.tutorialName = dataReader.GetString("tutorialName");
      this.tutorialDescription = dataReader.GetString("tutorialDescription");
      this.tutorialImageUrl = dataReader.GetString("tutorialImageUrl");
      this.tutorialAreaKey = dataReader.GetString("tutorialAreaKey");
      this.tutorialIsActive = dataReader.GetBoolean("tutorialIsActive");
   }

#endif

   #region Private Variables

   #endregion
}
