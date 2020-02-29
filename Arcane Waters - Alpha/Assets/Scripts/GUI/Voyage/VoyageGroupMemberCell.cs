using UnityEngine;
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

   // The frame used if the portrait is the local player's
   public Sprite localPlayerFrame;

   // The frame used if the portrait is not the local player's
   public Sprite nonLocalPlayerFrame;

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
         frameImage.sprite = nonLocalPlayerFrame;
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
         hpBar.enabled = false;
         return;
      }

      // Update the portrait background
      characterPortrait.updateBackground(entity);

      // Update the hp bar
      hpBar.enabled = true;
      hpBar.fillAmount = (float) entity.currentHealth / entity.maxHealth;
      hpBar.color = hpBarGradient.Evaluate(hpBar.fillAmount);
   }

   public void OnPointerEnter (PointerEventData eventData) {
      if (_active) {
         tooltipBox.SetActive(true);
      }
   }

   public void OnPointerExit (PointerEventData eventData) {
      if (_active) {
         tooltipBox.SetActive(false);
      }
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
      playerNameText.text = entity.entityName;
      playerLevelText.text = "LvL " + LevelUtil.levelForXp(entity.XP).ToString();

      // Set the portrait frame for local or non local entities
      if (entity.isLocalPlayer) {
         frameImage.sprite = localPlayerFrame;
      } else {
         frameImage.sprite = nonLocalPlayerFrame;
      }
   }

   #region Private Variables

   // The id of the displayed user
   private int _userId = -1;

   // Gets set to true when the cell is updating the group member info
   private bool _active = true;

   #endregion
}
