using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Networking;
using static EditorSQLManager;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System;

public class XmlVersionManagerClient : MonoBehaviour {
   #region Public Variables

   // The player pref key for clients
   public static string XML_VERSION = "xml_version";

   // The server directory
   public static string WEB_DIRECTORY = "http://localhost/";

   public static string XML_VERSION_GET = "getXmlVersion.php";
   public static string XML_ZIP_GET = "downloadZip.php?id=";

   // Path of the streaming filess
   public static string ZIP_PATH = Application.streamingAssetsPath + "/XmlZip/XmlContent.zip";
   public static string TEXT_PATH = Application.streamingAssetsPath + "/XmlTexts/";

   // Progress indicators
   public int targetProgress;
   public int currentProgress;

   // Self
   public static XmlVersionManagerClient self;

   // Blocks the character panel while loading data
   public GameObject loadBlocker;

   #endregion

   private void Awake () {
      self = this;
   }

   public void initializeClient () {
      loadBlocker.SetActive(true);
      StartCoroutine(CO_ProcessClientData());
   }

   private IEnumerator CO_ProcessClientData () {
      int clientXmlVersion = PlayerPrefs.GetInt(XML_VERSION, 0);
      D.log("Client version is: " + clientXmlVersion);

      UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + XML_VERSION_GET);
      yield return www.SendWebRequest();
      if (www.isNetworkError || www.isHttpError) {
         D.warning(www.error);
      } else {
         string rawData = www.downloadHandler.text;

         // Web request will return the xml version of the server
         int serverVersion = int.Parse(rawData);
         string clientMessage = "";

         if (serverVersion > clientXmlVersion) {
            clientMessage = "Client is outdated ver: " + clientXmlVersion + ", downloading new version: " + serverVersion;
            D.log(clientMessage);
            StartCoroutine(CO_DownloadClientData(serverVersion));
         } else {
            clientMessage = "Client is up to date";
            D.log(clientMessage);
            processClientXml();
         }
      }
   }

   private IEnumerator CO_DownloadClientData (int targetVersion) {
      yield return new WaitForSeconds(1);

      var uwr = new UnityWebRequest(WEB_DIRECTORY + XML_ZIP_GET + targetVersion, UnityWebRequest.kHttpVerbGET);
      string path = ZIP_PATH;
      string resultMessage = "";
      uwr.downloadHandler = new DownloadHandlerFile(path);
      yield return uwr.SendWebRequest();
      if (uwr.isNetworkError || uwr.isHttpError) {
         resultMessage = uwr.error;
      } else {
         resultMessage = "File successfully downloaded and saved to " + path;
      }

      D.log(resultMessage);
      StartCoroutine(CO_ExtractTextFiles());
      PlayerPrefs.SetInt(XML_VERSION, targetVersion);
   }

   private IEnumerator CO_ExtractTextFiles () {
      yield return new WaitForSeconds(.1f);
      using (ZipInputStream s = new ZipInputStream(File.OpenRead(ZIP_PATH))) {
         ZipEntry theEntry;
         while ((theEntry = s.GetNextEntry()) != null) {
            string directoryName = Path.GetDirectoryName(theEntry.Name);
            string fileName = Path.GetFileName(theEntry.Name);

            // create directory
            if (directoryName.Length > 0) {
               Directory.CreateDirectory(directoryName);
            }

            if (fileName != String.Empty) {
               string filename = TEXT_PATH + "/";
               filename += theEntry.Name;

               D.log("Unzipping: " + theEntry.Name);
               using (FileStream streamWriter = File.Create(filename)) {
                  int size = 2048;
                  byte[] fdata = new byte[2048];
                  while (true) {
                     size = s.Read(fdata, 0, fdata.Length);
                     if (size > 0) {
                        streamWriter.Write(fdata, 0, size);
                     } else {
                        break;
                     }
                  }
               }
            }
         }
      }

      D.log("Finished Extracting Zip");
      processClientXml();
   }

   private void processClientXml () {
      D.log("Initializing Text Extraction");
      targetProgress = 0;
      currentProgress = 0;
      StartCoroutine(CO_ExtractXmlData(EditorToolType.LandMonster));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.SeaMonster));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.BattlerAbility));
   }

   private IEnumerator CO_ExtractXmlData (EditorToolType xmlType) {
      targetProgress++;
      yield return new WaitForSeconds(.1f);

      switch (xmlType) {
         case EditorToolType.BattlerAbility:
            string path = TEXT_PATH + "abilities.txt";

            //Read the text from directly from the txt file
            StreamReader reader = new StreamReader(path);
            string abilityContent = reader.ReadToEnd();
            reader.Close();

            // Split each entry data
            string splitter = "[next]";
            string[] xmlGroup = abilityContent.Split(new string[] { splitter }, StringSplitOptions.None);

            List<AttackAbilityData> attackAbilityList = new List<AttackAbilityData>();
            List<BuffAbilityData> buffAbilityList = new List<BuffAbilityData>();
            foreach (string subGroup in xmlGroup) {
               string subSplitter = "[space]";
               string[] xmlSubGroup = subGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml managers
               if (xmlSubGroup.Length == 3) {
                  int abilityId = int.Parse(xmlSubGroup[0]);
                  int abilityType = int.Parse(xmlSubGroup[1]);
                  if (abilityType == 1 || abilityType == 3) {
                     AttackAbilityData attackAbility = Util.xmlLoad<AttackAbilityData>(xmlSubGroup[2]);
                     attackAbilityList.Add(attackAbility);
                  } else if (abilityType == 2) {
                     BuffAbilityData buffAbility = Util.xmlLoad<BuffAbilityData>(xmlSubGroup[2]);
                     buffAbilityList.Add(buffAbility);
                  }
               }
            }
            AbilityManager.self.receiveAbilitiesFromZipData(attackAbilityList.ToArray(), buffAbilityList.ToArray());
            break;
         case EditorToolType.LandMonster:
            path = TEXT_PATH + "land_monsters.txt";

            //Read the text from directly from the txt file
            reader = new StreamReader(path);
            string monsterContent = reader.ReadToEnd();
            reader.Close();

            // Split each entry data
            splitter = "[next]";
            xmlGroup = monsterContent.Split(new string[] { splitter }, StringSplitOptions.None);

            List<BattlerData> battlerDataList = new List<BattlerData>();
            foreach (string subGroup in xmlGroup) {
               string subSplitter = "[space]";
               string[] xmlSubGroup = subGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml managers
               if (xmlSubGroup.Length == 2) {
                  int monsterId = int.Parse(xmlSubGroup[0]);
                  BattlerData battlerData = Util.xmlLoad<BattlerData>(xmlSubGroup[1]);
                  battlerDataList.Add(battlerData);
               }
            }
            MonsterManager.self.receiveListFromZipData(battlerDataList.ToArray());
            break;
         case EditorToolType.SeaMonster:
            path = TEXT_PATH + "sea_monsters.txt";

            //Read the text from directly from the txt file
            reader = new StreamReader(path);
            string seaMonsterContent = reader.ReadToEnd();
            reader.Close();

            // Split each entry data
            splitter = "[next]";
            xmlGroup = seaMonsterContent.Split(new string[] { splitter }, StringSplitOptions.None);

            List<SeaMonsterEntityData> seaMonsterDataList = new List<SeaMonsterEntityData>();
            foreach (string subGroup in xmlGroup) {
               string subSplitter = "[space]";
               string[] xmlSubGroup = subGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml managers
               if (xmlSubGroup.Length == 2) {
                  int monsterId = int.Parse(xmlSubGroup[0]);
                  SeaMonsterEntityData seaMonsterData = Util.xmlLoad<SeaMonsterEntityData>(xmlSubGroup[1]);
                  seaMonsterDataList.Add(seaMonsterData);
               }
            }
            SeaMonsterManager.self.receiveListFromZipData(seaMonsterDataList.ToArray());
            break;
      }
      currentProgress++;
      checkTextExtractionProgress();
   }

   private void checkTextExtractionProgress () {
      D.log("Progress is: " + currentProgress + " / " + targetProgress);
      if (currentProgress >= targetProgress) {
         D.log("Finished assigning Xml Data");
         loadBlocker.SetActive(false);
      }
   }

   #region Private Variables

   #endregion
}
