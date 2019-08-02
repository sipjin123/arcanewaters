using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OreObj : NetworkBehaviour
{
   #region Public Variables

   // The unique ID assigned to this ore
   [SyncVar]
   public int areaID;

   // The unique ID assigned to this ore
   [SyncVar]
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

   // Determines if the instantiated obj is registered to an ore area
   public bool hasParentList;

   #endregion

   private void Awake () {
      // Component setup
      _graphicRaycaster = GetComponentInChildren<GraphicRaycaster>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
      _outline = GetComponent<SpriteOutline>();
      _spireRender = GetComponent<SpriteRenderer>();
   }

   private void Update () {
      if(hasParentList == false) {
         // Manual waiting for Sync vars to setup before registering each ore obj to their respective ore areas
         if(areaID != 0) {
            hasParentList = true;
            Area area = AreaManager.self.getArea((Area.Type) areaID);
            OreArea newOreArea = area.GetComponent<OreArea>();
            transform.SetParent(newOreArea.oreObjHolder);

            // Register ore to their respective ore areas
            newOreArea.registerNetworkOre(this);

            // Registers network object to manager list
            OreManager.self.registerOreObj(id, this);

            // Disables sprite if mining is complete
            if (didUserInteract(Global.player.userId)) {
               int lastIcon = oreData.miningDurabilityIcon.Count - 1;
               _spireRender.sprite = oreData.miningDurabilityIcon[lastIcon];
            }
         }
      }

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
      if (oreLife >= _oreMaxLife - 1) {
         return true;
      }
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
      if (isActive == false || Global.player == null || _clickableBox == null || Global.isInBattle() || didUserInteract(Global.player.userId)) {
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

      // Iterates through the ore life
      oreLife++;

      // Iterates the sprite index
      _spireRender.sprite = oreData.miningDurabilityIcon[oreLife];

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
         Global.player.rpc.Cmd_UpdateOreMining(id, oreLife);
      }
   }

   protected void handleSpriteOutline () {
      if (_outline != null) {
         _outline.color = Color.white;
         _outline.setVisibility(MouseManager.self.isHoveringOver(_clickableBox));
      }
   }

   IEnumerator CO_previewReward() {
      yield return new WaitForSeconds(.5f);
      Global.player.rpc.Cmd_MinedOre(oreData.oreType);
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