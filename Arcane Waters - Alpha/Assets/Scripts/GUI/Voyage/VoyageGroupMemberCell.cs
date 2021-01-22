﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using ProceduralMap;
using Mirror;
using UnityEngine.EventSystems;

public class VoyageGroupMemberCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   #region Public Variables

   // The character portrait
   public CharacterPortrait characterPortrait;

   // The frame image
   public Image frameImage;

   // The hp bar
   public Image hpBar;

   // The colors of the hp bar
   public Gradient hpBarGradient;

   // The tooltip container
   public GameObject tooltipBox;

   // The name of the player
   public Text playerNameText;

   // The level of the player
   public Text playerLevelText;

   #endregion

   public void Awake () {
      // Disable the hp bar
      hpBar.enabled = false;

      // Hide the tooltip
      tooltipBox.SetActive(false);
   }

   public void setCellForGroupMember (int userId) {
      _userId = userId;

      // Find the NetEntity of the displayed user
      NetEntity entity = EntityManager.self.getEntity(_userId);

      // Check if the entity is visible by this client
      if (entity == null) {
         _active = false;

         // Initialize the portrait with a question mark
         characterPortrait.initialize(entity);         
      } else {
         _active = true;

         // Set the portrait when the entity is initialized
         StartCoroutine(CO_InitializePortrait(entity));
      }
   }

   public void Update () {
      if (Global.player == null || !Global.player.isLocalPlayer || !VoyageGroupManager.isInGroup(Global.player) ||
         !_active) {
         return;
      }

      // Try to find the entity of the displayed user
      NetEntity entity = EntityManager.self.getEntity(_userId);
      if (entity == null) {
         hpBar.enabled = false;
         return;
      }

      // Update the portrait background
      characterPortrait.updateBackground(entity);

      // Allow right clicking to bring up the context menu, only if no panel is opened
      if (InputManager.isLeftClickKeyPressed() && _mouseOver && !PanelManager.self.hasPanelInLinkedList()) {
         PanelManager.self.contextMenuPanel.showDefaultMenuForUser(_userId, entity.entityName);
      }

      int currentHP = entity.currentHealth;
      int maxHP = entity.maxHealth;

      // If the user is in battle, get the battler hp values
      if (entity.isInBattle()) {
         Battler battler = BattleManager.self.getBattler(_userId);
         if (battler != null) {
            currentHP = battler.displayedHealth;
            maxHP = battler.getStartingHealth();
         }
      }

      // Update the hp bar
      hpBar.enabled = true;
      hpBar.fillAmount = (float) currentHP / maxHP;
      hpBar.color = hpBarGradient.Evaluate(hpBar.fillAmount);
   }

   public void OnPointerEnter (PointerEventData eventData) {
      if (_active) {
         tooltipBox.SetActive(true);
      }
      _mouseOver = true;
   }

   public void OnPointerExit (PointerEventData eventData) {
      if (_active) {
         tooltipBox.SetActive(false);
      }
      _mouseOver = false;
   }

   public int getUserId () {
      return _userId;
   }

   public bool isMouseOver () {
      return _mouseOver;
   }

   private IEnumerator CO_InitializePortrait (NetEntity entity) {
      // Wait until the entity has received its initialization data
      while (Util.isEmpty(entity.entityName)) {
         yield return null;
      }

      characterPortrait.initialize(entity);
      playerNameText.text = entity.entityName;
      playerLevelText.text = "LvL " + LevelUtil.levelForXp(entity.XP).ToString();
   }

   #region Private Variables

   // The id of the displayed user
   private int _userId = -1;

   // Gets set to true when the cell is updating the group member info
   private bool _active = true;

   // Gets set to true when the mouse is hovering the cell
   private bool _mouseOver = false;

   #endregion
}
