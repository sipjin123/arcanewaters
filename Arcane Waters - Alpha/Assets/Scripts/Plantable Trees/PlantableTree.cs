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

   // Sprites for each growth stage
   public List<Sprite> sprites = new List<Sprite>();

   // Main sprite renderer of this tree
   public SpriteRenderer spriteRenderer = null;

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

   // The main sprite of a fully grown tree
   public Sprite grownTreeSprite = null;

   #endregion

   private void Awake () {
      statusText.enabled = false;

      // Don't need this in server
      if (!Util.isBatch()) {
         _propertyBlock = new MaterialPropertyBlock();
      }
   }

   public void applyState (bool justCreated, PlantableTreeInstanceData instanceData, PlantableTreeDefinition definitionData) {
      transform.localPosition = instanceData.position;
      GetComponent<ZSnap>().snapZ();
      _treeDefinition = definitionData;
      data = instanceData;

      _isLocalPlayerOwner = Global.player != null && Global.player.userId == instanceData.planterUserId;

      updateVisuals(TimeManager.self.getLastServerUnixTimestamp());
   }

   private Sprite pickSprite (PlantableTreeInstanceData instanceData, PlantableTreeDefinition definitionData, long currentTimestamp) {
      // We are hard picking indexes here, not ideal
      return sprites[Mathf.Clamp(instanceData.growthStagesCompleted + (definitionData.isFullyGrown(instanceData, currentTimestamp) ? 1 : 0), 0, sprites.Count - 1)];
   }

   private void Update () {
      if (Util.isBatch()) {
         return;
      }

      // Shader controls whether the tree shakes or not
      float shakeStrength = Time.time - lastChopTime > 0.5f ? 0 : currentChopCount / 3f;
      spriteRenderer.GetPropertyBlock(_propertyBlock);
      _propertyBlock.SetFloat("_ChopAmount", shakeStrength);
      spriteRenderer.SetPropertyBlock(_propertyBlock);

      if (_hovered) {
         statusText.text = _treeDefinition.getStatusText(data, TimeManager.self.getLastServerUnixTimestamp(), _isLocalPlayerOwner);
      }

      long time = TimeManager.self.getLastServerUnixTimestamp();
      if (time != _lastUpdateTime) {
         _lastUpdateTime = time;
         updateVisuals(_lastUpdateTime);
      }
   }

   public void receiveChop () {
      if (Time.time - lastChopTime > 0.6f) {
         currentChopCount = 0;
      }
      lastChopTime = Time.time;
      currentChopCount++;
   }

   private void updateVisuals (long currentTimestamp) {
      if (Util.isBatch()) {
         return;
      }

      spriteRenderer.sprite = pickSprite(data, _treeDefinition, currentTimestamp);

      waterNeededIcon.SetActive(_treeDefinition.needsWatering(data, currentTimestamp));
      interactionNeededIcon.SetActive(false);
      //interactionNeededIcon.SetActive(_treeDefinition.canTetherUntether(data, currentTimestamp));
   }

   [Client]
   public void hoverEnter () {
      _hovered = true;
      statusText.text = _treeDefinition.getStatusText(data, TimeManager.self.getLastServerUnixTimestamp(), _isLocalPlayerOwner);
      statusText.enabled = true;
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

   // The type of tree this is
   private PlantableTreeDefinition _treeDefinition = null;

   // Is tree hovered right now
   private bool _hovered = false;

   // Last time we updated the tree
   private long _lastUpdateTime;

   // Material property block of the main renderer
   private MaterialPropertyBlock _propertyBlock;

   // Is local player the owner of this tree
   private bool _isLocalPlayerOwner = false;

   #endregion
}
