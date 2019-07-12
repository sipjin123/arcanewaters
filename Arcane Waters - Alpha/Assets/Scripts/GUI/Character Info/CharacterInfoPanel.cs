using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class CharacterInfoPanel : Panel, IPointerClickHandler {
   #region Public Variables

   // Our character stack
   public CharacterStack characterStack;

   // The Guild Invite button
   public Button guildInviteButton;

   // The experience progress bar
   public Image levelProgressBar;

   // The class, faction, and specialty icons
   public Image classIcon;
   public Image specialtyIcon;
   public Image factionIcon;

   // The jobs progress bars
   public Image farmerProgressBar;
   public Image minerProgressBar;
   public Image explorerProgressBar;
   public Image sailorProgressBar;
   public Image traderProgressBar;
   public Image crafterProgressBar;

   // Our various texts
   public Text nameText;
   public Text levelText;
   public Text classText;
   public Text specialtyText;
   public Text factionText;
   public Text guildText;
   public Text strengthText;
   public Text dexterityText;
   public Text intelligenceText;
   public Text spiritText;
   public Text vitalityText;
   public Text luckText;
   public Text xpText;
   public Text farmerText;
   public Text minerText;
   public Text explorerText;
   public Text sailorText;
   public Text traderText;
   public Text crafterText;

   // Self
   public static CharacterInfoPanel self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public void receiveDataFromServer (UserObjects userObjects, Stats stats, Jobs jobs, string guildName) {
      _userObjects = userObjects;
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

      // Update the icons
      classIcon.sprite = ImageManager.getSprite("Icons/Classes/class_" +userObjects.userInfo.classType);
      specialtyIcon.sprite = ImageManager.getSprite("Icons/Specialties/specialty_" + userObjects.userInfo.specialty);
      factionIcon.sprite = ImageManager.getSprite("Icons/Factions/faction_" + userObjects.userInfo.faction);

      // Note the levels
      int farmerLevel = LevelUtil.levelForXp(jobs.farmerXP);
      int minerLevel = LevelUtil.levelForXp(jobs.minerXP);
      int explorerLevel = LevelUtil.levelForXp(jobs.explorerXP);
      int sailorLevel = LevelUtil.levelForXp(jobs.sailorXP);
      int traderLevel = LevelUtil.levelForXp(jobs.traderXP);
      int crafterLevel = LevelUtil.levelForXp(jobs.crafterXP);

      // Update the jobs progress bars
      farmerProgressBar.fillAmount = (float) LevelUtil.getProgressTowardsCurrentLevel(jobs.farmerXP) / (float) LevelUtil.xpForLevel(farmerLevel + 1);
      minerProgressBar.fillAmount = (float) LevelUtil.getProgressTowardsCurrentLevel(jobs.minerXP) / (float) LevelUtil.xpForLevel(minerLevel + 1);
      explorerProgressBar.fillAmount = (float) LevelUtil.getProgressTowardsCurrentLevel(jobs.explorerXP) / (float) LevelUtil.xpForLevel(explorerLevel + 1);
      sailorProgressBar.fillAmount = (float) LevelUtil.getProgressTowardsCurrentLevel(jobs.sailorXP) / (float) LevelUtil.xpForLevel(sailorLevel + 1);
      traderProgressBar.fillAmount = (float) LevelUtil.getProgressTowardsCurrentLevel(jobs.traderXP) / (float) LevelUtil.xpForLevel(traderLevel + 1);
      crafterProgressBar.fillAmount = (float) LevelUtil.getProgressTowardsCurrentLevel(jobs.crafterXP) / (float) LevelUtil.xpForLevel(crafterLevel + 1);

      // Fill in Texts
      nameText.text = info.username;
      levelText.text = "LVL " + currentLevel;
      classText.text = "" + info.classType;
      specialtyText.text = Specialty.toString(info.specialty);
      factionText.text = Faction.toString(info.faction);
      guildText.text = "Guild: " + guildName;
      strengthText.text = "Str: " + stats.strength;
      dexterityText.text = "Dex: " + stats.precision;
      intelligenceText.text = "Int: " + stats.intelligence;
      spiritText.text = "Spi: " + stats.spirit;
      vitalityText.text = "Vit: " + stats.vitality;
      luckText.text = "Luc: " + stats.luck;
      xpText.text = "EXP: " + LevelUtil.getProgressTowardsCurrentLevel(info.XP) + " / " + LevelUtil.xpForLevel(currentLevel + 1);
      farmerText.text = "" + farmerLevel;
      minerText.text = "" + minerLevel;
      explorerText.text = "" + explorerLevel;
      sailorText.text = "" + sailorLevel;
      traderText.text = "" + traderLevel;
      crafterText.text = "" + crafterLevel;
   }

   public void guildInviteButtonPressed () {
      Global.player.rpc.Cmd_InviteToGuild(_userObjects.userInfo.userId);
   }

   public void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the panel
      if (eventData.rawPointerPress == this.gameObject) {
         PanelManager.self.popPanel();
      }
   }

   #region Private Variables

   // The current user info
   protected UserObjects _userObjects;

   #endregion
}
