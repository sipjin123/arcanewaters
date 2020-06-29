using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;
using UnityEngine.Events;

public class SeaMonsterDataPanel : MonoBehaviour
{
   #region Public Variables

   // The generic popup reference
   public GenericSelectionPopup genericPopup;

   // References the tool manager
   public SeaMonsterToolManager monsterToolManager;

   // Reference to the current XML Template being modified
   public SeaMonsterDataTemplate currentXMLTemplate;

   // Button for closing panel
   public Button revertButton;

   // Button for updating the new data
   public Button saveExitButton;
   
   // The selection panel for monster types
   public GameObject selectionPanel;

   // Caches the initial type incase it is changed
   public string startingName;
   
   // The avatar icon of the enemy
   public Image avatarIcon;

   // The button that toggles the avatar selection
   public Button closeAvatarSelectionButton;

   // Button that opens the selection panel
   public Button openAvatarSelectionButton;

   // Previews the sprite selection
   public Image previewSelectionIcon;

   // The cache list for icon selection
   public Dictionary<string, Sprite> iconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> skillIconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> castIconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> hitIconSpriteList = new Dictionary<string, Sprite>();

   public Sprite emptySprite;

   // Audio source to play the sample clips
   public AudioSource audioSource;

   // Image Selection
   public GameObject imageTemplateParent;
   public GameObject imageTemplate;

   // Sprite Preview
   public GameObject previewPanel;
   public Button previewMonster, closePreview;
   public SeaMonsterDisplay seaMonsterDisplay;

   // Skill features
   public Transform skillHolder, skillOptionHolder;
   public GameObject skillSelectionPanel;
   public ShipSkillTemplate skillTemplate;
   public Button addSkillButton;
   public Button closeSkillSelectionButton;

   public enum DirectoryType
   {
      AvatarSprite = 0,
      PrimarySprite = 1,
      SecondarySprite = 2,
      RippleSprite = 3,
      RippleTexture = 4,
      CorpseSprite = 5
   }

   // Monster Loots
   public GameObject lootSelectionPanel;
   public GameObject lootTemplateParent;
   public GameObject itemCategoryParent, itemTypeParent;
   public ItemCategoryTemplate itemCategoryTemplate;
   public ItemTypeTemplate itemTypeTemplate;
   public Button confirmItemButton, closeItemPanelButton;
   public Item.Category selectedCategory;
   public string itemDataSelected;
   public int itemTypeIDSelected;
   public InputField rewardItemMin, rewardItemMax;

   // Togglers
   public Button toggleMainStats, toggleSubStats1, toggleSubStats2, toggleLoots;
   public GameObject mainStatsContent, subStats1Content, subStats2Content, lootsContent;
   public GameObject mainStatsDropdown, subStats1Dropdown, subStats2Dropdown, lootsDropdown;

   // Spawn points feature
   public Button addSpawnPoint;
   public ProjectileSpawnRow projectileSpawnRow;
   public Transform projectileSpawnParent;
   public List<ProjectileSpawnRow> projectileSpawnRowList = new List<ProjectileSpawnRow>();
   public GameObject duplicateWarning;

   // Warning feature
   public GameObject warningPanel;
   public Text warningText;

   // Reference to current xml id
   public int currentXmlId;

   // Toggler to determine if this sql data is active in the database
   public Toggle xml_toggler;

   // Loot group UI requirements
   public Button lootGroupButton;
   public Text lootGroupText, lootGroupIndexText;
   int lootGroupIdSelected = 0;
   public Transform lootGroupParent;
   public GameObject lootGroupPrefab;
   public UnityEvent lootGroupSelected = new UnityEvent();

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         saveExitButton.gameObject.SetActive(false);
      }

      previewPanel.SetActive(false);
      revertButton.onClick.AddListener(() => {
         monsterToolManager.loadAllDataFiles();
         gameObject.SetActive(false);
      });

      lootGroupButton.onClick.AddListener(() => {
         lootGroupSelected.AddListener(() => {
            lootGroupIdSelected = int.Parse(lootGroupIndexText.text);
            loadLootGroupById(lootGroupIdSelected);
         });
         genericPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.LootGroupsSeaMonsters, lootGroupIndexText, lootGroupSelected);
      });

      previewMonster.onClick.AddListener(() => {
         SeaMonsterEntityData dataCopy = getSeaMonsterData();

         if (dataCopy.animGroup != Anim.Group.None && 
         dataCopy.defaultSpritePath != "" &&
         dataCopy.defaultRippleSpritePath != "" &&
         dataCopy.avatarSpritePath != "") {
            seaMonsterDisplay.setData(dataCopy);
            previewPanel.SetActive(true);
         } else {
            StartCoroutine(CO_ShowWarning("Insufficient Data"));
         }
      });
      closePreview.onClick.AddListener(() => {
         seaMonsterDisplay.closePanel();
         previewPanel.SetActive(false);
      });

      addSpawnPoint.onClick.AddListener(() => addProjectileSpawnRow());
      closeAvatarSelectionButton.onClick.AddListener(() => selectionPanel.SetActive(false));
      saveExitButton.onClick.AddListener(() => saveData());
      seaMonsterTypeButton.onClick.AddListener(() => openTypeSelectionPanel());
      openAvatarSelectionButton.onClick.AddListener(() => openImageSelectionPanel(DirectoryType.AvatarSprite));
      defaultSpriteButton.onClick.AddListener(() => openImageSelectionPanel(DirectoryType.PrimarySprite));
      secondarySpriteButton.onClick.AddListener(() => openImageSelectionPanel(DirectoryType.SecondarySprite));
      corpseSpriteButton.onClick.AddListener(() => openImageSelectionPanel(DirectoryType.CorpseSprite));
      defaultRippleTextureButton.onClick.AddListener(() => openImageSelectionPanel(DirectoryType.RippleTexture));
      defaultRippleSpriteButton.onClick.AddListener(() => openImageSelectionPanel(DirectoryType.RippleSprite));
      closeItemPanelButton.onClick.AddListener(() => {
         lootSelectionPanel.SetActive(false);
      });

      toggleMainStats.onClick.AddListener(() => {
         mainStatsContent.SetActive(!mainStatsContent.activeSelf);
         mainStatsDropdown.SetActive(!mainStatsContent.activeSelf);
      });
      toggleSubStats1.onClick.AddListener(() => {
         subStats1Content.SetActive(!subStats1Content.activeSelf);
         subStats1Dropdown.SetActive(!subStats1Content.activeSelf);
      });
      toggleSubStats2.onClick.AddListener(() => {
         subStats2Content.SetActive(!subStats2Content.activeSelf);
         subStats2Dropdown.SetActive(!subStats2Content.activeSelf);
      });
      toggleLoots.onClick.AddListener(() => {
         lootsContent.SetActive(!lootsContent.activeSelf);
         lootsDropdown.SetActive(!lootsContent.activeSelf);
      });

      roleType.maxValue = Enum.GetValues(typeof(RoleType)).Length - 1;
      roleType.onValueChanged.AddListener(_ => {
         roleTypeText.text = ((RoleType) roleType.value).ToString() + countSliderValue(roleType);
      });

      attackType.maxValue = Enum.GetValues(typeof(Attack.Type)).Length - 1;
      attackType.onValueChanged.AddListener(_ => {
         attackTypeText.text = ((Attack.Type) attackType.value).ToString() + countSliderValue(attackType);
      });

      animGroup.maxValue = Enum.GetValues(typeof(Anim.Group)).Length - 1;
      animGroup.onValueChanged.AddListener(_ => {
         animGroupText.text = ((Anim.Group) animGroup.value).ToString() + countSliderValue(animGroup);
      });

      closeSkillSelectionButton.onClick.AddListener(() => skillSelectionPanel.SetActive(false));
      addSkillButton.onClick.AddListener(() => {
         skillSelectionPanel.SetActive(true);

         skillOptionHolder.gameObject.DestroyChildren();
         foreach (ShipAbilityPair skillOption in monsterToolManager.shipSkillList) {
            if (!existingInInventory(skillOption.abilityId)) {
               ShipSkillTemplate skillOptionTemp = Instantiate(skillTemplate, skillOptionHolder.transform);
               skillOptionTemp.skillNameText.text = skillOption.abilityName;
               skillOptionTemp.skillIdText.text = skillOption.abilityId.ToString();

               skillOptionTemp.deleteButton.gameObject.SetActive(false);
               skillOptionTemp.selectButton.gameObject.SetActive(true);

               skillOptionTemp.selectButton.onClick.AddListener(() => {
                  skillSelectionPanel.SetActive(false);
                  ShipSkillTemplate skillTemp = Instantiate(skillTemplate, skillHolder.transform);
                  skillTemp.skillNameText.text = skillOption.abilityName;
                  skillTemp.skillIdText.text = skillOption.abilityId.ToString();
                  skillTemp.deleteButton.onClick.AddListener(() => {
                     Destroy(skillTemp.gameObject);
                  });
               });
            }
         }
      });
   }

   private bool existingInInventory (int id) {
      List<int> cachedAbilityList = new List<int>();
      foreach (Transform child in skillHolder) {
         int skillId = int.Parse(child.GetComponent<ShipSkillTemplate>().skillIdText.text);
         cachedAbilityList.Add(skillId);
      }
      return cachedAbilityList.Exists(_ => _ == id);
   }

   #region Save and Load Data

   public void loadData (SeaMonsterEntityData seaMonsterData, int xml_id, bool isEnabled) {
      xml_toggler.isOn = isEnabled;
      currentXmlId = xml_id;
      startingName = seaMonsterData.monsterName;
      monsterName.text = seaMonsterData.monsterName;
      seaMonsterType.text = seaMonsterData.seaMonsterType.ToString();
      isAggressive.isOn = seaMonsterData.isAggressive;
      autoMove.isOn = seaMonsterData.autoMove;
      isMelee.isOn = seaMonsterData.isMelee;
      isRanged.isOn = seaMonsterData.isRanged;
      shouldDropTreasure.isOn = seaMonsterData.shouldDropTreasure;
      isInvulnerable.isOn = seaMonsterData.isInvulnerable;
      maxProjectileDistanceGap.text = seaMonsterData.maxProjectileDistanceGap.ToString();
      maxMeleeDistanceGap.text = seaMonsterData.maxMeleeDistanceGap.ToString();
      maxDistanceGap.text = seaMonsterData.maxDistanceGap.ToString();
      attackType.value = (int) seaMonsterData.attackType;
      territoryRadius.text = seaMonsterData.territoryRadius.ToString();
      detectRadius.text = seaMonsterData.detectRadius.ToString();
      attackFrequency.text = seaMonsterData.attackFrequency.ToString();
      reloadDelay.text = seaMonsterData.reloadDelay.ToString();
      moveFrequency.text = seaMonsterData.moveFrequency.ToString();
      findTargetsFrequency.text = seaMonsterData.findTargetsFrequency.ToString();
      defaultSpritePath.text = seaMonsterData.defaultSpritePath;
      secondarySpritePath.text = seaMonsterData.secondarySpritePath;
      defaultRippleTexturePath.text = seaMonsterData.defaultRippleTexturePath;
      defaultRippleSpritePath.text = seaMonsterData.defaultRippleSpritePath;
      corpseSpritePath.text = seaMonsterData.corpseSpritePath;
      scaleOverride.text = seaMonsterData.scaleOverride.ToString();
      outlineScaleOverride.text = seaMonsterData.outlineScaleOverride.ToString();
      rippleScaleOverride.text = seaMonsterData.rippleScaleOverride.ToString();
      maxHealth.text = seaMonsterData.maxHealth.ToString();
      animGroup.value = (int) seaMonsterData.animGroup;
      roleType.value = (int) seaMonsterData.roleType;
      animationSpeedOverride.text = seaMonsterData.animationSpeedOverride.ToString();
      rippleAnimationSpeedOverride.text = seaMonsterData.rippleAnimationSpeedOverride.ToString();
      avatarIconPath.text = seaMonsterData.avatarSpritePath;
      rippleOffsetX.text = seaMonsterData.rippleLocOffset.x.ToString();
      rippleOffsetY.text = seaMonsterData.rippleLocOffset.y.ToString();

      animGroup.onValueChanged.Invoke(animGroup.value);
      roleType.onValueChanged.Invoke(roleType.value);
      attackType.onValueChanged.Invoke(attackType.value);

      lootGroupIdSelected = seaMonsterData.lootGroupId;
      if (lootGroupIdSelected > 0) {
         loadLootGroupById(lootGroupIdSelected);
      } else {
         lootGroupText.text = "";
         lootGroupParent.gameObject.DestroyChildren();
      }

      if (seaMonsterData.defaultSpritePath != null) {
         defaultSprite.sprite = ImageManager.getSprite(seaMonsterData.defaultSpritePath);
      }
      if (seaMonsterData.secondarySpritePath != null ) {
         secondarySprite.sprite = ImageManager.getSprite(seaMonsterData.secondarySpritePath);
      }
      if (seaMonsterData.defaultRippleSpritePath != null) {
         defaultRippleSprite.sprite = ImageManager.getSprite(seaMonsterData.defaultRippleSpritePath);
      }
      if (seaMonsterData.defaultRippleTexturePath != null) {
         defaultRippleTexture.sprite = ImageManager.getSprite(seaMonsterData.defaultRippleTexturePath);
      }
      if (seaMonsterData.corpseSpritePath != null) {
         corpseSprite.sprite = ImageManager.getSprite(seaMonsterData.corpseSpritePath);
      }
      if (seaMonsterData.avatarSpritePath != null) {
         avatarIcon.sprite = ImageManager.getSprite(seaMonsterData.avatarSpritePath);
      }

      // Load all the skills of the sea monster
      skillHolder.gameObject.DestroyChildren();
      foreach (int skillId in seaMonsterData.skillIdList) {
         ShipAbilityPair skillData = SeaMonsterToolManager.instance.shipSkillList.Find(_ => _.abilityId == skillId);
         if (skillData != null) {
            ShipSkillTemplate skillTemp = Instantiate(skillTemplate.gameObject, skillHolder).GetComponent<ShipSkillTemplate>();
            skillTemp.skillNameText.text = skillData.abilityName;
            skillTemp.skillIdText.text = skillData.abilityId.ToString();
            skillTemp.deleteButton.onClick.AddListener(()=> {
               Destroy(skillTemp.gameObject);
            });
         }
      }

      loadProjectileSpawnRow(seaMonsterData);
   }

   private SeaMonsterEntityData getSeaMonsterData () {
      SeaMonsterEntityData seaMonsterData = new SeaMonsterEntityData();

      seaMonsterData.monsterName = monsterName.text;
      seaMonsterData.seaMonsterType = (SeaMonsterEntity.Type) Enum.Parse(typeof(SeaMonsterEntity.Type), seaMonsterType.text);
      seaMonsterData.isAggressive = isAggressive.isOn;

      seaMonsterData.autoMove = autoMove.isOn;
      seaMonsterData.isMelee = isMelee.isOn;
      seaMonsterData.isRanged = isRanged.isOn;
      seaMonsterData.shouldDropTreasure = shouldDropTreasure.isOn;
      seaMonsterData.isInvulnerable = isInvulnerable.isOn;

      seaMonsterData.maxProjectileDistanceGap = float.Parse(maxProjectileDistanceGap.text);
      seaMonsterData.maxMeleeDistanceGap = float.Parse(maxMeleeDistanceGap.text);
      seaMonsterData.maxDistanceGap = float.Parse(maxDistanceGap.text);
      seaMonsterData.attackType = (Attack.Type) attackType.value;

      seaMonsterData.territoryRadius = float.Parse(territoryRadius.text);
      seaMonsterData.detectRadius = float.Parse(detectRadius.text);
      seaMonsterData.attackFrequency = float.Parse(attackFrequency.text);
      seaMonsterData.reloadDelay = float.Parse(reloadDelay.text);
      seaMonsterData.moveFrequency = float.Parse(moveFrequency.text);
      seaMonsterData.findTargetsFrequency = float.Parse(findTargetsFrequency.text);

      seaMonsterData.defaultSpritePath = defaultSpritePath.text; 
      seaMonsterData.secondarySpritePath = secondarySpritePath.text;
      seaMonsterData.defaultRippleTexturePath = defaultRippleTexturePath.text;
      seaMonsterData.defaultRippleSpritePath = defaultRippleSpritePath.text;
      seaMonsterData.corpseSpritePath = corpseSpritePath.text;
      seaMonsterData.avatarSpritePath = avatarIconPath.text;

      seaMonsterData.scaleOverride = float.Parse(scaleOverride.text);
      seaMonsterData.outlineScaleOverride = float.Parse(outlineScaleOverride.text);
      seaMonsterData.rippleScaleOverride = float.Parse(rippleScaleOverride.text);
      seaMonsterData.maxHealth = int.Parse(maxHealth.text);
      seaMonsterData.animGroup = (Anim.Group) animGroup.value;
      seaMonsterData.roleType = (RoleType) roleType.value;
      seaMonsterData.animationSpeedOverride = float.Parse(animationSpeedOverride.text);
      seaMonsterData.rippleAnimationSpeedOverride = float.Parse(rippleAnimationSpeedOverride.text);
      seaMonsterData.rippleLocOffset = new Vector3(float.Parse(rippleOffsetX.text), float.Parse(rippleOffsetY.text), 0);

      seaMonsterData.lootGroupId = lootGroupIdSelected;

      if (projectileSpawnRowList.Count > 0) {
         seaMonsterData.projectileSpawnLocations = new List<DirectionalPositions>();
         foreach (ProjectileSpawnRow row in projectileSpawnRowList) {
            DirectionalPositions newPos = new DirectionalPositions();
            newPos.direction = (Direction) row.directionSlider.value;
            float x = float.Parse(row.xValue.text);
            float y = float.Parse(row.yValue.text);
            float z = float.Parse(row.zValue.text);
            newPos.spawnTransform = new Vector3(x, y, z);
            seaMonsterData.projectileSpawnLocations.Add(newPos);
         }
      }

      seaMonsterData.skillIdList = new List<int>();
      foreach (Transform abilityTemplate in skillHolder.transform) {
         int skillId = int.Parse(abilityTemplate.GetComponent<ShipSkillTemplate>().skillIdText.text);
         seaMonsterData.skillIdList.Add(skillId);
      }

      return seaMonsterData;
   }

   public void saveData () {
      SeaMonsterEntityData rawData = getSeaMonsterData();

      monsterToolManager.saveDataToFile(rawData, currentXmlId, xml_toggler.isOn);
      gameObject.SetActive(false);
   }

   #endregion

   private void loadLootGroupById (int groupId) {
      LootGroupData lootGroupData = monsterToolManager.lootGroupDataCollection[groupId];
      lootGroupText.text = lootGroupData.lootGroupName;

      lootGroupParent.gameObject.DestroyChildren();
      foreach (TreasureDropsData lootGroupItemData in lootGroupData.treasureDropsCollection) {
         TreasureDropsItemTemplate itemTemplate = Instantiate(lootGroupPrefab, lootGroupParent).GetComponent<TreasureDropsItemTemplate>();
         Item cachedItem = lootGroupItemData.item;

         itemTemplate.dropChance.text = lootGroupItemData.spawnChance.ToString();
         itemTemplate.item = cachedItem;
         itemTemplate.itemIcon.sprite = ImageManager.getSprite(cachedItem.iconPath);
         itemTemplate.itemName.text = cachedItem.itemName;
         itemTemplate.itemType.text = cachedItem.category == Item.Category.CraftingIngredients ? "Material" : cachedItem.category.ToString();
      }
   }

   #region Stats Feature

   private void openImageSelectionPanel (DirectoryType directoryType) {
      selectionPanel.SetActive(true);
      imageTemplateParent.DestroyChildren();

      previewSelectionIcon.sprite = emptySprite;
      foreach (KeyValuePair<string, Sprite> sourceSprite in iconSpriteList) {
         GameObject iconTempObj = Instantiate(imageTemplate.gameObject, imageTemplateParent.transform);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.spriteIcon.sprite = sourceSprite.Value;
         iconTemp.itemTypeText.text = sourceSprite.Value.name;
         iconTemp.previewButton.onClick.AddListener(() => {
            previewSelectionIcon.sprite = sourceSprite.Value;
         });
         iconTemp.selectButton.onClick.AddListener(() => {
            switch (directoryType) {
               case DirectoryType.AvatarSprite:
                  avatarIconPath.text = sourceSprite.Key;
                  avatarIcon.sprite = sourceSprite.Value;
                  break;
               case DirectoryType.PrimarySprite:
                  defaultSpritePath.text = sourceSprite.Key;
                  defaultSprite.sprite = sourceSprite.Value;
                  break;
               case DirectoryType.SecondarySprite:
                  secondarySpritePath.text = sourceSprite.Key;
                  secondarySprite.sprite = sourceSprite.Value;
                  break;
               case DirectoryType.RippleTexture:
                  defaultRippleTexturePath.text = sourceSprite.Key;
                  defaultRippleTexture.sprite = sourceSprite.Value;
                  break;
               case DirectoryType.RippleSprite:
                  defaultRippleSpritePath.text = sourceSprite.Key;
                  defaultRippleSprite.sprite = sourceSprite.Value;
                  break;
               case DirectoryType.CorpseSprite:
                  corpseSpritePath.text = sourceSprite.Key;
                  corpseSprite.sprite = sourceSprite.Value;
                  break;
            }
            closeAvatarSelectionButton.onClick.Invoke();
         });
      }
   }

   private void openTypeSelectionPanel () {
      selectionPanel.SetActive(true);
      imageTemplateParent.DestroyChildren();

      previewSelectionIcon.sprite = emptySprite;
      foreach (SeaMonsterEntity.Type enemyType in Enum.GetValues(typeof(SeaMonsterEntity.Type))) {
         GameObject iconTempObj = Instantiate(imageTemplate.gameObject, imageTemplateParent.transform);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.itemTypeText.text = enemyType.ToString();
         iconTemp.selectButton.onClick.AddListener(() => {
            seaMonsterType.text = enemyType.ToString();
            closeAvatarSelectionButton.onClick.Invoke();
         });
      }
   }

   #endregion
   
   public void addProjectileSpawnRow() {
      GameObject obj = Instantiate(projectileSpawnRow.gameObject, projectileSpawnParent);
      ProjectileSpawnRow row = obj.GetComponent<ProjectileSpawnRow>();
      row.initData();
      row.deleteButton.onClick.AddListener(() => {
         projectileSpawnRowList.Remove(row);
         Destroy(obj);
      });
      row.directionSlider.onValueChanged.AddListener(_ => {
         checkValidity();
      });
      row.directionSlider.onValueChanged.Invoke(row.directionSlider.value);
      projectileSpawnRowList.Add(row);
   }

   public void checkValidity () {
      duplicateWarning.SetActive(false);
      List<Direction> directionList = new List<Direction>();
      foreach (ProjectileSpawnRow row in projectileSpawnRowList) {
         Direction currDirection = (Direction) row.directionSlider.value;
         if (directionList.Exists(_=>_ == currDirection)) {
            duplicateWarning.SetActive(true);
         } else {
            directionList.Add(currDirection);
         }
      }
   }

   public void loadProjectileSpawnRow (SeaMonsterEntityData data) {
      projectileSpawnParent.gameObject.DestroyChildren();
      projectileSpawnRowList = new List<ProjectileSpawnRow>();

      if (data.projectileSpawnLocations != null) {
         if (data.projectileSpawnLocations.Count > 0) {
            foreach (DirectionalPositions directionalPos in data.projectileSpawnLocations) {
               GameObject obj = Instantiate(projectileSpawnRow.gameObject, projectileSpawnParent);
               ProjectileSpawnRow row = obj.GetComponent<ProjectileSpawnRow>();
               row.initData();
               row.xValue.text = directionalPos.spawnTransform.x.ToString();
               row.yValue.text = directionalPos.spawnTransform.y.ToString();
               row.zValue.text = directionalPos.spawnTransform.z.ToString();
               row.directionSlider.value = (int) directionalPos.direction;
               row.directionSlider.onValueChanged.AddListener(_ => {
                  checkValidity();
               });
               row.deleteButton.onClick.AddListener(() => {
                  projectileSpawnRowList.Remove(row);
                  Destroy(obj);
               });
               row.directionSlider.onValueChanged.Invoke(row.directionSlider.value);
               projectileSpawnRowList.Add(row);
            }
         }
      }
   }

   private string countSliderValue (Slider slider) {
      return " ( " + slider.value + " / " + slider.maxValue + " )";
   }

   private IEnumerator CO_ShowWarning (string warningMsg) {
      warningText.text = warningMsg;
      warningPanel.SetActive(true);
      yield return new WaitForSeconds(2);
      warningPanel.SetActive(false);
   }

   #region Private Variables

#pragma warning disable 0649 

   // Main parameters
   [SerializeField, Header("Stats")] private InputField monsterName;
   [SerializeField] private Text seaMonsterType;
   [SerializeField] private Button seaMonsterTypeButton;
   [SerializeField] private Toggle isAggressive;
   [SerializeField] private Toggle autoMove;
   [SerializeField] private Toggle isMelee;
   [SerializeField] private Toggle isRanged;
   [SerializeField] private Toggle shouldDropTreasure;
   [SerializeField] private Toggle isInvulnerable;
   [SerializeField] private InputField maxProjectileDistanceGap;
   [SerializeField] private InputField maxMeleeDistanceGap;
   [SerializeField] private InputField maxDistanceGap;
   [SerializeField] private Slider attackType;
   [SerializeField] private Text attackTypeText;
   [SerializeField] private InputField territoryRadius;
   [SerializeField] private InputField detectRadius;
   [SerializeField] private InputField attackFrequency;
   [SerializeField] private InputField reloadDelay;
   [SerializeField] private InputField moveFrequency;
   [SerializeField] private InputField findTargetsFrequency;
   [SerializeField] private Text defaultSpritePath;
   [SerializeField] private Image defaultSprite;
   [SerializeField] private Button defaultSpriteButton;
   [SerializeField] private Text secondarySpritePath;
   [SerializeField] private Image secondarySprite;
   [SerializeField] private Button secondarySpriteButton;
   [SerializeField] private Text avatarIconPath;
   [SerializeField] private Text defaultRippleTexturePath;
   [SerializeField] private Image defaultRippleTexture;
   [SerializeField] private Button defaultRippleTextureButton;
   [SerializeField] private Text defaultRippleSpritePath;
   [SerializeField] private Image defaultRippleSprite;
   [SerializeField] private Button defaultRippleSpriteButton;
   [SerializeField] private Text corpseSpritePath;
   [SerializeField] private Image corpseSprite;
   [SerializeField] private Button corpseSpriteButton;
   [SerializeField] private InputField scaleOverride;
   [SerializeField] private InputField outlineScaleOverride;
   [SerializeField] private InputField rippleScaleOverride;
   [SerializeField] private InputField maxHealth;
   [SerializeField] private Slider animGroup;
   [SerializeField] private Text animGroupText;
   [SerializeField] private Slider roleType;
   [SerializeField] private Text roleTypeText;
   [SerializeField] private InputField animationSpeedOverride;
   [SerializeField] private InputField rippleAnimationSpeedOverride;
   [SerializeField] private InputField rippleOffsetX, rippleOffsetY;

#pragma warning restore 0649 

   #endregion
}
