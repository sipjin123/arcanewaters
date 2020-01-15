using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class ShipAbilityPanel : MonoBehaviour {
   #region Public Variables

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Reference to the tool manager
   public ShipAbilityToolManager toolManager;

   // Audio player
   public AudioSource audioSource;

   // Holds the selection popup
   public GenericSelectionPopup selectionPopup;

   // Test SFX Buttons
   public Button testHitSFXButton, testCastSFXButton;

   // Starting Name
   public string startingName;

   // List for toggle able tabs
   public List<TogglerClass> togglerList;

   #endregion

   private void Awake () {
      foreach (TogglerClass toggler in togglerList) {
         toggler.initListeners();
      }

      saveButton.onClick.AddListener(() => {
         ShipAbilityData itemData = getShipAbilityData();
         if (itemData != null) {
            if (itemData.abilityName != startingName) {
               toolManager.deleteDataFile(new ShipAbilityData { abilityName = startingName });
            }
            toolManager.saveXMLData(itemData);
            gameObject.SetActive(false);
         }
      });

      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         toolManager.loadXMLData();
      });

      _shipCastType.maxValue = Enum.GetValues(typeof(ShipAbilityData.ShipCastType)).Length - 1;
      _shipCastCollisionType.maxValue = Enum.GetValues(typeof(ShipAbilityData.ShipCastCollisionType)).Length - 1;
      _shipAbilityEffect.maxValue = Enum.GetValues(typeof(ShipAbilityData.ShipAbilityEffect)).Length - 1;
      _shipAbilityAttackType.maxValue = Enum.GetValues(typeof(Attack.Type)).Length - 1;
      _impactMagnitude.maxValue = Enum.GetValues(typeof(Attack.ImpactMagnitude)).Length - 1;

      _impactMagnitude.onValueChanged.AddListener(_ => {
         _impactMagnitudeText.text = ((Attack.ImpactMagnitude) _).ToString();
      });

      _shipCastType.onValueChanged.AddListener(_ => {
         _shipCastText.text = ((ShipAbilityData.ShipCastType) _).ToString();
      });

      _shipCastCollisionType.onValueChanged.AddListener(_ => {
         _shipCastCollisionText.text = ((ShipAbilityData.ShipCastCollisionType) _).ToString();
      });

      _shipAbilityEffect.onValueChanged.AddListener(_ => {
         _shipAbilityEffectText.text = ((ShipAbilityData.ShipAbilityEffect) _).ToString();
      });

      _shipAbilityAttackType.onValueChanged.AddListener(_ => {
         _shipAbilityAttackTypeText.text = ((Attack.Type) _).ToString();
      });

      _abilityPathButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.ShipAbilityIcon, _abilityPathIcon, _abilityPath);
      });
      _projectilePathButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.CannonSprites, _projectilePathIcon, _projectilePath);
      });
      _skillHitButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.ShipAbilityEffect, _skillHitIcon, _skillHitText);
      });
      _skillCastButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.ShipAbilityEffect, _skillCastIcon, _skillCastText);
      });

      _hitAudioButton.onClick.AddListener(() => {
         selectionPopup.toggleAudioSelection(_hitAudio, _hitAudioText);
      });
      _castAudioButton.onClick.AddListener(() => {
         selectionPopup.toggleAudioSelection(_castAudio, _castAudioText);
      });

      testHitSFXButton.onClick.AddListener(() => {
         if (_hitAudio != null) {
            audioSource.clip = _hitAudio;
            audioSource.Play();
         }
      });
      testCastSFXButton.onClick.AddListener(() => {
         if (_castAudio != null) {
            audioSource.clip = _castAudio;
            audioSource.Play();
         }
      });
   }

   public void loadData (ShipAbilityData shipAbilityData) {
      startingName = shipAbilityData.abilityName;
      _abilityName.text = shipAbilityData.abilityName;
      _abilityDescription.text = shipAbilityData.abilityDescription;

      // Loads all images
      _abilityPath.text = shipAbilityData.skillIconPath;
      _projectilePath.text = shipAbilityData.projectileSpritePath;
      _skillHitText.text = shipAbilityData.collisionSpritePath;
      _skillCastText.text = shipAbilityData.castSpritePath;

      if (shipAbilityData.skillIconPath != "") {
         _abilityPathIcon.sprite = ImageManager.getSprite(shipAbilityData.skillIconPath);
      }
      if (shipAbilityData.projectileSpritePath != "") {
         _projectilePathIcon.sprite = ImageManager.getSprite(shipAbilityData.projectileSpritePath);
      }
      if (shipAbilityData.collisionSpritePath != "") {
         _skillHitIcon.sprite = ImageManager.getSprite(shipAbilityData.collisionSpritePath);
      }
      if (shipAbilityData.castSpritePath != "") {
         _skillCastIcon.sprite = ImageManager.getSprite(shipAbilityData.castSpritePath);
      }

      // Loads all int and float data
      _abilityDamage.text = shipAbilityData.damage.ToString();
      _projectileSpeed.text = shipAbilityData.damage.ToString();
      _cooldown.text = shipAbilityData.coolDown.ToString();
      _fxPerFrame.text = shipAbilityData.abilitySpriteFXPerFrame.ToString();
      _levelRequirement.text = shipAbilityData.levelRequirement.ToString();
      _shipVariety.text = shipAbilityData.shipTypeVariety.ToString();

      // Loads all slider data
      _shipCastText.text = shipAbilityData.shipCastType.ToString();
      _shipCastCollisionText.text = shipAbilityData.shipCastCollisionType.ToString();
      _shipAbilityEffectText.text = shipAbilityData.shipAbilityEffect.ToString();
      _shipAbilityAttackTypeText.text = shipAbilityData.selectedAttackType.ToString();
      _impactMagnitudeText.text = shipAbilityData.impactMagnitude.ToString();

      _impactMagnitude.value = (int) shipAbilityData.impactMagnitude;
      _shipAbilityAttackType.value  = (int) shipAbilityData.selectedAttackType;
      _shipCastType.value = (int) shipAbilityData.shipCastType;
      _shipCastCollisionType.value = (int) shipAbilityData.shipCastCollisionType;
      _shipAbilityEffect.value = (int) shipAbilityData.shipAbilityEffect;

      _hitAudioText.text = shipAbilityData.collisionSFXPath;
      _castAudioText.text = shipAbilityData.castSFXPath;
      if (shipAbilityData.collisionSFXPath != "") {
         _hitAudio = AudioClipManager.self.getAudioClipData(shipAbilityData.collisionSFXPath).audioClip;
      }
      if (shipAbilityData.castSFXPath != "") {
         _castAudio = AudioClipManager.self.getAudioClipData(shipAbilityData.castSFXPath).audioClip;
      }
   }

   private ShipAbilityData getShipAbilityData () {
      ShipAbilityData abilityData = new ShipAbilityData();

      abilityData.abilityName = _abilityName.text;
      abilityData.abilityDescription = _abilityDescription.text;

      abilityData.shipCastType = (ShipAbilityData.ShipCastType) Enum.Parse(typeof(ShipAbilityData.ShipCastType), _shipCastText.text);
      abilityData.shipCastCollisionType = (ShipAbilityData.ShipCastCollisionType) Enum.Parse(typeof(ShipAbilityData.ShipCastCollisionType), _shipCastCollisionText.text);
      abilityData.shipAbilityEffect = (ShipAbilityData.ShipAbilityEffect) Enum.Parse(typeof(ShipAbilityData.ShipAbilityEffect), _shipAbilityEffectText.text);
      abilityData.selectedAttackType = (Attack.Type) Enum.Parse(typeof(Attack.Type), _shipAbilityAttackTypeText.text);
      abilityData.impactMagnitude = (Attack.ImpactMagnitude) Enum.Parse(typeof(Attack.ImpactMagnitude), _impactMagnitudeText.text);

      abilityData.shipTypeVariety = int.Parse(_shipVariety.text);
      abilityData.levelRequirement = int.Parse(_levelRequirement.text);
      abilityData.damage = int.Parse(_abilityDamage.text);

      abilityData.abilitySpriteFXPerFrame = float.Parse(_fxPerFrame.text);
      abilityData.coolDown = float.Parse(_cooldown.text);
      abilityData.projectileSpeed = float.Parse(_projectileSpeed.text);

      abilityData.skillIconPath = _abilityPath.text;
      abilityData.projectileSpritePath = _projectilePath.text;
      abilityData.castSpritePath = _skillCastText.text;
      abilityData.collisionSpritePath = _skillHitText.text;

      abilityData.castSFXPath = _castAudioText.text;
      abilityData.collisionSFXPath = _hitAudioText.text;

      return abilityData;
   }

   #region Private Variables
#pragma warning disable 0649
   [SerializeField]
   private InputField _abilityName, _abilityDescription;

   [SerializeField]
   private Slider _shipCastType;
   [SerializeField]
   private Text _shipCastText;

   [SerializeField]
   private Slider _shipAbilityEffect;
   [SerializeField]
   private Text _shipAbilityEffectText;

   [SerializeField]
   private Slider _shipAbilityAttackType;
   [SerializeField]
   private Text _shipAbilityAttackTypeText;

   [SerializeField]
   private Slider _shipCastCollisionType;
   [SerializeField]
   private Text _shipCastCollisionText;

   [SerializeField]
   private Slider _impactMagnitude;
   [SerializeField]
   private Text _impactMagnitudeText;

   [SerializeField]
   private InputField _shipVariety, _levelRequirement, _abilityDamage, _projectileSpeed, _fxPerFrame, _cooldown;

   [SerializeField]
   private Text _projectilePath;
   [SerializeField]
   private Button _projectilePathButton;
   [SerializeField]
   private Image _projectilePathIcon;

   [SerializeField]
   private Text _skillCastText;
   [SerializeField]
   private Button _skillCastButton;
   [SerializeField]
   private Image _skillCastIcon;

   [SerializeField]
   private Text _skillHitText;
   [SerializeField]
   private Button _skillHitButton;
   [SerializeField]
   private Image _skillHitIcon;

   [SerializeField]
   private Text _abilityPath;
   [SerializeField]
   private Button _abilityPathButton;
   [SerializeField]
   private Image _abilityPathIcon;

   [SerializeField]
   private AudioClip _castAudio;
   [SerializeField]
   private Button _castAudioButton;
   [SerializeField]
   private Text _castAudioText;

   [SerializeField]
   private AudioClip _hitAudio;
   [SerializeField]
   private Button _hitAudioButton;
   [SerializeField]
   private Text _hitAudioText;
#pragma warning restore 0649
   #endregion
}
