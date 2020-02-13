using UnityEngine;
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

      // Set the portrait
      characterPortrait.setPortrait(entity);

      // Check if the entity is visible by this client
      if (entity == null) {
         _active = false;
      } else {
         _active = true;
      }
   }

   public void Update () {
      if (Global.player == null || !Global.player.isLocalPlayer || Global.player.voyageGroupId == -1 ||
         !_active) {
         return;
      }

      // Try to find the SeaEntity of the displayed user
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

   #region Private Variables

   // The id of the displayed user
   private int _userId = -1;

   // Gets set to true when the cell is updating the group member info
   private bool _active = true;

   #endregion
}
