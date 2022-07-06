using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;

public class OreNode : NetworkBehaviour
{
   #region Public Variables

   // The Type of ore
   public enum Type {  None = 0, Iron = 1, Silver = 2, Gold = 3 }

   // The Type of ore
   [SyncVar]
   public Type oreType;

   // The unique ID assigned to this node
   [SyncVar]
   public int id;

   // The instance that this node is in
   [SyncVar]
   public int instanceId;

   // The number of times this ore node is interacted
   public int interactCount = 0;

   // The area key assigned to this ore
   [SyncVar]
   public string areaKey;

   // Our sprite renderer
   public SpriteRenderer spriteRenderer;

   // The various sprites we use to display the status of this ore node
   [HideInInspector]
   public Sprite[] oreSprites;

   // The list of user IDs that have mined this node
   public SyncList<int> userIds = new SyncList<int>();

   // List of arrows that indicate where the player is facing
   public List<DirectionalArrow> directionalArrow;

   // Reference to the ore pickup objects
   public Dictionary<int, OrePickup> orePickupCollection = new Dictionary<int, OrePickup>();

   // The total interact count for each ore node
   public static int MAX_INTERACT_COUNT = 2;

   // Determines the type of map this ore node belongs to
   [SyncVar]
   public Area.SpecialType mapSpecialType;

   // The group instance id
   [SyncVar]
   public int groupInstanceId = -1;

   // The duration to wait before refreshing this node
   [SyncVar]
   public double refreshTimer = 15;

   // If is disabled
   [SyncVar]
   public bool isDisabledByController;

   #endregion

   public void Awake () {
      // Look up components
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
   }

   private void Start () {
      // Make the node a child of the Area
      StartCoroutine(CO_SetAreaParent());

      // Load the sprites based on our type
      oreSprites = ImageManager.getSprites("Mining/" + this.oreType);
      spriteRenderer.enabled = true;

      // We don't need to do anything more when we're running in batch mode
      if (Util.isBatch()) {
         return;
      }

      // Update the sprite shown if we've already mined this node
      if (hasBeenMinedByUser() || finishedMining()) {
         spriteRenderer.sprite = oreSprites.Last();
         _outline.setVisibility(false);
         _clickableBox.enabled = false;
      } else {
         spriteRenderer.sprite = oreSprites.First();
      }

      if (!NetworkServer.active) {
         OreManager.self.registerOreNode(id, this);
      }

      if (isDisabledByController) {
         disableThisNode();
      }
   }

   public void updateSprite (int spriteId) {
      if (spriteId < oreSprites.Length) {
         spriteRenderer.sprite = oreSprites[spriteId];
      } else {
         if (oreSprites.Length > 0) {
            spriteRenderer.sprite = oreSprites[oreSprites.Length - 1];
         }
      }

      if (hasBeenMinedByUser() || finishedMining()) {
         _outline.setVisibility(false);
         _clickableBox.enabled = false;
      }
   }

   public void Update () {
      // Figure out whether our outline should be showing
      handleSpriteOutline();

      if (isDisabledByController && !finishedMining()) {
         disableThisNode();
      }
   }

   public void tryToMineNodeOnClient () {
      if (hasBeenMinedByUser()) {
         return;
      }

      // Increment our current sprite index
      spriteRenderer.sprite = oreSprites[getNextSpriteIndex()];
   }

   public void incrementInteractCount () {
      interactCount++;
   }

   public bool finishedMining () {
      return interactCount >= MAX_INTERACT_COUNT;
   }

   public void startResetTimer () {
      Invoke(nameof(resetSettings), (float) refreshTimer);
   }

   public void disableThisNode () {
      interactCount = MAX_INTERACT_COUNT;
      spriteRenderer.sprite = oreSprites[MAX_INTERACT_COUNT];

   }

   public void resetSettings () {
      if (isDisabledByController) {
         disableThisNode();
      } else {
         interactCount = 0;
         spriteRenderer.sprite = oreSprites[0];

         _outline.setVisibility(true);
         _clickableBox.enabled = true;
      }
   }

   public void handleSpriteOutline () {
      if (_outline == null) {
         return;
      }

      // Only show our outline when the mouse is over us
      if (_outline.isActiveAndEnabled) {
         _outline.setVisibility(MouseManager.self.isHoveringOver(_clickableBox));
      } 
   }

   public bool hasBeenMinedByUser () {
      if (Global.player == null) {
         return false;
      }

      return userIds.Contains(Global.player.userId);
   }

   public bool isGlobalPlayerNearby () {
      if (Global.player == null) {
         return false;
      }

      return (Vector2.Distance(Global.player.transform.position, this.transform.position) <= .24f);
   }

   public void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.oreNodeParent, worldPositionStays);
   }

   protected int getNextSpriteIndex () {
      // Find the next index in the ore sprites array
      for (int i = 0; i < oreSprites.Length-1; i++) {
         if (spriteRenderer.sprite == oreSprites[i]) {
            return i + 1;
         }
      }

      // We're at the last index of our ore sprites
      return oreSprites.Length - 1;
   }

   private IEnumerator CO_SetAreaParent () {
      // Wait until we have finished instantiating the area
      while (AreaManager.self.getArea(areaKey) == null) {
         yield return 0;
      }

      // Set as a child of the area
      Area area = AreaManager.self.getArea(this.areaKey);
      bool worldPositionStays = area.cameraBounds.bounds.Contains((Vector2) transform.position);
      setAreaParent(area, worldPositionStays);
   }

   #region Private Variables

   // Our various components
   protected SpriteOutline _outline;
   protected ClickableBox _clickableBox;

   #endregion
}