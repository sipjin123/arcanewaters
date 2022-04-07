using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SecretEntrance : MonoBehaviour {
   #region Public Variables

   // Sprite appropriate for the current state of this node
   public Sprite mainSprite, subSprite;

   // The current sprite renderer
   public SpriteRenderer spriteRenderer, subSpriteRenderer;

   // Sprites for animation
   public Sprite[] mainSpritesArray, subSpriteArray;

   // The secret entrance holder
   public SecretEntranceHolder secretEntranceHolder;

   // The position the sprite should be set after interacting
   public Transform interactPosition;

   // If this object is being used in the map editor
   public bool isMapEditorMode;

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

      // Sprite and collision enabled/disabled
      blockerSprite.enabled = false;
      postBlockerSprite.enabled = false;
      blockerSprite.gameObject.SetActive(!secretEntranceHolder.isInteracted);
      postBlockerSprite.gameObject.SetActive(secretEntranceHolder.isInteracted);
      if (!Util.isBatch()) {
         checkBlending();

         try {
            if (!secretEntranceHolder.isInteracted) {
               spriteRenderer.sprite = mainSprite;
               subSpriteRenderer.sprite = subSprite;
            } else {
               // Multiplies the animation speed depending on the number of sprites in the sprite sheet (the more the sprites the faster the animation)
               animationSpeed *= mainSpritesArray.Length / DEFAULT_SPRITESHEET_COUNT;
               animationSpeed = Mathf.Clamp(animationSpeed, .05f, 2);
            }

            if (subSprite.name == "none") {
               subSpriteRenderer.gameObject.SetActive(false);
            }
            spriteRenderer.enabled = true;
         } catch {
            D.debug("Failed to process sprites for Secret Entrance");
         }
      }

      // Server will set the warp info
      if (NetworkServer.active) {
         warp.areaTarget = secretEntranceHolder.areaTarget;
         warp.spawnTarget = secretEntranceHolder.spawnTarget;
         warp.newFacingDirection = secretEntranceHolder.newFacingDirection;

         // Make sure warp is disabled for host mode, otherwise player will be able to warp even if secret entrance has not been interacted
         if (NetworkClient.active && NetworkClient.isConnected) {
            warp.gameObject.SetActive(false);
         }
      } else {
         // Warp objects are only inactive for clients and will be enabled upon interaction
         // Server warps are always enabled since colliding on server side no longer triggers warp, server warp needs to stay active so server side warping coroutine can process properly
         warp.gameObject.SetActive(false);
      }

      if (!Util.isBatch()) { 
         if (subSprite.name == "none") {
            subSpriteRenderer.gameObject.SetActive(false);
         }

         setSprites();
         if (secretEntranceHolder.isInteracted) {
            _outline.setVisibility(false);
            spriteRenderer.sprite = mainSpriteComponent.sprites[mainSpriteComponent.maxIndex / 2];
            subSpriteRenderer.sprite = subSpriteComponent.sprites[subSpriteComponent.maxIndex / 2];
         } else {
            _outline.setVisibility(true);
            spriteRenderer.sprite = mainSpriteComponent.sprites[0];
            subSpriteRenderer.sprite = subSpriteComponent.sprites[0];

            mainSpriteComponent.currentIndex = mainSpriteComponent.maxIndex / 2;
            subSpriteComponent.currentIndex = subSpriteComponent.maxIndex / 2;
         }
      }
   }

   private void checkBlending (bool isInteractOverride = false) {
      // This feature will set the alpha of the primary sprite to zero if its true
      // The primary sprite is designed to be used as the sprite that the user interacts to reveal the second sprite that shows the actual entrance {bookcase / waterfall}
      if (secretEntranceHolder.canBlend) {
         Color tmp = spriteRenderer.color;
         tmp.a = secretEntranceHolder.isInteracted ? 1 : 0;
         if (isInteractOverride) {
            tmp.a = 1;
         }
         spriteRenderer.color = tmp;
         subSpriteRenderer.color = tmp;
      }

      // This feature will set the alpha of the secondary sprite to zero if its true
      // The secondary sprite is designed to be used as the sprite that animated once the path is revealed {trapdoor / boulder / tree stump}
      if (secretEntranceHolder.canBlendInteract2) {
         Color tmp = subSpriteRenderer.color;
         tmp.a = secretEntranceHolder.isInteracted ? 1 : 0;
         if (isInteractOverride) {
            tmp.a = 1;
         }
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
      if (_outline == null || secretEntranceHolder.isInteracted) {
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

   public void tryToInteract () {
      if (isGlobalPlayerNearby()) {
         secretEntranceHolder.completeInteraction();
         secretEntranceHolder.cachedSecretEntrance.interactAnimation();
      } else {
         D.editorLog("Player it Too far from the secret entrance!", Color.red);
      }
   }

   public bool isGlobalPlayerNearby () {
      if (Global.player == null) {
         return false;
      }

      return (Vector2.Distance(Global.player.transform.position, this.transform.position) <= 1);
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

   public void interactAnimation () {
      if (!Util.isBatch()) {
         setSprites();
         if (_outline == null) {
            _outline = GetComponentInChildren<SpriteOutline>();
         }
         _outline.setVisibility(false);
         _outline = null;

         InvokeRepeating("playMainSpriteAnimation", 0, animationSpeed);
         InvokeRepeating("playSubSpriteAnimation", 1, animationSpeed);

         blockerSprite.gameObject.SetActive(false);
         postBlockerSprite.gameObject.SetActive(true);
         checkBlending(true);
      }
   }

   public void closeEntrance () {
      if (!Util.isBatch()) {
         _outline = GetComponentInChildren<SpriteOutline>();
         _outline.setVisibility(true);

         secretEntranceHolder.isRunningCoroutineAnimation = true;
         InvokeRepeating("playMainSpriteAnimationClose", 0, animationSpeed);
         InvokeRepeating("playSubSpriteAnimationClose", 0, animationSpeed);

         blockerSprite.gameObject.SetActive(true);
         postBlockerSprite.gameObject.SetActive(false);
         checkBlending(true);
      }
   }

   #region Playing Animation

   private void playMainSpriteAnimationClose () {
      if (mainSpriteComponent.currentIndex < mainSpriteComponent.maxIndex -1) {
         mainSpriteComponent.currentIndex++;
         spriteRenderer.sprite = mainSpriteComponent.sprites[mainSpriteComponent.currentIndex];
      }
   }

   private void playSubSpriteAnimationClose () {
      if (subSpriteComponent.currentIndex < subSpriteComponent.maxIndex -1) {
         subSpriteComponent.currentIndex++;
         subSpriteRenderer.sprite = subSpriteComponent.sprites[subSpriteComponent.currentIndex];
      } else {
         if (!Util.isBatch()) {
            spriteRenderer.sprite = mainSpriteComponent.sprites[0];
            subSpriteRenderer.sprite = subSpriteComponent.sprites[0];
            subSpriteComponent.currentIndex = 0;
         }
         secretEntranceHolder.isRunningCoroutineAnimation = false;
         secretEntranceHolder.isFinishedAnimating = true;
         CancelInvoke();
      }
   }
   
   private void playMainSpriteAnimation () {
      if (mainSpriteComponent.currentIndex < mainSpriteComponent.maxIndex / 2) {
         mainSpriteComponent.currentIndex++;
         spriteRenderer.sprite = mainSpriteComponent.sprites[mainSpriteComponent.currentIndex];
      }
   }

   private void playSubSpriteAnimation () {
      // Play sound effect
      if (subSpriteComponent.currentIndex == 0) {
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.WATERFALL_SECRET, warp.transform.position);
      }

      if (subSpriteComponent.currentIndex < subSpriteComponent.maxIndex / 2) {
         subSpriteComponent.currentIndex++;
         subSpriteRenderer.sprite = subSpriteComponent.sprites[subSpriteComponent.currentIndex];
      } else {
         if (!Util.isBatch()) {
            spriteRenderer.sprite = mainSpriteComponent.sprites[mainSpriteComponent.maxIndex / 2];
            subSpriteRenderer.sprite = subSpriteComponent.sprites[subSpriteComponent.maxIndex / 2];
         }
         CancelInvoke();
      }
   }

   #endregion

   public void processInteraction () {
      // Let the animation play before enabling the warp object
      StartCoroutine(CO_ProcessInteraction());
   }

   private IEnumerator CO_ProcessInteraction () {
      yield return new WaitForSeconds(2);
      warp.gameObject.SetActive(true);
      secretEntranceHolder.isFinishedAnimating = true;
      blockerSprite.gameObject.SetActive(false);
      postBlockerSprite.gameObject.SetActive(true);
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