using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class SecretsNode : NetworkBehaviour {
   #region Public Variables

   // The id of this node
   public int secretsId;

   // Sprite appropriate for the current state of this node
   public Sprite initSprite, interactSprite;

   // The current sprite renderer
   public SpriteRenderer spriteRenderer;

   // The instance that this node is in
   [SyncVar]
   public int instanceId;

   // The world position of this node
   [SyncVar]
   public Vector2 syncedPosition;

   // The position the sprite should be set after interacting
   public Transform interactPosition;

   // The area for this warp
   public string areaTarget;

   // The spawn for this warp
   public string spawnTarget;

   // The facing direction we should have after spawning
   public Direction newFacingDirection = Direction.South;

   // The number of user's inside the secret area
   public SyncListInt userIds = new SyncListInt();

   // The area key assigned to this node
   [SyncVar]
   public string areaKey;

   // If this object is being used in the map editor
   public bool isMapEditorMode;

   // The sprite paths of this node
   [SyncVar]
   public string initSpritePath, interactSpritePath;

   // Max players that can enter the secret room
   public static int MAX_PLAYERS = 2;

   // The reference to the simple animation
   public SimpleAnimation simpleAnim;

   // Determines if the object is interacted
   public bool isInteracted = false;

   #endregion

   private void Awake () {
      // Look up components
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
   }

   private void Start () {
      if (isMapEditorMode) {
         return;
      }
      
      initSprite = ImageManager.getSprite(initSpritePath);
      interactSprite = ImageManager.getSprites(interactSpritePath)[0];
      spriteRenderer.sprite = initSprite;

      spriteRenderer.enabled = true;
      _outline.Regenerate();
      _outline.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;

      transform.position = syncedPosition;
      if (AreaManager.self.getArea(areaKey) != null) {
         transform.SetParent(AreaManager.self.getArea(areaKey).secretsParent);
      }
   }

   public void Update () {
      if (isMapEditorMode) {
         return;
      }

      // Figure out whether our outline should be showing
      handleSpriteOutline();
   }

   public void handleSpriteOutline () {
      if (_outline == null) {
         return;
      }

      // Only show our outline when the mouse is over us
      bool isHovering = MouseManager.self.isHoveringOver(_clickableBox);
      _outline.setVisibility(isHovering);
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         string value = field.v.Split(':')[0];
         switch (field.k.ToLower()) {
            case DataField.SECRETS_TYPE_ID:
               secretsId = int.Parse(value);
               break;
            case DataField.SECRETS_START_SPRITE:
               initSprite = ImageManager.getSprite(value);
               spriteRenderer.sprite = initSprite;
               initSpritePath = value;
               break;
            case DataField.SECRETS_INTERACT_SPRITE:
               interactSprite = ImageManager.getSprites(value)[0];
               interactSpritePath = value;
               break;
            case DataField.WARP_TARGET_MAP_KEY:
               string areaName = AreaManager.self.getAreaName(int.Parse(value));
               areaTarget = areaName;
               break;
            case DataField.WARP_TARGET_SPAWN_KEY:
               spawnTarget = value;
               break;
            case DataField.WARP_ARRIVE_FACING_KEY:
               if (field.tryGetDirectionValue(out Direction dir)) {
                  newFacingDirection = dir;
               }
               break;
            default:
               Debug.LogWarning($"Unrecognized data field key: {field.k}");
               break;
         }
      }
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      if (collision.GetComponent<PlayerBodyEntity>() != null) {
         PlayerBodyEntity entity = collision.GetComponent<PlayerBodyEntity>();
         if (entity.isServer && entity.connectionToClient != null) {
            if (!userIds.Contains(entity.userId) && userIds.Count < MAX_PLAYERS) {
               completeInteraction(entity);
            } else {
               if (userIds.Contains(entity.userId)) {
                  D.editorLog("Already interacted", Color.red);
               }

               if (userIds.Count >= MAX_PLAYERS) {
                  D.editorLog("Reached max players", Color.red);
               }
            }
         }
      }
   }

   public void tryToInteract () {
      if (Global.player != null) {
         if (!userIds.Contains(Global.player.userId) && userIds.Count < MAX_PLAYERS) {
            completeInteraction((PlayerBodyEntity) Global.player);
         } else {
            if (userIds.Contains(Global.player.userId)) {
               D.editorLog("Already interacted", Color.red);
            }

            if (userIds.Count >= MAX_PLAYERS) {
               D.editorLog("Reached max players", Color.red);
            }
         }
      }
   }

   private void completeInteraction (PlayerBodyEntity player) {
      if (!isInteracted) {
         isInteracted = true;
         spriteRenderer.sprite = interactSprite;
         simpleAnim.enabled = true;
         simpleAnim.resetAnimation();
         Rpc_InteractAnimation();
      }
      StartCoroutine(CO_ProcessInteraction(player));
   }

   [ClientRpc]
   public void Rpc_InteractAnimation () {
      spriteRenderer.sprite = interactSprite;
      simpleAnim.enabled = true;
      simpleAnim.resetAnimation();
   }

   private IEnumerator CO_ProcessInteraction (PlayerBodyEntity player) {
      yield return new WaitForSeconds(1.5f);
      spriteRenderer.transform.position = interactPosition.position;
      warpToSecret(player);
      isInteracted = false;
   }

   private void warpToSecret (PlayerBodyEntity newPlayer) {
      PlayerBodyEntity player = newPlayer;

      if (player == null || areaTarget.Length < 1 || spawnTarget.Length < 1) {
         return;
      }

      // If a player entered this warp on the server, move them
      SpawnID spawnID = new SpawnID(areaTarget, spawnTarget);
      Vector2 localPos = SpawnManager.self.getSpawnLocalPosition(spawnID);

      player.spawnInNewMap(areaTarget, localPos, newFacingDirection);
      userIds.Add(player.userId);

      // Keep track of the user's location while in the secrets room
      SecretsGameManager.self.enterUserToSecret(player.userId, areaTarget, player.instanceId, this);
   }

   #region Private Variables

   // Our various components
   protected SpriteOutline _outline;
   protected ClickableBox _clickableBox;

   #endregion
}