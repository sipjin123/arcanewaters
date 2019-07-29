using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OreObj : MonoBehaviour {
   #region Public Variables

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

   #endregion

   public void SetOreDAta(int id, Area.Type area, OreData oreData) {
      // Initializes ore data setup by the OreArea
      oreID = id;
      oreArea = area;
      this.oreData = oreData;

      // Setup default sprite
      _spireRender.sprite = oreData.miningDurabilityIcon[0];

      // Life setup of the ore and interaction availability
      oreLife = 1;
      _oreMaxLife = oreData.miningDurabilityIcon.Count;
      isActive = true;
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

   public void clientClickedMe () {
      if (isActive == false || Global.player == null || _clickableBox == null || Global.isInBattle()) {
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

   public void receiveOreUpdate() {
      miningAnimation.Play("mine");

      // Handles spamming
      _isMining = false;

      // Iterates the sprite index
      _spireRender.sprite = oreData.miningDurabilityIcon[oreLife];

      // Reduces interaction count
      oreLife++;

      // Disables the ore if life is depleted
      if (oreLife >= _oreMaxLife) {
         _outline.setVisibility(false);
         StartCoroutine(previewReward());
         isActive = false;
         return;
      }
   }

   private void updateOreLife() {
      if (_isMining == false) {
         // Tell server to update data
         _isMining = true;
         Global.player.rpc.Cmd_UpdateOreMining(oreID, (int)oreArea);
      }
   }

   public void rewardPlayer( ) {
      CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) oreData.ingredientReward, ColorType.DarkGreen, ColorType.DarkPurple, "");
      craftingIngredients.itemTypeId = (int) craftingIngredients.type;
      Item item = craftingIngredients;

      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemData(item);
      PanelManager.self.pushPanel(Panel.Type.Reward);

      Global.player.rpc.Cmd_DirectAddItem(item);
   }

   protected void handleSpriteOutline () {
      if (_outline != null) {
         _outline.color = Color.white;
         _outline.setVisibility(MouseManager.self.isHoveringOver(_clickableBox));
      }
   }

   IEnumerator previewReward() {
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