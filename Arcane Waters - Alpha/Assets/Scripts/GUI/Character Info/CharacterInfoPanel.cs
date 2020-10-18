using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;

public class CharacterInfoPanel : Panel {

   #region Public Variables

   // Our character stack
   public CharacterStack characterStack;

   [Header("Player Info")]
   // The Guild Invite button
   public Button guildInviteButton;

   // The experience progress bar
   public Image levelProgressBar;

   // Our various texts
   public Text nameText;
   public Text levelText;
   public Text guildText;
   public Text xpText;

   // The template for perks in the list
   [Header("Perks")]
   public PerkElementTemplate perkTemplate;

   // The parent for all the perks
   public Transform perkContainer;

   // The container grid layout
   public GridLayoutGroup perksGridLayoutGroup;

   // The unassigned perk points text
   public TextMeshProUGUI unassignedPerkPointsText;

   // The perk icon sprite borders
   public List<Sprite> perkIconBorders;

   // Self
   public static CharacterInfoPanel self;

   // Blocks the outdated info while waiting for server response
   public GameObject loadingBlocker;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public override void Start () {
      base.Start();
      Util.disableCanvasGroup(canvasGroup);
   }

   public void loadCharacterCache () {
      loadingBlocker.SetActive(true);

      // Load the character stack using the cached user info
      if (Global.getUserObjects() != null) {
         characterStack.gameObject.SetActive(true);
         characterStack.updateLayers(Global.getUserObjects());
      }
   }

   public void receivePerkData (List<PerkData> perkDataList) {
      perksGridLayoutGroup.enabled = true;
      perkContainer.gameObject.DestroyChildren();
      _perkIcons = new Dictionary<int, PerkElementTemplate>();

      _perkDataList = perkDataList;

      foreach (PerkData data in _perkDataList) {
         // Don't add unassigned points to the list 
         if ((Perk.Category) data.perkCategoryId == Perk.Category.None) {
            continue;
         }

         PerkElementTemplate template = Instantiate(perkTemplate, perkContainer);
         template.initializeData(data);
         _perkIcons.Add(data.perkId, template);
      }
   }

   public void receivePerkPoints (bool isLocalPlayer, Perk[] perks) {
      foreach (int perkId in _perkIcons.Keys) {         
         Perk perk = perks.FirstOrDefault(x => x.perkId == perkId);
         int points = perk != null ? perk.points : 0;

         _perkIcons[perkId].initializePoints(points, isLocalPlayer);
      }

      if (isLocalPlayer) {
         Perk perk = perks.FirstOrDefault(x => x.perkId == Perk.UNASSIGNED_ID);
         setAvailablePoints (perk != null ? perk.points : 0);
      }

      unassignedPerkPointsText.gameObject.SetActive(isLocalPlayer);
   }

   private void setAvailablePoints (int points) {
      unassignedPerkPointsText.SetText("Available Points: {0}", points);
   }

   public void receiveDataFromServer (UserObjects userObjects, Stats stats, Jobs jobs, string guildName, Perk[] perks) {
      if (userObjects.weapon.itemTypeId != 0) {
         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(userObjects.weapon.itemTypeId);
         userObjects.weapon.data = WeaponStatData.serializeWeaponStatData(weaponData);
      }
      if (userObjects.armor.itemTypeId != 0) {
         ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataByType(userObjects.armor.itemTypeId);
         userObjects.armor.data = ArmorStatData.serializeArmorStatData(armorData);
      }
      if (userObjects.hat.itemTypeId != 0) {
         HatStatData hatData = EquipmentXMLManager.self.getHatData(userObjects.hat.itemTypeId);
         userObjects.hat.data = HatStatData.serializeHatStatData(hatData);
      }

      _userObjects = userObjects;
      Global.lastUserGold = userObjects.userInfo.gold;
      Global.lastUserGems = userObjects.userInfo.gems;

      UserInfo info = userObjects.userInfo;
      int currentLevel = LevelUtil.levelForXp(info.XP);

      // Update the character stack and the gold and gems
      characterStack.updateLayers(userObjects);

      // Only show the guild invite button in certain situations
      guildInviteButton.gameObject.SetActive(false);
      if (info.userId != Global.player.userId && Global.player.guildId != 0 && info.guildId == 0) {
         guildInviteButton.gameObject.SetActive(true);
      }

      // Update the fill on the level progress bar
      levelProgressBar.fillAmount = (float) LevelUtil.getProgressTowardsCurrentLevel(info.XP) / (float) LevelUtil.xpForLevel(currentLevel + 1);

      // Fill in Texts
      nameText.text = info.username;
      levelText.text = "LVL " + currentLevel;
      guildText.text = "Guild: " + guildName;
      xpText.text = "EXP: " + LevelUtil.getProgressTowardsCurrentLevel(info.XP) + " / " + LevelUtil.xpForLevel(currentLevel + 1);

      receivePerkPoints(Global.player != null && userObjects.userInfo.userId == Global.player.userId, perks);
      loadingBlocker.SetActive(false);
   }

   public void guildInviteButtonPressed () {
      Global.player.rpc.Cmd_InviteToGuild(_userObjects.userInfo.userId);
   }

   public override void hide () {
      base.hide();
      canvasGroup.blocksRaycasts = false;

      // Disable any tooltip we may have enabled and reenable automatic tooltips
      TooltipManager.self.hideTooltip(true);
   }

   public override void show () {
      base.show();
      canvasGroup.blocksRaycasts = true;

      // Temporarily disable the automatic tooltips
      TooltipManager.self.isAutomaticTooltipEnabled = false;

      // Reenable the grid layout group in case it was disabled
      perksGridLayoutGroup.enabled = true;
   }

   #region Private Variables

   // The current user info
   protected UserObjects _userObjects;

   // The list of all the perks
   protected List<PerkData> _perkDataList;

   // The panel perk icons 
   protected Dictionary<int, PerkElementTemplate> _perkIcons = new Dictionary<int, PerkElementTemplate>();

   #endregion
}
