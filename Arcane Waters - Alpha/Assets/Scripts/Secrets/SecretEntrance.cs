using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class SecretEntrance : NetworkBehaviour {
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

   // Determines if the object is interacted
   [SyncVar]
   public bool isInteracted = false;

   // The warp associated with this secret entrance
   public Warp warp;

   // The object containing the UI
   public GameObject warpTextUI;

   // The text where the warp will lead to
   public Text warpAreaText;

   // The sprite to be animated
   public SecretObjectSpriteData mainSpriteComponent = new SecretObjectSpriteData();
   public SecretObjectSpriteData subSpriteComponent = new SecretObjectSpriteData();

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
      
      mainSprite = ImageManager.getSprite(initSpritePath);
      subSprite = ImageManager.getSprites(interactSpritePath)[0];
      if (!isInteracted) {
         warpTextUI.SetActive(false);
         spriteRenderer.sprite = mainSprite;
      } else {
         warpTextUI.SetActive(true);
         int spriteLength = ImageManager.getSprites(interactSpritePath).Length;
         spriteRenderer.sprite = ImageManager.getSprites(interactSpritePath)[spriteLength - 1];
      }

      spriteRenderer.enabled = true;
      _outline.Regenerate();
      _outline.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;

      warp.areaTarget = areaTarget;
      warp.spawnTarget = spawnTarget;
      warp.newFacingDirection = newFacingDirection;
      warp.warpEvent.AddListener(player => {
         userIds.Add(player.userId);

         // Keep track of the user's location while in the secrets room
         SecretsManager.self.enterUserToSecret(player.userId, areaTarget, player.instanceId, this);
      });

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
               mainSprite = ImageManager.getSprite(value);
               spriteRenderer.sprite = mainSprite;
               initSpritePath = value;
               break;
            case DataField.SECRETS_INTERACT_SPRITE:
               subSprite = ImageManager.getSprites(value)[0];
               subSpriteRenderer.sprite = subSprite;
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
            if (!userIds.Contains(entity.userId)) {
               completeInteraction(entity);
            } 
         }
      }
   }

   public void tryToInteract () {
      if (Global.player != null) {
         if (!userIds.Contains(Global.player.userId)) {
            completeInteraction((PlayerBodyEntity) Global.player);
         }
      }
   }

   private void completeInteraction (PlayerBodyEntity player) {
      if (!isInteracted) {
         isInteracted = true;

         setSprites();

         InvokeRepeating("playMainSpriteAnimation", 0, .1f);
         _outline.setVisibility(false);

         Rpc_InteractAnimation();
         StartCoroutine(CO_ProcessInteraction(player));
      }
   }

   private void setSprites () {
      spriteRenderer.sprite = mainSprite;
      subSpriteRenderer.sprite = subSprite;

      mainSpriteComponent.sprites = ImageManager.getSprites(mainSprite.texture);
      mainSpriteComponent.maxIndex = mainSpriteComponent.sprites.Length;
      mainSpriteComponent.currentIndex = 0;

      subSpriteComponent.sprites = ImageManager.getSprites(subSprite.texture);
      subSpriteComponent.maxIndex = subSpriteComponent.sprites.Length;
      subSpriteComponent.currentIndex = 0;
   }

   [ClientRpc]
   public void Rpc_InteractAnimation () {
      setSprites();

      _outline.setVisibility(false);
      InvokeRepeating("playMainSpriteAnimation", 0, .1f);
      InvokeRepeating("playSubSpriteAnimation", 1, .1f);
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
      spriteRenderer.sprite = mainSpriteComponent.sprites[mainSpriteComponent.maxIndex / 2];
      subSpriteRenderer.sprite = subSpriteComponent.sprites[subSpriteComponent.maxIndex / 2];
      CancelInvoke();
      warpTextUI.SetActive(true);
   }

   private IEnumerator CO_ProcessInteraction (PlayerBodyEntity player) {
      yield return new WaitForSeconds(1.5f);
      spriteRenderer.transform.position = interactPosition.position;
      warp.gameObject.SetActive(true);
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