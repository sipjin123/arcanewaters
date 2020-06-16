using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class SecretEntrance : NetworkBehaviour, IMapEditorDataReceiver {
   #region Public Variables

   // The id of this node
   public int secretsId;

   // Sprite appropriate for the current state of this node
   public Sprite mainSprite, subSprite;

   // The current sprite renderer
   public SpriteRenderer spriteRenderer, subSpriteRenderer;

   // The instance that this node is in
   [SyncVar]
   public int instanceId;

   // The unique id for each secret entrance per instance id
   [SyncVar]
   public int spawnId;

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

   // Determines if the object is interacted
   [SyncVar]
   public bool isInteracted = false;

   // The warp associated with this secret entrance
   public Warp warp;

   // The text where the warp will lead to
   public Text warpAreaText;

   // The animation speed of the secret entrance
   public float animationSpeed = .1f;

   // The sprite to be animated
   public SecretObjectSpriteData mainSpriteComponent = new SecretObjectSpriteData();
   public SecretObjectSpriteData subSpriteComponent = new SecretObjectSpriteData();

   // The base number of animation sprites in a sheet
   public static int DEFAULT_SPRITESHEET_COUNT = 24;

   // If the animation is finished
   [SyncVar]
   public bool isFinishedAnimating;

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

      // Make the node a child of the Area
      StartCoroutine(CO_SetAreaParent());

      if (!Util.isBatch()) {
         try {
            mainSprite = ImageManager.getSprite(initSpritePath);
            subSprite = ImageManager.getSprites(interactSpritePath)[0];
            if (!isInteracted) {
               spriteRenderer.sprite = mainSprite;
            } else {
               int spriteLength = ImageManager.getSprites(interactSpritePath).Length;
               Sprite[] mainSprites = ImageManager.getSprites(interactSpritePath);
               spriteRenderer.sprite = mainSprites[spriteLength - 1];

               // Multiplies the animation speed depending on the number of sprites in the sprite sheet (the more the sprites the faster the animation)
               animationSpeed *= mainSprites.Length / DEFAULT_SPRITESHEET_COUNT;
               animationSpeed = Mathf.Clamp(animationSpeed, .05f, 2);
            }

            if (subSprite.name == "none") {
               subSpriteRenderer.gameObject.SetActive(false);
            }
            spriteRenderer.enabled = true;

            _outline.Regenerate();
            _outline.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
         } catch {
            D.debug("Failed to process sprites for Secret Entrance");
         }
      }

      // Server will set the warp info
      if (NetworkServer.active) {
         warp.areaTarget = areaTarget;
         warp.spawnTarget = spawnTarget;
         warp.newFacingDirection = newFacingDirection;
         warp.warpEvent.AddListener(player => {
            userIds.Add(player.userId);

            // Keep track of the user's location while in the secrets room
            SecretsManager.self.enterUserToSecret(player.userId, areaTarget, player.instanceId, this);
         });
         warp.gameObject.SetActive(false);
      }

      if (isFinishedAnimating && !Util.isBatch()) {
         setSprites();
         if (subSprite.name == "none") {
            subSpriteRenderer.gameObject.SetActive(false);
         }

         _outline.setVisibility(false);
         spriteRenderer.sprite = mainSpriteComponent.sprites[mainSpriteComponent.maxIndex / 2];
         subSpriteRenderer.sprite = subSpriteComponent.sprites[subSpriteComponent.maxIndex / 2];
      }

      SecretsManager.self.registerSecretEntrance(new SecretEntranceSpawnData {
         instanceId = instanceId,
         spawnId = spawnId,
         secretEntrance = this
      });
   }

   public void Update () {
      if (isMapEditorMode) {
         return;
      }

      // Figure out whether our outline should be showing
      handleSpriteOutline();
   }

   public void handleSpriteOutline () {
      if (_outline == null || isInteracted) {
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
               initSpritePath = value;
               break;
            case DataField.SECRETS_INTERACT_SPRITE:
               interactSpritePath = value;
               break;
            case DataField.WARP_TARGET_MAP_KEY:
               string areaName = AreaManager.self.getAreaName(int.Parse(value));
               areaTarget = areaName;
               warpAreaText.text = areaTarget;
               break;
            case DataField.WARP_TARGET_SPAWN_KEY:
               spawnTarget = value;
               break;
            case DataField.PLACED_PREFAB_ID:
               // TODO: Confirm if this will still be needed
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

   public void tryToInteract () {
      if (Global.player != null) {
         Global.player.rpc.Cmd_InteractSecretEntrance(instanceId, spawnId);
      }
   }

   public void completeInteraction () {
      if (!isInteracted) {
         isInteracted = true;

         // Sends animation commands to all clients
         Rpc_InteractAnimation();

         // Let the animation play before enabling the warp object
         StartCoroutine(CO_ProcessInteraction());
      }
   }

   public void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.secretsParent, worldPositionStays);
   }

   private void setSprites () {
      if (!Util.isBatch()) {
         spriteRenderer.sprite = mainSprite;
         subSpriteRenderer.sprite = subSprite;

         mainSpriteComponent.sprites = ImageManager.getSprites(mainSprite.texture);
         mainSpriteComponent.maxIndex = mainSpriteComponent.sprites.Length;
         mainSpriteComponent.currentIndex = 0;

         subSpriteComponent.sprites = ImageManager.getSprites(subSprite.texture);
         subSpriteComponent.maxIndex = subSpriteComponent.sprites.Length;
         subSpriteComponent.currentIndex = 0;
      }
   }

   [ClientRpc]
   public void Rpc_InteractAnimation () {
      if (!Util.isBatch()) {
         setSprites();

         _outline.setVisibility(false);
         _outline.enabled = false;
         _outline = null;
         InvokeRepeating("playMainSpriteAnimation", 0, animationSpeed);
         InvokeRepeating("playSubSpriteAnimation", 1, animationSpeed);
      }
   }

   private void playMainSpriteAnimation () {
      if (mainSpriteComponent.currentIndex < mainSpriteComponent.maxIndex / 2) {
         mainSpriteComponent.currentIndex++;
         spriteRenderer.sprite = mainSpriteComponent.sprites[mainSpriteComponent.currentIndex];
      }
   }

   private void playSubSpriteAnimation () {
      if (subSpriteComponent.currentIndex < subSpriteComponent.maxIndex / 2) {
         subSpriteComponent.currentIndex++;
         subSpriteRenderer.sprite = subSpriteComponent.sprites[subSpriteComponent.currentIndex];
      } else {
         endSpriteAnimation();
      }
   }

   private void endSpriteAnimation () {
      if (!Util.isBatch()) {
         spriteRenderer.sprite = mainSpriteComponent.sprites[mainSpriteComponent.maxIndex / 2];
         subSpriteRenderer.sprite = subSpriteComponent.sprites[subSpriteComponent.maxIndex / 2];

      }
      CancelInvoke();
   }

   private IEnumerator CO_ProcessInteraction () {
      yield return new WaitForSeconds(2);
      spriteRenderer.transform.position = interactPosition.position;
      warp.gameObject.SetActive(true);
      isFinishedAnimating = true;
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

public class SecretObjectSpriteData {
   // The length of the sprite selected for the secret entrance
   public int currentIndex = 0;

   // The compilation of sprites cached
   public int maxIndex = 10;
   public Sprite[] sprites = new Sprite[0];
}