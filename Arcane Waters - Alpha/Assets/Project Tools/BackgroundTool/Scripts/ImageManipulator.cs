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
      public List<DraggableContent> draggedObjList = new List<DraggableContent>();

      // Sprite prefab
      public GameObject spritePrefab;

      // Content holder
      public Transform contentHolder;

      // Scroll rect reference
      public ScrollRect scrollRect;

      // The rect component of the spawnable sprite selection panel
      public RectTransform rectReference;

      // UI reference to the layer of the sprite
      public Slider zOffsetSlider;
      public Text zOffsetText;

      // Determines if the mouse is on the hover panel
      public bool isHoveringOnSelectionPanel;

      // Toggles the visibility of the selection panel
      public Toggle toggleSelectionPanel;

      // If the movable object is newly created
      public bool isNewlySpawned;

      // Canvas in charge of spawning sprites
      public GameObject creationPanel;

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

      // Max distance of the z axis
      public static int Z_AXIS_MAX_DIST = 10;

      // Shows or hides all layers
      public Toggle toggleAllLayers;

      // List of layer togglers spawned
      public List<Toggle> layerTogglerList;

      // Delete sprite template
      public Button deleteSpriteTemplate;

      // Enum type that alters how sprite templates will behave (will move if set to Move mode)
      public EditType currentEditType;

      // Button for altering the edit type
      public Button setMoveButton, setEditButton;

      // Determines if only one element is being dragged
      public bool singleDrag;

      // Shows what current mode the tool is in
      public Text modeDisplay;

      // Determines if the ui is in foreground/background etc
      public Slider layerTypeSlider;
      public Text layerTypeText;

      // Recently selected sprite template
      public SpriteTemplate recentSpriteTemp;

      // All UI objects that will be revealed when edit mode
      public GameObject[] editModeUI;

      // All UI objects that will be revealed when move mode
      public GameObject[] moveModeUI;

      // The sprite object to visualize the drag box
      public GameObject spriteHighlightObj;

      public class DraggableContent
      {
         // The reference to the object
         public GameObject spriteObject;

         // The reference to the sprite renderer
         public SpriteTemplate cachedSpriteTemplate;

         // The offset position from the mouse click location
         public Vector3 spriteOffset;
      }

      public enum LayerType
      {
         None = 0,
         Background = 1,
         Midground = 2,
         Foreground = 3
      }

      public enum EditType
      {
         None = 0,
         Move = 1,
         Edit = 2,
      }

      #endregion

      private void Start () {
         self = this;
         spriteTemplateDataList = new List<SpriteTemplateData>();

         setMoveButton.onClick.AddListener(() => setEditMode(EditType.Move));
         setEditButton.onClick.AddListener(() => setEditMode(EditType.Edit));

         deleteSpriteTemplate.onClick.AddListener(() => {
            SpriteTemplateData spriteTempData = spriteTemplateDataList.Find(_ => _ == recentSpriteTemp.spriteTemplateData);
            if (spriteTempData != null) {
               spriteTemplateDataList.Remove(spriteTempData);
               GameObject.Destroy(recentSpriteTemp.gameObject);
            }
            clearCache();
         });

         toggleSelectionPanel.onValueChanged.AddListener(isOn => {
            creationPanel.SetActive(isOn);
         });

         isLockedToggle.onValueChanged.AddListener(_ => {
            recentSpriteTemp.spriteTemplateData.isLocked = _;
         });

         zOffsetSlider.onValueChanged.AddListener(currLayer => {
            if (draggedObjList.Count > 0) {
               zOffsetText.text = currLayer.ToString();
               foreach (DraggableContent draggableContent in draggedObjList) {
                  draggableContent.cachedSpriteTemplate.spriteTemplateData.layerIndex = (int) currLayer;
                  draggableContent.cachedSpriteTemplate.spriteRender.sortingOrder = (int) currLayer;
               }
            }
         });

         layerTypeSlider.onValueChanged.AddListener(currLayer => {
            if (recentSpriteTemp != null) {
               layerTypeText.text = ((LayerType) currLayer).ToString();
               Vector3 currPos = recentSpriteTemp.transform.localPosition;
               recentSpriteTemp.spriteTemplateData.layerIndex = (int) currLayer;
               recentSpriteTemp.spriteRender.sortingOrder = (int) currLayer;
               recentSpriteTemp.transform.localPosition = new Vector3(currPos.x, currPos.y, -currLayer);
            } 
         });

         toggleAllLayers.onValueChanged.AddListener(_ => {
            foreach (Toggle toggle in layerTogglerList) {
               toggle.isOn = _;
            }
         });

         layerTogglerList = new List<Toggle>();
         for (int i = 0; i < Z_AXIS_MAX_DIST; i++) {
            Toggle togglerInstance = Instantiate(layerTogglerPrefab, layerTogglerParent).GetComponentInChildren<Toggle>();
            Text togglerLabel = togglerInstance.GetComponentInChildren<Text>();
            togglerLabel.text = i.ToString();
            int cachedID = i;
            layerTogglerList.Add(togglerInstance);

            togglerInstance.onValueChanged.AddListener(_ => {
               setToggledLayers(cachedID, _);
            });
         }

         setEditMode(EditType.Move);
      }

      public void setEditMode (EditType editType) {
         modeDisplay.text = editType.ToString();
         currentEditType = editType;

         foreach (GameObject gameObj in editModeUI) {
            gameObj.SetActive(editType == EditType.Edit);
         }
         foreach (GameObject gameObj in moveModeUI) {
            resetDraggedGroup();
            gameObj.SetActive(editType == EditType.Move);
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

      public void clearCache () {
         if (draggedObjList.Count > 0) {
            foreach (DraggableContent draggableContent in draggedObjList) {
               draggableContent.cachedSpriteTemplate.spriteRender.color = Color.white;
            }
         }
         draggedObjList.Clear();
         isDragging = false;
      }

      #region Mouse Behavior

      public void beginHoverObj (SpriteTemplate spriteTemp) {
         if (!isDragging) {
            spriteTemp.spriteRender.color = Color.blue;
         }
      }

      public void stopHoverObj (SpriteTemplate spriteTemp) {
         spriteTemp.spriteRender.color = Color.white;
      }

      public void beginDragSelectionGroup (List<SpriteSelectionTemplate> spriteTempGroup, bool isSingleDrag) {
         singleDrag = isSingleDrag;
         resetDraggedGroup();
         Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);

         foreach (SpriteSelectionTemplate spriteTemp in spriteTempGroup) {
            SpriteTemplate templateObj = createInstance(spriteTemp.spriteIcon.sprite, spriteTemp.spritePath, false).GetComponent<SpriteTemplate>();
            templateObj.transform.position = spriteTemp.transform.position;

            Vector3 initOffset = templateObj.transform.localPosition - pos;
            templateObj.spriteRender.color = Color.red;

            DraggableContent newDragContent = new DraggableContent {
               spriteObject = templateObj.gameObject,
               cachedSpriteTemplate = templateObj,
               spriteOffset = initOffset
            };
            draggedObjList.Add(newDragContent);
            recentSpriteTemp = templateObj;

            if (isSingleDrag) {
               break;
            }
         }

         if (currentEditType == EditType.Move) {
            isDragging = true;
         }
      }

      public void beginDragSpawnedGroup (List<SpriteTemplate> spriteTempGroup, bool isSingleDrag) {
         singleDrag = isSingleDrag;
         resetDraggedGroup();
         Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);

         foreach (SpriteTemplate spriteTemp in spriteTempGroup) {
            Vector3 initOffset = spriteTemp.transform.localPosition - pos;
            spriteTemp.spriteRender.color = Color.red;

            DraggableContent dragContent = new DraggableContent { 
               cachedSpriteTemplate = spriteTemp,
               spriteObject = spriteTemp.gameObject,
               spriteOffset = initOffset
            };
            draggedObjList.Add(dragContent);
            recentSpriteTemp = spriteTemp;

            if (isSingleDrag) {
               break;
            }
         }

         if (currentEditType == EditType.Move) {
            isDragging = true;
         } else if (currentEditType == EditType.Edit) {
            recentSpriteTemp.spriteRender.color = Color.red;
            layerTypeSlider.value = recentSpriteTemp.spriteTemplateData.layerIndex;
            isLockedToggle.isOn = recentSpriteTemp.spriteTemplateData.isLocked;
         }
      }

      private void resetDraggedGroup () {
         foreach (DraggableContent dragContent in draggedObjList) {
            dragContent.cachedSpriteTemplate.spriteRender.color = Color.white;
         }
         draggedObjList.Clear();
      }

      public void stopDrag () {
         resetDraggedGroup();
         scrollRect.enabled = true;
         isDragging = false;
      }

      #endregion
      
      public void endClick () {
         foreach (DraggableContent draggableContent in draggedObjList) {
            draggableContent.cachedSpriteTemplate.createdFromPanel = false;
         }

         if (isNewlySpawned) {
            isNewlySpawned = false;
            creationPanel.SetActive(true);
         }
         stopDrag();
      }

      private void Update () {
         if (draggedObjList.Count > 0 && currentEditType == EditType.Move) {
            Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);

            foreach (DraggableContent draggableContent in draggedObjList) {
               if (!draggableContent.cachedSpriteTemplate.spriteTemplateData.isLocked) {
                  pos.z = -draggableContent.cachedSpriteTemplate.spriteTemplateData.layerIndex;
                  draggableContent.spriteObject.transform.localPosition = new Vector3(draggableContent.spriteOffset.x + pos.x, draggableContent.spriteOffset.y + pos.y, -draggableContent.cachedSpriteTemplate.spriteTemplateData.layerIndex);
                  draggableContent.cachedSpriteTemplate.spriteTemplateData.localPosition = draggableContent.spriteObject.transform.localPosition;
               }
            }

            if (Input.GetKeyDown(KeyCode.Mouse0) && !singleDrag) {
               endClick();
            }
         }

         if (Input.GetKeyDown(KeyCode.Mouse1)) {
            clearCache();
         }
      }

      #region Sprite Generation

      public GameObject createInstance (Sprite spriteContent, string newSpritePath, bool selectImmediately,SpriteTemplateData newSpritedata = null) {
         SpriteTemplate spriteTemplate = createTemplate(newSpritedata, newSpritePath, spriteContent);

         // Only snap to mouse when creating from sprite panel
         if (newSpritedata == null) {
            creationPanel.SetActive(false);
            Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);
            spriteTemplate.transform.localPosition = new Vector3(pos.x, pos.y, -1);
            spriteTemplate.createdFromPanel = true;
            isNewlySpawned = true;
         }

         spriteTemplate.gameObject.SetActive(true);
         return spriteTemplate.gameObject;
      }

      private SpriteTemplate createTemplate (SpriteTemplateData spriteData, string spritePath, Sprite spriteContent) {
         GameObject obj = Instantiate(spritePrefab, contentHolder);
         SpriteTemplate spriteTemplate = obj.GetComponent<SpriteTemplate>();

         spriteTemplate.spriteRender.sprite = spriteContent;
         spriteTemplate.gameObject.AddComponent<BoxCollider2D>();

         if (spriteData == null) {
            spriteTemplate.spriteTemplateData.layerIndex = (int)LayerType.Foreground;
            spriteTemplate.spriteTemplateData.isLocked = false;
            spriteTemplate.spriteTemplateData.spritePath = spritePath;
         } else {
            spriteTemplate.spriteTemplateData = spriteData;
            spriteTemplate.setTemplate();
         }
         spriteTemplate.spriteRender.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
         spriteTemplate.spriteRender.sortingOrder = spriteTemplate.spriteTemplateData.layerIndex;
         spriteTemplateDataList.Add(spriteTemplate.spriteTemplateData);

         return spriteTemplate;
      }

      public void generateSprites (List<SpriteTemplateData> spriteDataList) {
         spriteTemplateDataList = new List<SpriteTemplateData>();
         contentHolder.gameObject.DestroyChildren();

         foreach (SpriteTemplateData spriteData in spriteDataList) {
            Sprite loadedSprite = ImageManager.getSprite(spriteData.spritePath);
            createInstance(loadedSprite, spriteData.spritePath, false, spriteData);
         }
      }

      #endregion

      #region Private Variables

      // Reference to the main camera
      [SerializeField] private Camera _mainCam;

      #endregion
   }
}