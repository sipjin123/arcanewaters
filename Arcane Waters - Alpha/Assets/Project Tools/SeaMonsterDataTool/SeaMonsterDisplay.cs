using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class SeaMonsterDisplay : MonoBehaviour {
   #region Public Variables

   // Reference for seamonster panel
   public SeaMonsterDataPanel seaMonsterPanel;

   // Sprite related variables
   public SpriteRenderer defaultSprite;
   public SpriteRenderer outlineSprite;
   public SpriteRenderer rippleSprite;
   public SimpleAnimation simpleAnim, rippleSimpleAnim;

   // UI Notification and controls
   public Slider animSlider;
   public Text animText;
   public GameObject warningNotSupported;
   public GameObject spawnPointParent;

   // Toggle options
   public Button toggleSprites;
   public Button toggleSpawnPoints;
   public Button toggleFlip;

   // Data cache
   public SeaMonsterEntityDataCopy currentData;

   // Spawn Indicator UI 
   public GameObject spawnPointIcon;
   public GameObject spawnPointIconParent;

   // Sliders for coordinates
   public Slider ySlider, xSlider;
   public Text ySliderText, xSliderText;

   // Button to save coordinates
   public Button saveCoordinates;

   // Transform Indicator World
   public GameObject overrideTransformPrefab;
   public GameObject overrideTransformParent;

   // The name of the selected coordinate
   public Text coordName;

   // Panel holding the ui for spawn data alteration
   public GameObject spawnUIPane;

   #endregion

   private void Awake () {
      toggleSpawnPoints.onClick.AddListener(() => {
         spawnUIPane.SetActive(!spawnUIPane.activeSelf);
         if (currentData.projectileSpawnLocations == null) {
            spawnPointParent.SetActive(spawnUIPane.activeSelf);
         } else {
            if (currentData.projectileSpawnLocations.Count > 0) {
               spawnPointParent.SetActive(false);
            } else {
               spawnPointParent.SetActive(spawnUIPane.activeSelf);
            }
         }
         spawnPointIconParent.SetActive(spawnUIPane.activeSelf);
         overrideTransformParent.SetActive(spawnUIPane.activeSelf);
      });

      toggleFlip.onClick.AddListener(() => {
         defaultSprite.flipX = !defaultSprite.flipX;
         rippleSprite.flipX = !rippleSprite.flipX;
      });

      saveCoordinates.onClick.AddListener(() => saveData());

      toggleSprites.onClick.AddListener(() => {
         _isPrimarySprite = !_isPrimarySprite;
         if (_isPrimarySprite) {
            Sprite newSprite = ImageManager.getSprite(currentData.defaultSpritePath);
            if (newSprite != null) {
               defaultSprite.sprite = newSprite;
            }
         } else {
            Sprite newSprite = ImageManager.getSprite(currentData.secondarySpritePath);
            if (newSprite != null) {
               defaultSprite.sprite = newSprite;
            }
         }
      });
   }

   public void saveData() {
      seaMonsterPanel.loadProjectileSpawnRow(currentData);
   }

   public void closePanel() {
      spawnPointIconParent.DestroyChildren();
      overrideTransformParent.DestroyChildren();

      spawnUIPane.SetActive(false);
      spawnPointParent.SetActive(false);
      spawnPointIconParent.SetActive(false);
      overrideTransformParent.SetActive(false);
   }

   public void setData (SeaMonsterEntityDataCopy monsterData) {
      currentData = monsterData;

      animSlider.onValueChanged.RemoveAllListeners();
      animSlider.maxValue = Enum.GetValues(typeof(Anim.Type)).Length - 1;
      animSlider.onValueChanged.AddListener(_ => {
         simpleAnim.initialize();
         try {
            warningNotSupported.SetActive(false);
            simpleAnim.playAnimation((Anim.Type) animSlider.value);
            rippleSimpleAnim.playAnimation((Anim.Type) animSlider.value);

            simpleAnim.isPaused = false;
            rippleSimpleAnim.isPaused = false;
         } catch {
            warningNotSupported.SetActive(true);
         }
         animText.text = ((Anim.Type) animSlider.value).ToString();
      });

      defaultSprite.sprite = ImageManager.getSprite(monsterData.defaultSpritePath);
      rippleSprite.sprite = ImageManager.getSprite(monsterData.defaultRippleSpritePath);

      outlineSprite.transform.localScale = new Vector3(monsterData.outlineScaleOverride, monsterData.outlineScaleOverride, monsterData.outlineScaleOverride);
      defaultSprite.transform.localScale = new Vector3(monsterData.scaleOverride, monsterData.scaleOverride, monsterData.scaleOverride);
      rippleSprite.transform.localScale = new Vector3(monsterData.rippleScaleOverride, monsterData.rippleScaleOverride, monsterData.rippleScaleOverride);

      spawnPointIconParent.DestroyChildren();
      overrideTransformParent.DestroyChildren();

      if (monsterData.projectileSpawnLocations != null) {
         if (monsterData.projectileSpawnLocations.Count > 0) {
            spawnPointParent.SetActive(false);
            int loopIndex = 0;
            foreach (DirectionalPositions spawnData in monsterData.projectileSpawnLocations) {
               GameObject worldObj = Instantiate(overrideTransformPrefab, overrideTransformParent.transform);
               worldObj.transform.localPosition = new Vector3(spawnData.spawnTransform.x, spawnData.spawnTransform.y, spawnData.spawnTransform.z);

               GameObject uiObj = Instantiate(spawnPointIcon, spawnPointIconParent.transform);
               SpawnDataUITemplate uiTemplate = uiObj.GetComponent<SpawnDataUITemplate>();
               uiTemplate.templateName.text = spawnData.direction.ToString();
               uiTemplate.toggleButton.onValueChanged.AddListener(_ => {
                  worldObj.SetActive(!worldObj.activeSelf);
               });

               uiTemplate.selectButton.onClick.AddListener(() => {
                  coordName.text = spawnData.direction.ToString();
                  xSlider.onValueChanged.RemoveAllListeners();
                  ySlider.onValueChanged.RemoveAllListeners();

                  xSlider.value = spawnData.spawnTransform.x;
                  xSlider.onValueChanged.AddListener(_ => {
                     spawnData.spawnTransform = new Vector3(_ , spawnData.spawnTransform.y, spawnData.spawnTransform.z);
                     xSliderText.text = "x: "+_.ToString("f2");

                     worldObj.transform.localPosition = new Vector3(_, spawnData.spawnTransform.y, spawnData.spawnTransform.z);
                  });

                  ySlider.value = spawnData.spawnTransform.y;
                  ySlider.onValueChanged.AddListener(_ => {
                     spawnData.spawnTransform = new Vector3(spawnData.spawnTransform.x, _, spawnData.spawnTransform.z);
                     ySliderText.text = "y: "+_.ToString("f2");

                     worldObj.transform.localPosition = new Vector3(spawnData.spawnTransform.x, _, spawnData.spawnTransform.z);
                  });
                  xSlider.onValueChanged.Invoke(xSlider.value);
                  ySlider.onValueChanged.Invoke(ySlider.value);
               });

               uiObj.SetActive(true);
               if (loopIndex == 0) {
                  uiTemplate.selectButton.onClick.Invoke();
               }
               loopIndex++;
            }
         }
      }

      simpleAnim.group = monsterData.animGroup;
      rippleSimpleAnim.group = monsterData.animGroup;

      simpleAnim.frameLengthOverride = monsterData.animationSpeedOverride;
      rippleSimpleAnim.frameLengthOverride = monsterData.rippleAnimationSpeedOverride;

      simpleAnim.enabled = true;
      rippleSimpleAnim.enabled = true; 

      rippleSimpleAnim.initialize();
      simpleAnim.initialize();
   }

   #region Private Variables

   private bool _isPrimarySprite = true;

   #endregion
}
