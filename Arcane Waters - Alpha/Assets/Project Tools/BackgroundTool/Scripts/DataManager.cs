using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System.Linq;
using System;
using System.IO;

namespace BackgroundTool
{
   public class DataManager : MonoBehaviour
   {
      #region Public Variables

      // Button to save the data
      public Button saveButton, saveTextButton;

      // Load selected dropdown file
      public Button loadButton;

      // Dropdown refering to saved files
      public Dropdown dropDownFiles;

      // List of background data loaded from the server
      public List<BackgroundContentData> backgroundContentList;

      // Reference to the error indicators
      public GameObject errorPanel;
      public Text errorText;
      public Button exitErrorPanelButton;

      // The name of the background template
      public InputField backgroundName;

      // The minimum spawn points required per team
      public const int MIN_SPAWN_COUNT = 6;

      // Error Messages
      public static string SPAWNPOINT_ERROR = "Not enough spawn points in the scene.";

      // Links the dropdown option index and xml id
      public Dictionary<int, int> backgroundXmlPair;

      // The recent xml id used before overwritting data
      public int cachedXMLId = -1;

      // Determines the biome type of the background
      public Dropdown biomeDropdown;

      // The id of the owner of the content
      public int currentOwnerID;

      #endregion

      private void Start () {
         bool canSaveData = MasterToolAccountManager.PERMISSION_LEVEL == AdminManager.Type.ContentWriter 
            || MasterToolAccountManager.PERMISSION_LEVEL == AdminManager.Type.Admin;

         saveButton.interactable = canSaveData;

         saveButton.onClick.AddListener(() => {
            saveData();
         });

         loadButton.onClick.AddListener(() => {
            string currentOption = dropDownFiles.options[dropDownFiles.value].text;
            BackgroundContentData contentData = backgroundContentList.Find(_ => _.backgroundName == currentOption);
            if (contentData != null) {
               Dropdown.OptionData optionData = biomeDropdown.options.Find(_ => _.text == contentData.biomeType.ToString());
               biomeDropdown.value = biomeDropdown.options.IndexOf(optionData);
               ImageManipulator.self.generateSprites(contentData.spriteTemplateList);
               currentOwnerID = contentData.ownerId;
            }
         });

         dropDownFiles.onValueChanged.AddListener(_ => {
            backgroundName.text = dropDownFiles.options[_].text;
         });

         exitErrorPanelButton.onClick.AddListener(() => {
            showErrorPanel(false, "");
         });

         // Setup biome options
         List<Dropdown.OptionData> biomeOptions = new List<Dropdown.OptionData>();
         foreach (Biome.Type biomeType in Enum.GetValues(typeof(Biome.Type))) {
            Dropdown.OptionData newOptionData = new Dropdown.OptionData {
               image = null,
               text = biomeType.ToString()
            };

            biomeOptions.Add(newOptionData);
         }
         biomeDropdown.options = biomeOptions;

         Invoke("loadData", MasterToolScene.loadDelay);
      }

      private void saveData () {
         List<SpriteTemplateData> spriteTempList = ImageManipulator.self.spriteTemplateDataList;
         if (hasValidContent(spriteTempList)) {
            BackgroundContentData newContentData = new BackgroundContentData();

            // Set id and name of bg data
            int currentID = backgroundXmlPair[dropDownFiles.value];
            newContentData.xmlId = currentID;
            newContentData.backgroundName = backgroundName.text;

            // Get the sprite data list
            newContentData.spriteTemplateList = spriteTempList;

            // Get biome data
            string biomeName = biomeDropdown.options[biomeDropdown.value].text;
            newContentData.biomeType = (Biome.Type) Enum.Parse(typeof(Biome.Type), biomeName);

            // Save to SQL Database
            saveData(newContentData);
         } else {
            showErrorPanel(true, SPAWNPOINT_ERROR);
         }
      }

      private bool hasValidContent (List<SpriteTemplateData> spriteTempList) {
         bool hasEnoughDefenderSlots = spriteTempList.FindAll(_ => _.contentCategory == ImageLoader.BGContentCategory.SpawnPoints_Defenders).Count >= MIN_SPAWN_COUNT;
         bool hasEnoughAttackerSlots = spriteTempList.FindAll(_ => _.contentCategory == ImageLoader.BGContentCategory.SpawnPoints_Attackers).Count >= MIN_SPAWN_COUNT;

         if (hasEnoughDefenderSlots && hasEnoughAttackerSlots) {
            return true;
         } else {
            return false;
         }
      }

      private void showErrorPanel (bool isActive, string message) {
         errorText.text = message;
         errorPanel.SetActive(isActive);
      }

      public void saveData (BackgroundContentData data) {
         cachedXMLId = data.xmlId;

         XmlSerializer ser = new XmlSerializer(data.GetType());
         var sb = new StringBuilder();
         using (var writer = XmlWriter.Create(sb)) {
            ser.Serialize(writer, data);
         }

         string xmlContent = sb.ToString();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            cachedXMLId = DB_Main.updateBackgroundXML(data.xmlId, xmlContent, data.backgroundName);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               loadData();
            });
         });
      }

      public void loadData () {
         XmlLoadingPanel.self.startLoading();
         backgroundContentList = new List<BackgroundContentData>();
         backgroundContentList.Add(new BackgroundContentData { backgroundName = MasterToolScene.UNDEFINED, xmlId = -1 });

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> backgroundData = DB_Main.getBackgroundXML();
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (XMLPair xmlPair in backgroundData) {

                  if ((xmlPair.xmlOwnerId == MasterToolAccountManager.self.currentAccountID
                  && MasterToolAccountManager.PERMISSION_LEVEL == AdminManager.Type.ContentWriter)
                  || MasterToolAccountManager.PERMISSION_LEVEL == AdminManager.Type.Admin
                  || MasterToolAccountManager.PERMISSION_LEVEL == AdminManager.Type.QA) {

                     TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
                     BackgroundContentData bgContentData = Util.xmlLoad<BackgroundContentData>(newTextAsset);
                     bgContentData.xmlId = xmlPair.xmlId;
                     bgContentData.ownerId = xmlPair.xmlOwnerId;
                     backgroundContentList.Add(bgContentData);
                  }
               }
               setUIContent();
            });
         });
      }

      private void setUIContent () {
         dropDownFiles.ClearOptions();
         backgroundXmlPair = new Dictionary<int, int>();
         List<Dropdown.OptionData> optionList = new List<Dropdown.OptionData>();

         int index = 0;
         int cachedIndex = 0;
         foreach (BackgroundContentData bgContentData in backgroundContentList) {
            Dropdown.OptionData newOptionData = new Dropdown.OptionData {
               image = null,
               text = bgContentData.backgroundName
            };
            optionList.Add(newOptionData);
            backgroundXmlPair.Add(index, bgContentData.xmlId);

            if (bgContentData.xmlId == cachedXMLId) {
               cachedIndex = index;
            }
            index++;
         }
         dropDownFiles.AddOptions(optionList);

         dropDownFiles.value = cachedIndex;
         XmlLoadingPanel.self.finishLoading();
      }

      #region Private Variables

      #endregion
   }
}