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

   // If the sprites can blend with the assets behind it
   [SyncVar]
   public bool canBlend, canBlendInteract2;

   // The position the sprite should be set after interacting
   public Transform interactPosition;

   // The area for this warp
   public string areaTarget;

   // The spawn for this warp
   public string spawnTarget;

   // Information about targeted map, can be null if unset
   public Map targetInfo;

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

   // Collider altering values
   [SyncVar]
   public Vector2 colliderScale, colliderOffset, switchOffset;

   // Collider altering values of the post interact collision
   [SyncVar]
   public Vector2 postColliderScale, postColliderOffset;

   // The sprite that has a collider that blocks the user collision before the path is revealed
   public SpriteRenderer blockerSprite;

   // The sprite that has a collider that blocks the user collision after path is revealed
   public SpriteRenderer postBlockerSprite;

   #endregion

   private void Awake () {
      // Look up components
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
   }

   private void OnEnable () {
      blockerSprite.enabled = false;
      postBlockerSprite.enabled = false;
   }

   private void Start () {
      if (isMapEditorMode) {
         return;
      }

      // Make the node a child of the Area
      StartCoroutine(CO_SetAreaParent());

      // Collision Setup
      blockerSprite.transform.localPosition = colliderOffset;
      blockerSprite.transform.localScale = colliderScale;
      postBlockerSprite.transform.localPosition = postColliderOffset;
      postBlockerSprite.transform.localScale = postColliderScale;

      // Switch offset
      spriteRenderer.transform.localPosition = switchOffset;
      _clickableBox.transform.position = spriteRenderer.transform.position;

      // Sprite and collision enabled/disabled
      blockerSprite.enabled = false;
      postBlockerSprite.enabled = false;
      blockerSprite.gameObject.SetActive(!isInteracted);
      postBlockerSprite.gameObject.SetActive(isInteracted);
      if (!Util.isBatch()) {
         checkBlending();

         try {
            mainSprite = ImageManager.getSprite(initSpritePath);
            subSprite = ImageManager.getSprites(interactSpritePath)[0];
            if (!isInteracted) {
               spriteRenderer.sprite = mainSprite;
               subSpriteRenderer.sprite = subSprite;
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

   private void checkBlending () {
      // This feature will set the alpha of the primary sprite to zero if its true
      // The primary sprite is designed to be used as the sprite that the user interacts to reveal the second sprite that shows the actual entrance {bookcase / waterfall}
      if (canBlend) {
         Color tmp = spriteRenderer.color;
         tmp.a = isInteracted ? 1 : 0;
         spriteRenderer.color = tmp;
         subSpriteRenderer.color = tmp;
      }

      // This feature will set the alpha of the secondary sprite to zero if its true
      // The secondary sprite is designed to be used as the sprite that animated once the path is revealed {trapdoor / boulder / tree stump}
      if (canBlendInteract2) {
         Color tmp = subSpriteRenderer.color;
         tmp.a = isInteracted ? 1 : 0;
         subSpriteRenderer.color = tmp;
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
      if (isHovering && isGlobalPlayerNearby()) {
         _outline.setVisibility(true);
      } else {
         _outline.setVisibility(false);
      }
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
            case DataField.TARGET_MAP_INFO_KEY:
               targetInfo = field.objectValue<Map>();
               break;
            case DataField.WARP_ARRIVE_FACING_KEY:
               if (field.tryGetDirectionValue(out Direction dir)) {
                  newFacingDirection = dir;
               }
               break;
            case DataField.SECRETS_COLLIDER_OFFSET_X:
               try {
                  float newVal = float.Parse(field.v);
                  colliderOffset.x = newVal;
               } catch {

               }
               break;
            case DataField.SECRETS_COLLIDER_OFFSET_Y:
               try {
                  float newVal = float.Parse(field.v);
                  colliderOffset.y = newVal;
               } catch {

               }
               break;
            case DataField.SECRETS_COLLIDER_SCALE_X:
               try {
                  float newValue = float.Parse(field.v);
                  colliderScale.x = newValue;
               } catch {

               }
               break;
            case DataField.SECRETS_COLLIDER_SCALE_Y:
               try {
                  float newValue = float.Parse(field.v);
                  colliderScale.y = newValue;
               } catch {

               }
               break;
            case DataField.SECRETS_POST_COLLIDER_OFFSET_X:
               try {
                  float newVal = float.Parse(field.v);
                  postColliderOffset.x = newVal;
               } catch {

               }
               break;
            case DataField.SECRETS_POST_COLLIDER_OFFSET_Y:
               try {
                  float newVal = float.Parse(field.v);
                  postColliderOffset.y = newVal;
               } catch {

               }
               break;
            case DataField.SECRETS_POST_COLLIDER_SCALE_X:
               try {
                  float newValue = float.Parse(field.v);
                  postColliderScale.x = newValue;
               } catch {

               }
               break;
            case DataField.SECRETS_POST_COLLIDER_SCALE_Y:
               try {
                  float newValue = float.Parse(field.v);
                  postColliderScale.y = newValue;
               } catch {

               }
               break;
            case DataField.SECRETS_CAN_BLEND:
               canBlend = field.v.ToLower() == "true";
               break;
            case DataField.SECRETS_CAN_BLEND_INTERACTED:
               canBlendInteract2 = field.v.ToLower() == "true";
               break;
            case DataField.SECRETS_SWITCH_OFFSET_X:
               try {
                  float newVal = float.Parse(field.v);
                  switchOffset.x = newVal;
               } catch {

               }
               break;
            case DataField.SECRETS_SWITCH_OFFSET_Y:
               try {
                  float newVal = float.Parse(field.v);
                  switchOffset.y = newVal;
               } catch {

               }
               break; 
         }
      }
   }

   public void tryToInteract () {
      if (isGlobalPlayerNearby()) {
         Global.player.rpc.Cmd_InteractSecretEntrance(instanceId, spawnId);
      } else {
         D.editorLog("Player it Too far from the secret entrance!", Color.red);
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

   public bool isGlobalPlayerNearby () {
      if (Global.player == null) {
         return false;
      }

      return (Vector2.Distance(Global.player.transform.position, this.transform.position) <= 1);
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
         blockerSprite.gameObject.SetActive(false);
         postBlockerSprite.gameObject.SetActive(true);
         checkBlending();
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
      //spriteRenderer.transform.position = interactPosition.position;
      warp.gameObject.SetActive(true);
      isFinishedAnimating = true;
      blockerSprite.gameObject.SetActive(false);
      postBlockerSprite.gameObject.SetActive(true);
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

   [SerializeField]
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