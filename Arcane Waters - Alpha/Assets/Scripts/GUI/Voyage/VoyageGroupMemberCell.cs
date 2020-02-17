﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using ProceduralMap;
using Mirror;
using UnityEngine.EventSystems;

public class VoyageGroupMemberCell : MonoBehaviour
{
   #region Public Variables

   // The character portrait
   public CharacterPortrait characterPortrait;

   // The hp circle
   public Image hpCircle;

   // The hp circle background
   public Image hpCircleBackground;

   // The colors of the hp circle
   public Color normalHpColor;
   public Color lowHpColor;

   #endregion

   public void Awake () {
      // Set the portrait pointer events
      characterPortrait.pointerEnterEvent.RemoveAllListeners();
      characterPortrait.pointerExitEvent.RemoveAllListeners();
      characterPortrait.pointerEnterEvent.AddListener(() => onPointerEnterPortrait());
      characterPortrait.pointerExitEvent.AddListener(() => onPointerExitPortrait());

      // Disable the hp circle
      hpCircle.enabled = false;
      hpCircleBackground.enabled = false;
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
      if (Global.player == null || !Global.player.isLocalPlayer || !VoyageManager.isInVoyage(Global.player) ||
         !_active) {
         return;
      }

      // Try to find the entity of the displayed user
      NetEntity entity = EntityManager.self.getEntity(_userId);
      if (entity == null) {
         hpCircle.enabled = false;
         hpCircleBackground.enabled = false;
         return;
      }

      // If the user is in combat, display his hp
      if (entity.hasAnyCombat()) {
         hpCircle.enabled = true;
         hpCircleBackground.enabled = true;
         hpCircle.fillAmount = (float) entity.currentHealth / entity.maxHealth;

         // Set the color of the hp indicator
         if (hpCircle.fillAmount > 0.25f) {
            hpCircle.color = normalHpColor;
         } else {
            hpCircle.color = lowHpColor;
         }
      } else {
         hpCircle.enabled = false;
         hpCircleBackground.enabled = false;
      }
   }

   public void onPointerEnterPortrait () {
   }

   public void onPointerExitPortrait () {
   }

   public int getUserId () {
      return _userId;
   }

   private IEnumerator CO_InitializePortrait (NetEntity entity) {
      // Wait until the entity has received its initialization data
      while (Util.isEmpty(entity.entityName)) {
         yield return null;
      }

      characterPortrait.initialize(entity);
   }

   #region Private Variables

   // The id of the displayed user
   private int _userId = -1;

   // Gets set to true when the cell is updating the group member info
   private bool _active = true;

   #endregion
}