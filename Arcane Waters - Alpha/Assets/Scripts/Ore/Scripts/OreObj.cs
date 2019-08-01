using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OreObj : NetworkBehaviour
{
   #region Public Variables

   // The unique ID assigned to this chest
   [SyncVar(hook = "onAreaIDChanged")]
   public int areaID;

   // The unique ID assigned to this chest
   [SyncVar(hook = "onIDChanged")]
   public int id;

   // The instance that this chest is in
   [SyncVar]
   public int instanceId;

   // How close we have to be in order to mine
   public static float MINING_DISTANCE = .45f;

   // Holds scriptable object data
   public OreData oreData;

   // Animation for mining
   public Animator miningAnimation;

   // Number of interaction to get reward
   public int oreLife;

   // If mining is active
   public bool isActive = true;

   // Unique id of the ore
   public int oreID;

   // The area where the ore spawned
   public Area.Type oreArea;

   // The index of the spawn point
   public int oreSpawnID;

   // The list of user IDs that have opened this chest
   public SyncListInt userIds = new SyncListInt();

   public bool hasParentList;

   #endregion

   void onAreaIDChanged (int id) {
      if (hasParentList == false) {
         hasParentList = true;
         Area area = AreaManager.self.getArea((Area.Type) id);
         OreArea newOreArea = area.GetComponent<OreArea>();
         newOreArea.oreList.Add(this);
         transform.SetParent(newOreArea.oreObjHolder);
      }
   }

   void onIDChanged (int id) {
      OreManager.self.registerOreObj(id, this);
   }

   private void Awake () {
      // Component setup
      _graphicRaycaster = GetComponentInChildren<GraphicRaycaster>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
      _outline = GetComponent<SpriteOutline>();
      _spireRender = GetComponent<SpriteRenderer>();
   }

   private void Update () {
      if (_graphicRaycaster != null) {
         _graphicRaycaster.gameObject.SetActive(!PanelManager.self.hasPanelInStack());
      }
      if (oreLife < _oreMaxLife) {
         // Only show our outline when the mouse is over the object
         handleSpriteOutline();

         // Allow pressing keyboard to interact
         if (InputManager.isActionKeyPressed() && Global.player != null && isCloseToGlobalPlayer()) {
            clientClickedMe();
         }
      }
   }

   public bool didUserInteract(int userID) {
         return userIds.Contains(userID);
   }

   public bool finishedMining(int oreLife) {
      if (oreLife >= _oreMaxLife - 1)
         return true;
      return false;
   }

   public void setOreData (int id, Area.Type area, OreData oreData) {
      // Initializes ore data setup by the OreArea
      oreID = id;
      oreArea = area;
      this.oreData = oreData;

      // Setup default sprite
      _spireRender.sprite = oreData.miningDurabilityIcon[0];
      _spireRender.enabled = true;

      // Life setup of the ore and interaction availability
      oreLife = 0;
      _oreMaxLife = oreData.miningDurabilityIcon.Count;
      isActive = true;
   }

   public void clientClickedMe () {
      if (isActive == false || Global.player == null || _clickableBox == null || Global.isInBattle()) {
         return;
      }

      if(didUserInteract(Global.player.userId)) {
         return;
      }

      // Only works when the player is close enough
      if (Vector2.Distance(this.transform.position, Global.player.transform.position) > MINING_DISTANCE) {
         Instantiate(PrefabsManager.self.tooFarPrefab, this.transform.position + new Vector3(0f, .24f), Quaternion.identity);
         return;
      }
      updateOreLife();
   }

   public bool isCloseToGlobalPlayer () {
      if (Global.player == null) {
         return false;
      }

      return (Vector2.Distance(Global.player.transform.position, this.transform.position) < MINING_DISTANCE);
   }

   public void receiveOreUpdate () {
      // Play animation
      miningAnimation.Play("mine");

      // Handles spamming
      _isMining = false;

      // Iterates the sprite index
      _spireRender.sprite = oreData.miningDurabilityIcon[oreLife+1];

      // Iterates through the ore life
      oreLife++;

      // Disables the ore if life is depleted
      if (oreLife >= _oreMaxLife - 1) {
         _outline.setVisibility(false);
         StartCoroutine(CO_previewReward());
         
         isActive = false;
      }
   }

   private void updateOreLife() {
      if (_isMining == false) {
         // Tell server to update data
         _isMining = true;
         //Global.player.rpc.Cmd_UpdateOreMining(oreID, (int)oreArea);
         Global.player.rpc.Cmd_UpdateOreMining(id, oreLife);
      }
   }

   public void rewardPlayer( ) {
      RewardManager.self.requestIngredient(oreData.ingredientReward);
   }

   protected void handleSpriteOutline () {
      if (_outline != null) {
         _outline.color = Color.white;
         _outline.setVisibility(MouseManager.self.isHoveringOver(_clickableBox));
      }
   }

   IEnumerator CO_previewReward() {
      yield return new WaitForSeconds(.5f);
      rewardPlayer();
   }

   #region Private Variables

   // Our clickable box
   private ClickableBox _clickableBox;

   // The Raycaster for our clickable canvas
   protected GraphicRaycaster _graphicRaycaster;

   // Our outline
   protected SpriteOutline _outline;

   // Object sprite
   protected SpriteRenderer _spireRender;

   // Max life before ore gets mined
   protected int _oreMaxLife;

   // A flag to determine if the ore is being mined
   protected bool _isMining;

   #endregion
}