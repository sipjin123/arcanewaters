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

   // Our sprite renderer
   public SpriteRenderer spriteRenderer;

   // The various sprites we use to display the status of this ore node
   [HideInInspector]
   public Sprite[] oreSprites;

   // The list of user IDs that have mined this node
   public SyncListInt userIds = new SyncListInt();

   // List of arrows that indicate where the player is facing
   public List<DirectionalArrow> directionalArrow;

   #endregion

   public void Awake () {
      // Look up components
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
   }

   private void Start () {
      // Load the sprites based on our type
      oreSprites = ImageManager.getSprites("Mining/" + this.oreType);

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
   }

   public void Update () {
      // Figure out whether our outline should be showing
      handleSpriteOutline();

      // Allow pressing keyboard to mine the ore node
      if (InputManager.isActionKeyPressed() && !hasBeenMined() && isGlobalPlayerNearby()) {
         tryToMineNodeOnClient();
      }
   }

   public void tryToMineNodeOnClient () {
      if (hasBeenMined()) {
         return;
      }

      // The player has to be close enough
      if (!isGlobalPlayerNearby()) {
         Instantiate(PrefabsManager.self.tooFarPrefab, this.transform.position + new Vector3(0f, .24f), Quaternion.identity);
         return;
      }

      // Increment our current sprite index
      spriteRenderer.sprite = oreSprites[getNextSpriteIndex()];

      // If we finished mining the node, send a message to the server
      if (spriteRenderer.sprite == oreSprites.Last()) {
         Global.player.rpc.Cmd_MineNode(this.id);
      }
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

   private void setArrowDirection(Direction direction) {
      disableArrows();
      directionalArrow.Find(_ => _.direction == direction).gameObj.SetActive(true);
   }

   private void disableArrows() {
      for (int i = 0; i < directionalArrow.Count; i++) {
         directionalArrow[i].gameObj.SetActive(false);
      }
   }

   private void OnTriggerStay2D (Collider2D collision) {
      if (collision.GetComponent<PlayerObserverManager>() != null) {
         if(Global.player == collision.GetComponent<NetEntity>()) {
            Vector2 pos = Global.player.transform.position;
            if (pos.x < transform.position.x) {
               setArrowDirection(Direction.West);
            } else {
               setArrowDirection(Direction.East);
            }
         }
      }
   }

   private void OnTriggerExit2D (Collider2D collision) {
      if (collision.GetComponent<PlayerObserverManager>() != null) {
         if (Global.player == collision.GetComponent<NetEntity>()) {
            disableArrows();
         }
      }
   }

   #region Private Variables

   // Our various components
   protected SpriteOutline _outline;
   protected ClickableBox _clickableBox;

   #endregion
}