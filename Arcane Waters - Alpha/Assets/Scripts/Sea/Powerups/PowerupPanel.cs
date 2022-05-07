﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System.Linq;
using DG.Tweening;

public class PowerupPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   // Singleton instance
   public static PowerupPanel self;

   // A reference to the powerup icon prefab
   public GameObject powerupIconPrefab, landPowerupIconPrefab;

   // A list of colors associated with each rarity type
   public List<Color> rarityColors;

   // A reference to the canvas that this panel is in
   public Canvas parentCanvas;

   // A reference to the rect transform of the layout group that contains this panel
   public RectTransform containingLayoutGroup;

   // Reference to the container of this Powerup Panel
   public RectTransform powerupPanelContainer;

   #endregion

   private void Awake () {
      self = this;
      _layoutGroup = GetComponent<GridLayoutGroup>();
      _rectTransform = GetComponent<RectTransform>();
   }

   #region Land Powerup

   public void updateLandPowerups (List<LandPowerupData> powerups) {
      clearSeaPowerups();
      clearLandPowerups();
      transform.gameObject.DestroyChildren();

      foreach (LandPowerupData powerup in powerups) {
         addLandPowerup(powerup);
      }
   }

   public void clearLandPowerups () {
      for (int i = 0; i < _landPowerupIcons.Count; i++) {
         if (_landPowerupIcons[i] != null) {
            Destroy(_landPowerupIcons[i].gameObject);
         }
      }
      _landPowerupIcons.Clear();

      // Hide the panel container if there are no powerups
      powerupPanelContainer.gameObject.SetActive(false);
   }

   public void addLandPowerup (LandPowerupData powerupData) {
      if (Global.player is PlayerBodyEntity) {
         if (!gameObject.activeInHierarchy) {
            gameObject.SetActive(true);
         }

         LandPowerupIcon newPowerup = Instantiate(landPowerupIconPrefab, transform).GetComponent<LandPowerupIcon>();
         newPowerup.init(powerupData.landPowerupType, Rarity.Type.Common);
         _landPowerupIcons.Add(newPowerup);

         // When a new powerup icon is added, sort the list by rarity
         LandPowerupIcon[] orderedIcons = _landPowerupIcons.OrderBy(x => (int) x.rarity).ToArray();
         for (int i = 0; i < orderedIcons.Length; i++) {
            if (orderedIcons[i].transform.childCount >= i) {
               orderedIcons[i].transform.SetSiblingIndex(i);
            }
         }

         newPowerup.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);

         // Show the powerup container
         powerupPanelContainer.gameObject.SetActive(true);
      }
   }

   public void addItemBuff (Item item, bool isLand, string areaKey) {
      if (VoyageManager.isLeagueArea(areaKey) || WorldMapManager.isWorldMapArea(areaKey) || VoyageManager.isTreasureSiteArea(areaKey)) {
         if (!gameObject.activeInHierarchy) {
            gameObject.SetActive(true);
         }
         if (!powerupPanelContainer.gameObject.activeInHierarchy) {
            powerupPanelContainer.gameObject.SetActive(true);
         }
      }

      if (isLand) {
         LandPowerupIcon newPowerup = Instantiate(landPowerupIconPrefab, transform).GetComponent<LandPowerupIcon>();
         newPowerup.initializeItemPowerup(item);
         _landPowerupIcons.Add(newPowerup);
         newPowerup.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
      } else {
         PowerupIcon newPowerup = Instantiate(powerupIconPrefab, transform).GetComponent<PowerupIcon>();
         newPowerup.initializeItemPowerup(item);
         _powerupIcons.Add(newPowerup);
      }
   }

   public void removePowerup (LandPowerupData powerupData) {
      LandPowerupIcon powerupIcon = _landPowerupIcons.Find(_ => _.landPowerupType == powerupData.landPowerupType && _.rarity == Rarity.Type.Common);

      if (_landPowerupIcons.Count < 1 || transform.childCount < 1 || powerupIcon == null) {
         return;
      }

      _landPowerupIcons.RemoveAt(0);
      Destroy(transform.GetChild(0).gameObject);

      // When a new powerup icon is removed, sort the list by rarity
      LandPowerupIcon[] orderedIcons = _landPowerupIcons.OrderBy(x => (int) x.rarity).ToArray();
      for (int i = 0; i < orderedIcons.Length; i++) {
         orderedIcons[i].transform.SetSiblingIndex(i);
      }

      if (transform.childCount < 1) {
         powerupPanelContainer.gameObject.SetActive(false);
      }
   }

   #endregion

   #region Sea Powerup

   public void addPowerup (Powerup.Type type, Rarity.Type rarity, NetEntity netEntity) {
      if (netEntity == null) {
         return;
      }

      if (netEntity is ShipEntity) {
         PowerupIcon newPowerup = Instantiate(powerupIconPrefab, transform).GetComponent<PowerupIcon>();
         newPowerup.init(type, rarity);
         _powerupIcons.Add(newPowerup);

         newPowerup.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);

         // Update remaining icon when an icon is removed and sort by rarity
         updatePowerupIconSorting();
      }
   }

   public void removePowerup (Powerup.Type type, Rarity.Type rarity) {
      PowerupIcon powerupIcon = _powerupIcons.Find(_ => _.type == type && _.rarity == rarity);

      if (_powerupIcons.Count < 1 || transform.childCount < 1) {
         return;
      }

      _powerupIcons.Remove(powerupIcon);
      Destroy(powerupIcon.gameObject);

      // Update remaining icon when an icon is removed and sort by rarity
      updatePowerupIconSorting();
   }
   
   public void updatePowerupIconSorting() {
      // When a new powerup icon is added/removed, update the list by rarity
      PowerupIcon[] orderedIcons = _powerupIcons.OrderBy(x => (int) x.rarity).ToArray();
      for (int i = 0; i < orderedIcons.Length; i++) {
         orderedIcons[i].transform.SetSiblingIndex(i);
      }

      powerupPanelContainer.gameObject.SetActive(transform.childCount > 0);
   }

   public void clearSeaPowerups () {
      for (int i = 0; i < _powerupIcons.Count; i++) {
         if (_powerupIcons[i] != null) {
            Destroy(_powerupIcons[i].gameObject);
         }
      }
      _powerupIcons.Clear();

      // Hide the panel container if there are no powerups
      powerupPanelContainer.gameObject.SetActive(false);
   }

   public void updatePowerups (List<Powerup> powerups, NetEntity netEntity) {
      clearLandPowerups();
      clearSeaPowerups();
      transform.gameObject.DestroyChildren();

      foreach (Powerup powerup in powerups) {
         addPowerup(powerup.powerupType, powerup.powerupRarity, netEntity);
      }
   }

   #endregion

   private void Update () {
      float targetSpacing = (_isExpanded) ? EXPANDED_SPACING : COLLAPSED_SPACING;
      _layoutGroup.spacing = new Vector2(Mathf.Lerp(_layoutGroup.spacing.x, targetSpacing, Time.deltaTime * EXPAND_SPEED), _layoutGroup.spacing.y);
   }

   public void OnPointerEnter (PointerEventData eventData) {
      _isExpanded = true;
   }

   public void OnPointerExit (PointerEventData eventData) {
      _isExpanded = false;
   }

   public void rebuildLayoutGroup () {
      LayoutRebuilder.ForceRebuildLayoutImmediate(containingLayoutGroup);
   }

   public bool hasLandPowerup (LandPowerupType landPowerupType) {
      return _landPowerupIcons.FindAll(_ => _.landPowerupType == landPowerupType).Count > 0;
   }

   #region Private Variables

   // A reference to the layout group that holds the powerup icons
   private GridLayoutGroup _layoutGroup;

   // Whether the powerup panel is expanded
   private bool _isExpanded = false;

   // The spacing of the horizontal layout group when icons are collapsed
   private const float COLLAPSED_SPACING = -32.0f;

   // The spacing of the horizontal layout group when icons are expanded
   private const float EXPANDED_SPACING = 4.0f;

   // How fast the icons expand / collapse
   private const float EXPAND_SPEED = 10.0f;

   // A list of references to our current powerups
   [SerializeField]
   private List<PowerupIcon> _powerupIcons = new List<PowerupIcon>();

   // A list of references to our current powerups
   [SerializeField]
   private List<LandPowerupIcon> _landPowerupIcons = new List<LandPowerupIcon>();

   // A reference to the rect transform of this panel
   private RectTransform _rectTransform;

   #endregion
}
