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

   // The world position of this node
   [SyncVar]
   public Vector2 syncedPosition;

   // The number of times this ore node is interacted
   [SyncVar]
   public int interactCount;

   // Our sprite renderer
   public SpriteRenderer spriteRenderer;

   // The various sprites we use to display the status of this ore node
   [HideInInspector]
   public Sprite[] oreSprites;

   // The list of user IDs that have mined this node
   public SyncListInt userIds = new SyncListInt();

   // List of arrows that indicate where the player is facing
   public List<DirectionalArrow> directionalArrow;

   // Reference to the ore pickup objects
   public Dictionary<int, OrePickup> orePickupCollection = new Dictionary<int, OrePickup>();

   // The total interact count for each ore node
   public static int MAX_INTERACT_COUNT = 2;

   #endregion

   public void Awake () {
      // Look up components
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
   }

   private void Start () {
      // Load the sprites based on our type
      oreSprites = ImageManager.getSprites("Mining/" + this.oreType);
      spriteRenderer.enabled = true;

      transform.position = syncedPosition;

      // We don't need to do anything more when we're running in batch mode
      if (Application.isBatchMode) {
         return;
      }

      // Update the sprite shown if we've already mined this node
      if (hasBeenMined()) {
         spriteRenderer.sprite = oreSprites.Last();
      } else {
         spriteRenderer.sprite = oreSprites.First();
      }

      OreManager.self.registerOreNode(this.id, this);
   }

   public void updateSprite (int spriteId) {
      spriteRenderer.sprite = oreSprites[spriteId];
   }

   public void Update () {
      // Figure out whether our outline should be showing
      handleSpriteOutline();
   }

   public void tryToMineNodeOnClient () {
      if (hasBeenMined()) {
         return;
      }

      // Increment our current sprite index
      spriteRenderer.sprite = oreSprites[getNextSpriteIndex()];
   }

   public bool finishedMining () {
      return interactCount >= MAX_INTERACT_COUNT;
   }

   public void handleSpriteOutline () {
      if (_outline == null) {
         return;
      }

      // Only show our outline when the mouse is over us
      _outline.setVisibility(MouseManager.self.isHoveringOver(_clickableBox));
   }

   public bool hasBeenMined () {
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

   #region Private Variables

   // Our various components
   protected SpriteOutline _outline;
   protected ClickableBox _clickableBox;

   #endregion
}