using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class PlantableTree : MonoBehaviour
{
   #region Public Variables

   // Types of actions the player can make when growing trees
   public enum GroomingAction
   {
      None = 0,
      Tether = 1,
      Untether = 2
   }

   // Main sprite renderer of this tree
   public SpriteRenderer spriteRenderer = null;

   // Tree shadow renderer
   public SpriteRenderer shadowRenderer = null;

   // The instance data of this tree
   public PlantableTreeInstanceData data = null;

   // Status text that's shown to the play
   public Text statusText = null;

   // Visual for showing water needed icon
   public GameObject waterNeededIcon = null;

   // Visual for showing interaction needed icon
   public GameObject interactionNeededIcon = null;

   // Space requirer component of this tree
   public SpaceRequirer spaceRequirer = null;

   // How many chops were applied recently
   public int currentChopCount = 0;

   // When was the last chop added
   public float lastChopTime = 0;

   // Animation curves used to determine how tree rotates during chopping
   public AnimationCurve[] wiggleCurves;

   // Particle system we use to play leaf fall effect
   public ParticleSystem leafFallEffect;

   #endregion

   private void Awake () {
      statusText.enabled = false;
   }

   public void applyState (bool justCreated, PlantableTreeInstanceData instanceData, PlantableTreeDefinition definitionData, bool client) {
      transform.localPosition = instanceData.position;
      GetComponent<ZSnap>().snapZ();

      _treeDefinition = definitionData;
      data = instanceData;

      _isLocalPlayerOwner = Global.player != null && Global.player.userId == instanceData.planterUserId;

      // If tree was cut down, play explode effect
      if (!justCreated && client && definitionData.isStump(instanceData)) {
         EffectManager.self.create(Effect.Type.Leaves_Exploding, (Vector2) transform.position + Vector2.up * 0.32f);
      }

      updateVisuals(TimeManager.self.getLastServerUnixTimestamp(), true);

      if (Application.isEditor) {
         _statePreviewEditor = JsonUtility.ToJson(instanceData) + System.Environment.NewLine + JsonUtility.ToJson(definitionData);
      }
   }

   private void setTreeSprite (PlantableTreeInstanceData instanceData, PlantableTreeDefinition definitionData, long currentTimestamp) {
      Sprite[] sprites = ImageManager.getSprites(definitionData.growthStageSprites);
      if (sprites.Length == 0) {
         D.error("Plantable tree " + definitionData.title + " has no sprites assigned!");
         return;
      }

      // Check if we should use a stump
      if (definitionData.leavesStump && instanceData.growthStagesCompleted >= PlantableTreeDefinition.STUMP_GROWTH_STAGE) {
         shadowRenderer.enabled = false;
         spriteRenderer.sprite = sprites[sprites.Length - 1];
         return;
      }

      shadowRenderer.enabled = true;

      // Get current index, check if we are fully grown, clamp it in range
      int max = definitionData.leavesStump ? sprites.Length - 2 : sprites.Length - 1;
      int index = Mathf.Clamp(
         instanceData.growthStagesCompleted + (definitionData.isFullyGrown(instanceData, currentTimestamp) ? 1 : 0),
         0,
         max);

      spriteRenderer.sprite = sprites[index];
      shadowRenderer.sprite = ImageManager.getSprites(shadowRenderer.sprite.texture)[index];
   }

   public float chopyness = 10f;

   private void Update () {
      if (Util.isBatch()) {
         return;
      }

      // Animate chopping wiggle
      if (currentChopCount == 0) {
         spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, 0);
      } else {
         float val = wiggleCurves[Mathf.Clamp(currentChopCount - 1, 0, wiggleCurves.Length - 1)].Evaluate(Time.time - lastChopTime);
         val *= (_cutFromLeftSide ? 1f : -1f);
         spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, val);
      }

      if (_hovered) {
         statusText.text = _treeDefinition.getStatusText(data, TimeManager.self.getLastServerUnixTimestamp(), _isLocalPlayerOwner);
      }

      long time = TimeManager.self.getLastServerUnixTimestamp();
      if (time != _lastUpdateTime) {
         _lastUpdateTime = time;
         updateVisuals(_lastUpdateTime, false);
      }
   }

   public void receiveChop (bool isFromLeftSide) {
      _cutFromLeftSide = isFromLeftSide;

      if (Time.time - lastChopTime > 0.9f) {
         currentChopCount = 0;
      }
      lastChopTime = Time.time;
      currentChopCount++;

      if (!_treeDefinition.isStump(data) && _treeDefinition.leavesStump) {
         int particleCount = (int) Mathf.Pow(3, currentChopCount);

         ParticleSystem.EmissionModule em = leafFallEffect.emission;
         em.rateOverTime = (int) (particleCount / leafFallEffect.main.duration);

         leafFallEffect.Play();
      }

      // Play sound effect
      SoundEffectManager.self.playTreeChop(this.transform.position, currentChopCount >= 3);
   }

   public bool isOneHitAwayFromDestroy () {
      return currentChopCount == 2;
   }

   private void updateVisuals (long currentTimestamp, bool stateChanged) {
      if (Util.isBatch()) {
         return;
      }

      if (stateChanged) {
         setTreeSprite(data, _treeDefinition, currentTimestamp);
      }

      waterNeededIcon.SetActive(_treeDefinition.needsWatering(data, currentTimestamp) && isPlayerHoldingWaterCan());
      interactionNeededIcon.SetActive(false);
      //interactionNeededIcon.SetActive(_treeDefinition.canTetherUntether(data, currentTimestamp));

      if (stateChanged && spriteRenderer.sprite != null) {
         waterNeededIcon.transform.localPosition = new Vector3(
            waterNeededIcon.transform.localPosition.x,
            getSpriteNonTransparentHeight(spriteRenderer.sprite),
            waterNeededIcon.transform.localPosition.z);
      }
   }
   
   private bool isPlayerHoldingWaterCan () {
      // Get player instance and check if action type is watering crop
      if (Global.player != null && Global.player is BodyEntity) {
         BodyEntity playerBody = (BodyEntity) Global.player;

         return (playerBody.weaponManager.actionType == Weapon.ActionType.WaterCrop);
      }

      return false;
   }

   private float getSpriteNonTransparentHeight (Sprite sprite) {
      int pixelX = (int) sprite.rect.center.x;
      int pixelHeight = (int) sprite.rect.height;

      for (int i = 0; i < sprite.rect.height && i < 500; i++) {
         if (sprite.texture.GetPixel(pixelX, (int) sprite.rect.yMax - i).a > 0.05f) {
            pixelHeight = (int) sprite.rect.height - i;
            break;
         }
      }

      return pixelHeight / sprite.pixelsPerUnit;
   }

   [Client]
   public void hoverEnter () {
      _hovered = true;
      statusText.text = _treeDefinition.getStatusText(data, TimeManager.self.getLastServerUnixTimestamp(), _isLocalPlayerOwner);
      //statusText.enabled = true;
      statusText.enabled = false;
   }

   [Client]
   public void hoverExit () {
      _hovered = false;
      statusText.enabled = false;
   }

   [Client]
   public void click (BaseEventData eventData) {
      //if (Global.player == null) return;

      //PlantableTreeManager.self.playerClickedTree(data.id);
   }

   #region Private Variables

   // Preview of this tree's data in editor
   [SerializeField, TextArea()]
   private string _statePreviewEditor = "";

   // The type of tree this is
   private PlantableTreeDefinition _treeDefinition = null;

   // Is tree hovered right now
   private bool _hovered = false;

   // Last time we updated the tree
   private long _lastUpdateTime;

   // Is local player the owner of this tree
   private bool _isLocalPlayerOwner = false;

   // Is this tree being cut from left side
   private bool _cutFromLeftSide = false;

   #endregion
}
