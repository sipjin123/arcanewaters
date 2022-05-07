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

   // Renderer, which displays the dock
   public SpriteRenderer dockRenderer;

   // The parent of all buildings
   public Transform buildingsParent;

   // List of outpost dock sprites, indexed by direction
   public Sprite[] dockDirectionSprites = new Sprite[9];

   // A child object that will pass on onTriggerEnter2D events for food filling
   public TriggerDetector foodFillDetector;

   #endregion

   protected override void Awake () {
      base.Awake();

      foodFillDetector.onTriggerStay += onFoodFillTriggerStay;

      _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
   }

   public override void OnStartClient () {
      base.OnStartClient();

      setDirection(outpostDirection);
      outpostsClient.Add(this);
      updateGuildIconSprites();
   }

   protected override void Update () {
      base.Update();

      if (isServer && isDead()) {
         NetworkServer.Destroy(this.gameObject);
      }
   }

   public override void setAreaParent (Area area, bool worldPositionStays) {
      base.setAreaParent(area, worldPositionStays);

      // Trim trees around the outpost
      OutpostUtil.disableTreesAroundOutpost(area, this);
   }

   protected override void OnDestroy () {
      base.OnDestroy();

      int index = outpostsClient.IndexOf(this);
      if (index >= 0) {
         outpostsClient.RemoveAt(index);
      }
   }

   private void onFoodFillTriggerStay (Collider2D collider) {
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
         if (entity.guildId == guildId || entity.guildId == 0) {
            entity.currentFood = Mathf.Clamp(entity.currentFood + Time.deltaTime * FOOD_FILL_PER_SECOND, 0, entity.maxFood);
         }
      }
   }

   protected override NetEntity getAttackerInRange () {
      // If we're targeting a player ship that's in our range, don't find a new target
      if (_aimTarget && _aimTarget.isPlayerShip() && isInRange(_aimTarget.transform.position)) {
         return _aimTarget;
      }

      if (InstanceManager.self.tryGetInstance(instanceId, out Instance instance)) {
         foreach (NetworkBehaviour beh in instance.entities) {
            NetEntity entity = beh as NetEntity;
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

   protected override bool isValidTarget (NetEntity entity) {
      return
         entity != null && entity.isPlayerShip() &&
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
      foreach (SpriteRenderer ren in _spriteRenderers) {
         Util.setAlpha(ren, validBuild ? 1f : 0.5f);
      }

      setDirection(direction);
   }

   public void setDirection (Direction direction) {
      buildingsParent.transform.localPosition = -Util.getDirectionFromFacing(direction) * OutpostUtil.DOCK_TO_BUILDING_DISTANCE;

      int spriteIndex = (int) direction;
      if (spriteIndex < 0 || spriteIndex >= dockDirectionSprites.Length) {
         D.error("Invalid sprite for outpost dock");
         return;
      }

      dockRenderer.sprite = dockDirectionSprites[spriteIndex];
   }

   public IEnumerator CO_ActivateAfter (float delay) {
      yield return new WaitForSeconds(delay);

      setIsActivated(true);
      Rpc_SetIsActivated(true);
   }

   #region Private Variables

   // All the sprite renderers that the outpost has
   private SpriteRenderer[] _spriteRenderers;

   #endregion
}
