using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class PlayerJobPanel : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public PlayerJobToolManager toolManager;

   // Holds the selection popup
   public GenericSelectionPopup selectionPopup;

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Caches the initial type incase it is changed
   public int startingType;

   // List for toggle able tabs
   public List<TogglerClass> togglerList;

   // Holds the reference to the stats UI
   public StatHolderPanel statHolderpanel;

   #endregion

   private void Awake () {
      saveButton.onClick.AddListener(() => {
         PlayerJobData itemData = getJobData();
         if (itemData != null) {
            if ((int)itemData.type != startingType) {
               toolManager.deleteDataFile(new PlayerJobData { type = (Jobs.Type)startingType });
            }
            toolManager.saveXMLData(itemData);
            gameObject.SetActive(false);
         }
      });
      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         toolManager.loadXMLData();
      });

      _jobTypeButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.PlayerJobType, _jobTypeText);
      });
      _changeAvatarSpriteButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.PlayerJobIcons, _avatarIcon, _avatarSpritePath);
      });

      foreach (TogglerClass toggler in togglerList) {
         toggler.initListeners();
      }
   }

   private PlayerJobData getJobData () {
      PlayerJobData jobData = new PlayerJobData();

      jobData.playerStats = statHolderpanel.getStatData();
      jobData.type = (Jobs.Type) Enum.Parse(typeof(Jobs.Type), _jobTypeText.text);
      jobData.jobName = _jobName.text;
      jobData.description = _jobDescription.text;
      jobData.jobIconPath = _avatarSpritePath.text;

      return jobData;
   }

   public void loadPlayerJobData (PlayerJobData jobData) {
      startingType = (int)jobData.type;
      _jobTypeText.text = jobData.type.ToString();
      _jobName.text = jobData.jobName;
      _jobDescription.text = jobData.description;

      _avatarSpritePath.text = jobData.jobIconPath;
      if (jobData.jobIconPath != null) {
         _avatarIcon.sprite = ImageManager.getSprite(jobData.jobIconPath);
      } else {
         _avatarIcon.sprite = selectionPopup.emptySprite;
      }

      statHolderpanel.loadStatData(jobData.playerStats);
   }

   #region Private Variables
#pragma warning disable 0649

   // Item Type
   [SerializeField]
   private Button _jobTypeButton;
   [SerializeField]
   private Text _jobTypeText;

   // Item Name
   [SerializeField]
   private InputField _jobName;

   // Item Info
   [SerializeField]
   private InputField _jobDescription;

   // Icon
   [SerializeField]
   private Button _changeAvatarSpriteButton;
   [SerializeField]
   private Text _avatarSpritePath;
   [SerializeField]
   private Image _avatarIcon;

#pragma warning restore 0649
   #endregion
}
