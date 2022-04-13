using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using NubisDataHandling;

public class CraftingAnvil : MonoBehaviour
{
   #region Public Variables

   // Our sprite renderer
   public SpriteRenderer spriteRenderer;

   // Our box collider
   public BoxCollider2D boxCollider;

   // Our trigger collider for the Open button
   public CircleCollider2D triggerCollider;

   // The container for our animated arrow
   public GameObject arrowContainer;

   #endregion

   private void Awake () {
      // Look up components
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();

      // Enable the colliders
      triggerCollider.enabled = true;
      boxCollider.enabled = true;
   }

   public void Update () {
      // Activate certain things when the global player is nearby
      arrowContainer.SetActive(_isGlobalPlayerNearby);

      // Figure out whether our outline should be showing
      handleSpriteOutline();

      // Allow pressing keyboard to open the crafting panel
      if (_isGlobalPlayerNearby && InputManager.self.inputMaster.UIControl.Equip.WasPressedThisFrame()) {
         openCraftingPanel();
      }
   }

   public void openCraftingPanel () {
      // The player has to be close enough
      if (!_isGlobalPlayerNearby) {
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .24f)).asTooFar();
         return;
      }

      NubisDataFetcher.self.fetchCraftableData(0, CraftingPanel.ROWS_PER_PAGE, CraftingPanel.craftingCategoryList);
   }

   public void handleSpriteOutline () {
      if (_outline == null) {
         return;
      }

      // Only show our outline when the mouse is over us
      if (_outline && _clickableBox && MouseManager.self) {
         _outline.setVisibility(MouseManager.self.isHoveringOver(_clickableBox));
      }
   }

   private void OnTriggerStay2D (Collider2D other) {
      NetEntity entity = other.GetComponent<NetEntity>();

      // If our player enters the trigger, we show the GUI
      if (entity != null && Global.player != null && entity.userId == Global.player.userId) {
         _isGlobalPlayerNearby = true;
      }
   }

   private void OnTriggerExit2D (Collider2D other) {
      NetEntity entity = other.GetComponent<NetEntity>();

      // If our player exits the trigger, we hide the GUI
      if (entity != null && Global.player != null && entity.userId == Global.player.userId) {
         _isGlobalPlayerNearby = false;
      }
   }

   #region Private Variables

   // Gets set to true when the global player is nearby
   protected bool _isGlobalPlayerNearby;

   // Our various components
   protected SpriteOutline _outline;
   protected ClickableBox _clickableBox;

   #endregion
}