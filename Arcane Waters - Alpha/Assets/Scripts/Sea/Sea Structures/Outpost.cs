using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Outpost : SeaStructureTower, IObserver
{
   #region Public Variables

   // How far away this unit can target and attack enemies
   public static float ATTACK_RANGE = 4.5f;

   // The range at which the attack range circle will be displayed
   public static float WARNING_RANGE = 5.5f;

   // How fast we fill ships with food
   public const float FOOD_FILL_PER_SECOND = 100f;

   // Which direction is the outpost facing
   [SyncVar]
   public Direction outpostDirection;

   // List of outposts in the client
   public static List<SeaStructure> outpostsClient = new List<SeaStructure>();
   
   [SyncVar]
   public bool displayMaterials;

   // Renderer, which displays the dock
   public SpriteRenderer dockRenderer;

   // The parent of all buildings
   public Transform buildingsParent;

   // List of outpost dock sprites, indexed by direction
   public Sprite[] dockDirectionSprites = new Sprite[9];

   // A child object that will pass on onTriggerEnter2D events for food filling
   public TriggerDetector foodFillDetector;

   // The food bar we use to display food
   public FoodBar foodBar;

   // The initial material count
   [SyncVar]
   public int initialMaterials;

   // The total material required
   public const int MATERIAL_REQUIREMENT = 500;

   // UI display for supplying required materials
   public GameObject supplyMaterialPanel, supplyMaterialBlocker;
   public Text materialRequirementText;
   public Image materialImage;
   public Button addMaterialButton;

   #endregion

   protected override void Awake () {
      base.Awake();

      foodFillDetector.onTriggerStay += onFoodFillTriggerStay;

      _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

      if (NetworkClient.active) {
         _outline = GetComponent<SpriteOutline>();
         _outline.setNewColor(Color.white);
         _outline.setVisibility(false);
      }
   }

   protected override void Start () {
      base.Start();
      if (NetworkClient.active) {
         Minimap.self.addOutpostIcon(this);
         if (initialMaterials < 1) {
            supplyMaterialPanel.SetActive(displayMaterials);
            materialRequirementText.text = 0 + "/" + MATERIAL_REQUIREMENT;
            supplyMaterialBlocker.SetActive(false);
         }
      }
   }

   public void onPointerExit () {
      _hovered = false;
      _outline.setVisibility(false);
   }

   public void onPointerEnter () {
      _hovered = true;
   }

   public override void OnStartClient () {
      base.OnStartClient();

      setDirection(outpostDirection);
      outpostsClient.Add(this);
      updateGuildIconSprites();
   }

   protected override void Update () {
      base.Update();

      if (initialMaterials < MATERIAL_REQUIREMENT) {
         return;
      }

      if (NetworkClient.active && _isActivated) {
         _outline.setVisibility(canPlayerInteract(Global.player) && _hovered);
      }


      if (!_isActivated || !isServer) {
         return;
      }

      if (isDead() || currentFood <= 0) {
         NetworkServer.Destroy(this.gameObject);
      }

      currentFood = Mathf.Clamp(currentFood - FOOD_PER_SECOND * Time.deltaTime, 0, maxFood);
   }

   public void onClick () {
      if (!canPlayerInteract(Global.player)) {
         return;
      }

      if (initialMaterials < MATERIAL_REQUIREMENT) {
         D.debug("Cant interact unfinished outpost!");
         return;
      }

      // Only works when the player is close enough
      if (!Util.distanceLessThan2D(transform.position, Global.player.transform.position, 1f)) {
         FloatingCanvas.instantiateAt(transform.position).asTooFar();
         return;
      }

      PanelManager.self.get<OutpostPanel>(Panel.Type.Outpost).open(this);
   }

   public bool canPlayerInteract (NetEntity entity) {
      return entity != null && (entity.guildId == guildId || entity.guildAllies.Contains(guildId));
   }

   public override void setAreaParent (Area area, bool worldPositionStays) {
      base.setAreaParent(area, worldPositionStays);

      // Trim trees around the outpost
      OutpostUtil.disableTreesAroundOutpost(area, this);
   }

   protected override void OnDestroy () {
      base.OnDestroy();

      if (NetworkClient.active) {
         Minimap.self.deleteOutpostIcon(this);
      }

      int index = outpostsClient.IndexOf(this);
      if (index >= 0) {
         outpostsClient.RemoveAt(index);
      }
   }

   private void onFoodFillTriggerStay (Collider2D collider) {
      if (initialMaterials < MATERIAL_REQUIREMENT) {
         return;
      }

      // If we have no guild, don't do anything
      if (guildId <= 0) {
         return;
      }

      // We only want to tun this on the server
      if (!NetworkServer.active) {
         return;
      }

      if (collider.TryGetComponent(out PlayerShipEntity entity)) {
         // Check if entity can receive food from us
         if (entity.guildId == guildId || entity.guildAllies.Contains(guildId) || entity.guildId == 0) {
            entity.currentFood = Mathf.Clamp(entity.currentFood + Time.deltaTime * FOOD_FILL_PER_SECOND, 0, entity.maxFood);
         }
      }
   }

   protected override SeaEntity getAttackerInRange (bool logData = false) {
      // If we're targeting a player ship that's in our range, don't find a new target
      if (_aimTarget && _aimTarget.isPlayerShip() && isInRange(_aimTarget.transform.position)) {
         return _aimTarget;
      }

      if (InstanceManager.self.tryGetInstance(instanceId, out Instance instance)) {
         foreach (NetworkBehaviour beh in instance.entities) {
            SeaEntity entity = beh as SeaEntity;
            if (entity != null) {
               if (isValidTarget(entity)) {
                  if (isInRange(entity.transform.position)) {
                     return entity;
                  }
               }
            }
         }
      }

      return null;
   }

   public void clickedSupplyButton () {
      int temporaryFixedSupplyValue = 100;
      if (Global.player != null) {
         supplyMaterialBlocker.SetActive(true);
         addMaterialButton.interactable = false;
         Global.player.rpc.Cmd_AddMaterialToOutpost(Global.player.guildId, Global.player.guildName, areaKey, temporaryFixedSupplyValue);
      }
   }

   [TargetRpc]
   public void Target_ReceiveFailMessage (NetworkConnection connection, string message) {
      ChatManager.self.addChat(message, ChatInfo.Type.System);
      supplyMaterialBlocker.SetActive(false);
      addMaterialButton.interactable = true;
   }

   [ClientRpc]
   public void Rpc_ReceiveInitialMaterials (int updatedMaterials, int materialsAdded) {
      supplyMaterialBlocker.SetActive(false);
      addMaterialButton.interactable = true;
      materialRequirementText.text = updatedMaterials + "/" + MATERIAL_REQUIREMENT;
      if (updatedMaterials >= MATERIAL_REQUIREMENT) {
         supplyMaterialPanel.SetActive(false);
      }
   }

   [ClientRpc]
   public void Rpc_FloatingMessage (string message) {
      Vector3 pos = this.transform.position + new Vector3(0f, .32f);
      GameObject messageCanvas = Instantiate(PrefabsManager.self.warningTextPrefab);
      messageCanvas.transform.position = pos;
      messageCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = message;
   }

   protected override bool isValidTarget (NetEntity entity) {
      return
         entity != null &&
         !entity.isAllyOf(this) && entity.isEnemyOf(this);
   }

   protected override float getAttackRange () {
      return ATTACK_RANGE;
   }

   protected override float getWarningRange () {
      return WARNING_RANGE;
   }

   public int getInstanceId () {
      return instanceId;
   }

   public void setAsBuildHighlight (bool validBuild, Direction direction) {
      supplyMaterialPanel.SetActive(false);
      foreach (SpriteRenderer ren in _spriteRenderers) {
         Util.setAlpha(ren, validBuild ? 1f : 0.5f);
      }

      foodBar.gameObject.SetActive(false);
      setDirection(direction);

      // Important to disable colliders after SetDirection, since that adds another collider
      foreach (Collider2D col in GetComponentsInChildren<Collider2D>()) {
         col.enabled = false;
      }
   }

   public void setDirection (Direction direction) {
      buildingsParent.transform.localPosition = -Util.getDirectionFromFacing(direction) * OutpostUtil.DOCK_TO_BUILDING_DISTANCE;

      int spriteIndex = (int) direction;
      if (spriteIndex < 0 || spriteIndex >= dockDirectionSprites.Length) {
         D.error("Invalid sprite for outpost dock");
         return;
      }

      dockRenderer.sprite = dockDirectionSprites[spriteIndex];

      // Clean up previously added colliders
      foreach (PolygonCollider2D poly in dockRenderer.gameObject.GetComponents<PolygonCollider2D>()) {
         Destroy(poly);
      }
      // Add collider to dock here so it gets generated based on sprite
      dockRenderer.gameObject.AddComponent<PolygonCollider2D>();
   }

   public IEnumerator CO_ActivateAfter (float delay) {
      yield return new WaitForSeconds(delay);
      setIsActivated(true);
      Rpc_SetIsActivated(true);
   }

   #region Private Variables

   // All the sprite renderers that the outpost has
   private SpriteRenderer[] _spriteRenderers;

   // Are we hovered right now (client)
   private bool _hovered = false;

   #endregion
}
