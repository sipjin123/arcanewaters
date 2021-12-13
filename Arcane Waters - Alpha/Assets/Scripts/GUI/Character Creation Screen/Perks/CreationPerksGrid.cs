﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Linq;
using System;

public class CreationPerksGrid : MonoBehaviour {
   #region Public Variables

   // The number of points that can be assigned during creation
   public const int AVAILABLE_POINTS = 3;

   // Self
   public static CreationPerksGrid self;

   // The prefab of perk icons
   public GameObject perksPrefab;

   // The transform holding the perks prefab
   public Transform perkPrefabHolder;

   #endregion

   private void Awake () {
      self = this;
   }

   public void initialize () {
      self = this;

      _assignedPerkPoints = new Dictionary<int, int>();
      _icons.ForEach(icon => icon.setAssignedPoints(0));
      _availablePoints = AVAILABLE_POINTS;
      _availablePointsText.text = _availablePoints.ToString();

      initializeIcons();
   }

   private void OnEnable () {
      if (TooltipManager.self != null) {
         TooltipManager.self.isAutomaticTooltipEnabled = false;
      }
   }

   private void OnDisable () {
      TooltipManager.self.isAutomaticTooltipEnabled = true;
   }

   public void initializeIcons () {
      _icons = new List<CreationPerkIcon>();
      perkPrefabHolder.gameObject.DestroyChildren();
      foreach (KeyValuePair<int, Perk.Category> pieceType in PerkManager.self.getPerkCategories()) {
         CreationPerkIcon newPerkIcon = Instantiate(perksPrefab, perkPrefabHolder).GetComponent<CreationPerkIcon>();
         newPerkIcon.perkId = (int) pieceType.Key;
         PerkData data = PerkManager.self.getPerkData(newPerkIcon.perkId);
         if (data == null) {
            D.editorLog("Failed to get data: {" + pieceType + "} {" + newPerkIcon.perkId + "}", Color.red);
         } else {
            newPerkIcon.initialize(data);
         }
      }
   }

   public bool hasAvailablePoints () {
      return _availablePoints > 0;
   }

   public void assignPoint (CreationPerkIcon icon) {
      int perkId = icon.perkId;

      if (hasAvailablePoints()) {
         if (_assignedPerkPoints.ContainsKey(perkId)) {
            _assignedPerkPoints[perkId]++;
         } else {
            _assignedPerkPoints.Add(perkId, 1);
         }

         _availablePoints--;
         _availablePointsText.text = _availablePoints.ToString();
         icon.setAssignedPoints(_assignedPerkPoints[perkId]);

         SoundEffectManager.self.playFmodSfx(SoundEffectManager.ASSIGN_PERK_POINT);
      } else {
         SoundEffectManager.self.playGuiButtonConfirmSfx();
      }
   }

   public void unassignPoint (CreationPerkIcon icon) {
      int perkId = icon.perkId;

      if (_assignedPerkPoints.ContainsKey(perkId) && _assignedPerkPoints[perkId] > 0) {
         _assignedPerkPoints[perkId]--;
         _availablePoints++;
         _availablePointsText.text = _availablePoints.ToString();
         icon.setAssignedPoints(_assignedPerkPoints[perkId]);

         SoundEffectManager.self.playFmodSfx(SoundEffectManager.UNASSIGN_PERK_POINT);
      } else {
         SoundEffectManager.self.playGuiButtonConfirmSfx();
         //SoundManager.play2DClip(SoundManager.Type.GUI_Press);
         //SoundEffectManager.self.playFmod2DWithPath(SoundEffectManager.BUTTON_CONFIRM_PATH);
      }
   }

   public List<Perk> getAssignedPoints () {
      List<Perk> perks = new List<Perk>();

      foreach (int perkId in _assignedPerkPoints.Keys) {
         perks.Add(new Perk(perkId, _assignedPerkPoints[perkId]));
      }

      return perks;
   }

   public Sprite getBorderForLevel (int level) {
      int index = level > 0 ? level - 1 : level;
      return _perkIconBorders[index];
   }

   public void onResetPointsClicked () {
      PanelManager.self.showConfirmationPanel("Do you want to reset all your points?", () => initialize());
   }

   #region Private Variables

   // The perk icon sprite borders
   [SerializeField]
   private List<Sprite> _perkIconBorders;

   // The perk icons
   [SerializeField]
   private List<CreationPerkIcon> _icons;

   // The available points text
   [SerializeField]
   private Text _availablePointsText;

   // The available points   
   private int _availablePoints = 3;

   // The assigned points
   private Dictionary<int, int> _assignedPerkPoints = new Dictionary<int, int>();
   
   #endregion
}
