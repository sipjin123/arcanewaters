using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Text;

namespace BackgroundTool
{
   public class ImageManipulator : MonoBehaviour
   {
      #region Public Variables

      // Self
      public static ImageManipulator self;

      // Obj that will be dragged
      public GameObject draggedObj;

      // Sprite prefab
      public GameObject spritePrefab;

      // Content holder
      public Transform contentHolder;

      // Scroll rect reference
      public ScrollRect scrollRect;

      // Last known sprite template
      public SpriteTemplate cachedTemplate;

      // UI reference to scale the sprite
      public Slider scaleSlider;
      public Text scaleText;

      // UI reference to rotate the sprite
      public Slider rotationSlider;
      public Text rotationText;

      // UI reference to the layer of the sprite
      public Slider layerSlider;
      public Text layerText;

      // Determines if the mouse is on the hover panel
      public bool isHoveringOnSelectionPanel;

      // Toggles the visibility of the selection panel
      public Toggle toggleSelectionPanel;

      // If the movable object is newly created
      public bool isNewlySpawned;

      // Canvas in charge of spawning sprites
      public CanvasGroup creationCanvas;

      // The summary of the data of the sprite
      public Text summaryContent;

      // The floating object that holds the summary content
      public Transform summaryContentHolder;

      // Determines if a sprite is being dragged
      public bool isDragging;

      // Toggler ui to determine if UI is locked
      public Toggle isLockedToggle;

      // List of sprite templates spawned
      public List<SpriteTemplateData> spriteTemplateDataList;

      // Prefab for toggler that can be spawned
      public GameObject layerTogglerPrefab;

      // Content holder for toggler prefab
      public Transform layerTogglerParent;

      // Max layers allowed in the editor
      public static int MAX_LAYER_COUNT = 10;

      // Shows or hides all layers
      public Toggle toggleAllLayers;

      // List of layer togglers spawned
      public List<Toggle> layerTogglerList;

      // Delete sprite template
      public Button deleteSpriteTemplate;

      #endregion

      private void Start () {
         self = this;
         spriteTemplateDataList = new List<SpriteTemplateData>();

         deleteSpriteTemplate.onClick.AddListener(() => {
            SpriteTemplateData spriteTempData = spriteTemplateDataList.Find(_ => _ == cachedTemplate.spriteTemplateData);
            if (spriteTempData != null) {
               spriteTemplateDataList.Remove(spriteTempData);
               GameObject.Destroy(cachedTemplate.gameObject);
            }
            clearCache();
         });

         toggleSelectionPanel.onValueChanged.AddListener(isOn => {
            creationCanvas.gameObject.SetActive(isOn);
         });

         isLockedToggle.onValueChanged.AddListener(_ => {
            if (cachedTemplate != null) {
               cachedTemplate.spriteTemplateData.isLocked = _;
            }
         });

         scaleSlider.onValueChanged.AddListener(scale => {
            if (cachedTemplate != null) {
               if (!cachedTemplate.spriteTemplateData.isLocked) {
                  scaleText.text = scale.ToString("f1");
                  cachedTemplate.spriteTemplateData.scaleAlteration = scale;
                  cachedTemplate.transform.localScale = new Vector3(scale, scale, scale);
               }
            }
         });

         rotationSlider.onValueChanged.AddListener(rotation => {
            if (cachedTemplate != null) {
               if (!cachedTemplate.spriteTemplateData.isLocked) {
                  rotationText.text = rotation.ToString("f1");
                  cachedTemplate.spriteTemplateData.rotationAlteration = rotation;
                  cachedTemplate.transform.localEulerAngles = new Vector3(0, 0, rotation);
               }
            }
         });

         layerSlider.onValueChanged.AddListener(currLayer => {
            if (cachedTemplate != null) {
               if (!cachedTemplate.spriteTemplateData.isLocked) {
                  layerText.text = currLayer.ToString();
                  cachedTemplate.spriteTemplateData.layerIndex = (int) currLayer;
                  cachedTemplate.spriteRender.sortingOrder = (int) currLayer;
               }
            }
         });

         toggleAllLayers.onValueChanged.AddListener(_ => {
            foreach (Toggle toggle in layerTogglerList) {
               toggle.isOn = _;
            }
         });

         layerTogglerList = new List<Toggle>();
         for (int i = 0; i < MAX_LAYER_COUNT; i++) {
            Toggle togglerInstance = Instantiate(layerTogglerPrefab, layerTogglerParent).GetComponentInChildren<Toggle>();
            Text togglerLabel = togglerInstance.GetComponentInChildren<Text>();
            togglerLabel.text = i.ToString();
            int cachedID = i;
            layerTogglerList.Add(togglerInstance);

            togglerInstance.onValueChanged.AddListener(_ => {
               setToggledLayers(cachedID, _);
            });
         }
      }

      private void setToggledLayers (int layerID, bool isEnabled) {
         foreach (Transform spriteTempObj in contentHolder) {
            SpriteTemplate spriteTemp = spriteTempObj.GetComponent<SpriteTemplate>();
            if (spriteTemp.spriteTemplateData.layerIndex == layerID) {
               spriteTemp.gameObject.SetActive(isEnabled);
            }
         }
      }

      public void beginHoverObj (SpriteTemplate spriteTemp) {
         if (!isDragging) {
            summaryContentHolder.gameObject.SetActive(true);
            Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);
            pos.z = 0;
            summaryContentHolder.position = pos;
            spriteTemp.spriteRender.color = Color.blue;

            StringBuilder stringBuild = new StringBuilder();
            stringBuild.Append("Scale: " + spriteTemp.spriteTemplateData.scaleAlteration.ToString("f1") + " \n");
            stringBuild.Append("Layer: " + spriteTemp.spriteTemplateData.layerIndex + " \n");
            stringBuild.Append("Locked: " + spriteTemp.spriteTemplateData.isLocked + " \n");
            summaryContent.text = stringBuild.ToString();
         }
      }

      public void stopHoverObj (SpriteTemplate spriteTemp) {
         spriteTemp.spriteRender.color = Color.white;
         summaryContentHolder.gameObject.SetActive(false);
      }

      public void stopHoverObj () {
         summaryContentHolder.gameObject.SetActive(false);
      }

      public void beginDragObj (SpriteTemplate spriteTemp) {
         // Sets previous sprite to default
         if (cachedTemplate != null) {
            cachedTemplate.spriteRender.color = Color.white;
         }

         Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);
         _initialOffset = spriteTemp.transform.localPosition - pos;

         isDragging = true;
         isNewlySpawned = false;
         stopHoverObj();

         // Cache template first
         cachedTemplate = spriteTemp;

         // Alter template related data
         scaleSlider.value = spriteTemp.spriteTemplateData.scaleAlteration;
         rotationSlider.value = spriteTemp.spriteTemplateData.rotationAlteration;
         layerSlider.value = spriteTemp.spriteTemplateData.layerIndex;
         isLockedToggle.isOn = spriteTemp.spriteTemplateData.isLocked;

         draggedObj = spriteTemp.gameObject;
         cachedTemplate.spriteRender.color = Color.red;
         scrollRect.enabled = false;
      }

      public void stopDrag () {
         draggedObj = null;
         scrollRect.enabled = true;
         isDragging = false;
      }

      public void clearCache () {
         if (cachedTemplate) {
            cachedTemplate.spriteRender.color = Color.white;
            cachedTemplate = null;
         }
         draggedObj = null;
         isDragging = false;
      }

      private void Update () {
         if (draggedObj != null) {
            Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);
            if (!cachedTemplate.spriteTemplateData.isLocked) {
               pos.z = -cachedTemplate.spriteTemplateData.layerIndex;
               draggedObj.transform.localPosition = new Vector3(_initialOffset.x + pos.x, _initialOffset.y + pos.y, 0);
            }

            if (Input.GetKeyUp(KeyCode.Mouse0)) {
               cachedTemplate.createdFromPanel = false;
               cachedTemplate.spriteTemplateData.localPosition = draggedObj.transform.localPosition;
               if (isNewlySpawned) {
                  creationCanvas.gameObject.SetActive(true);
               }
               stopDrag();
            }
         }

         if (Input.GetKeyDown(KeyCode.Mouse1)) {
            clearCache();
         }
      }

      public void createInstance (Sprite spriteContent, string newSpritePath, SpriteTemplateData newSpritedata = null) {
         SpriteTemplate spriteTemplate = createTemplate(newSpritedata, newSpritePath, spriteContent);

         // Only snap to mouse when creating from sprite panel
         if (newSpritedata == null) {
            creationCanvas.gameObject.SetActive(false);
            Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);
            spriteTemplate.transform.localPosition = new Vector3(pos.x, pos.y, -1);
            spriteTemplate.createdFromPanel = true;
            spriteTemplate.OnMouseDown();
            isNewlySpawned = true;
         }

         spriteTemplate.gameObject.SetActive(true);
         stopHoverObj();
      }

      private SpriteTemplate createTemplate (SpriteTemplateData spriteData, string spritePath, Sprite spriteContent) {
         GameObject obj = Instantiate(spritePrefab, contentHolder);
         SpriteTemplate spriteTemplate = obj.GetComponent<SpriteTemplate>();

         spriteTemplate.spriteRender.sprite = spriteContent;
         spriteTemplate.gameObject.AddComponent<BoxCollider2D>();

         if (spriteData == null) {
            spriteTemplate.spriteTemplateData.scaleAlteration = 1;
            spriteTemplate.spriteTemplateData.rotationAlteration = 0;
            spriteTemplate.spriteTemplateData.layerIndex = 0;
            spriteTemplate.spriteTemplateData.isLocked = false;
            spriteTemplate.spriteTemplateData.spritePath = spritePath;
         } else {
            spriteTemplate.spriteTemplateData = spriteData;
            spriteTemplate.setTemplate();
         }
         spriteTemplateDataList.Add(spriteTemplate.spriteTemplateData);

         return spriteTemplate;
      }

      public void generateSprites (List<SpriteTemplateData> spriteDataList) {
         spriteTemplateDataList = new List<SpriteTemplateData>();
         contentHolder.gameObject.DestroyChildren();

         foreach (SpriteTemplateData spriteData in spriteDataList) {
            Sprite loadedSprite = ImageManager.getSprite(spriteData.spritePath);
            createInstance(loadedSprite, spriteData.spritePath, spriteData);
         }
      }

      #region Private Variables

      // Initial offset of the sprite location
      private Vector3 _initialOffset;

      // Reference to the main camera
      [SerializeField] private Camera _mainCam;

      #endregion
   }
}