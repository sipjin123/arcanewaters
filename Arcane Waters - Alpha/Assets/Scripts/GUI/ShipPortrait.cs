using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using ProceduralMap;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ShipPortrait : MonoBehaviour
{
   #region Public Variables

   // Our Canvas Group
   public CanvasGroup canvasGroup;

   // The character portrait
   public CharacterPortrait portrait;

   // The frame image
   public Image frameImage;

   // The frame used if the portrait is the local player's
   public Sprite localPlayerFrame;

   // The frame used if the portrait is not the local player's
   public Sprite nonLocalPlayerFrame;

   #endregion

   void Awake () {
      // Look up components
      _entity = GetComponentInParent<SeaEntity>();

      // Start hidden
      hide();
   }

   void Start () {
      StartCoroutine(CO_InitializePortrait());
   }

   void Update () {
      // Only show the user portrait when the mouse is over the entity or its member cell in the group panel
      if (!_entity.isDead() && (_entity.isMouseOver() || _entity.isAttackCursorOver() || VoyageGroupPanel.self.isMouseOverMemberCell(_entity.userId))) {
         show();
         portrait.updateBackground(_entity);
      } else {
         hide();
      }
   }

   public void show () {
      this.canvasGroup.alpha = 1f;
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
   }

   private IEnumerator CO_InitializePortrait () {
      // Wait until the entity has been initialized
      while (Util.isEmpty(_entity.entityName)) {
         yield return null;
      }

      portrait.initialize(_entity);

      // Set the portrait frame for local or non local entities
      if (_entity.isLocalPlayer) {
         frameImage.sprite = localPlayerFrame;
      } else {
         frameImage.sprite = nonLocalPlayerFrame;
      }
   }

   #region Private Variables

   // Our associated Sea Entity
   protected SeaEntity _entity;

   #endregion
}
