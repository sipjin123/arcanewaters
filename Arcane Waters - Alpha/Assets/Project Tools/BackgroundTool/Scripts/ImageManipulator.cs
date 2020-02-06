using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Text;
using System;

namespace BackgroundTool
{
   [Serializable]
   public class DraggableContent
   {
      // The reference to the object
      public GameObject spriteObject;

      // The reference to the sprite renderer
      public SpriteTemplate cachedSpriteTemplate;

      // The offset position from the mouse click location
      public Vector3 spriteOffset;
   }

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
      public static int Z_AXIS_MAX_DIST = 4;

      // Shows or hides all layers
      public Toggle toggleAllLayers;

      // List of layer togglers spawned
      public List<Toggle> layerTogglerList;

      // Delete sprite template
      public Button deleteSpriteTemplate;

      // Determines if only one element is being dragged
      public bool singleDrag;

      // Determines if the ui is in foreground/background etc
      public Slider layerTypeSlider;
      public Text layerTypeText;

      // The sprite object to visualize the drag box
      public GameObject spriteHighlightObj;

      // Determines if the tool is spawning sprites
      public bool isSpawning;

      // Caches the total spawned objects for ID purposes
      public int spawnCount = 0;

      // Force disable highlighting upon mouse drag
      public bool disableHighlighting;

      // Determine if the mouse is over any highlighted sprite
      public bool isHoveringHighlight;

      public enum LayerType
      {
         None = 0,
         Background = 1,
         Midground = 2,
         Foreground = 3
      }

      #endregion

      private void Start () {
         self = this;
         spriteTemplateDataList = new List<SpriteTemplateData>();

         deleteSpriteTemplate.onClick.AddListener(() => {
            if (draggedObjList.Count > 0) {
               foreach (DraggableContent draggableContent in draggedObjList) {
                  spriteTemplateDataList.Remove(draggableContent.cachedSpriteTemplate.spriteTemplateData);
                  GameObject.Destroy(draggableContent.cachedSpriteTemplate.gameObject);
               }
               clearCache();
            }
         });

         toggleSelectionPanel.onValueChanged.AddListener(isOn => {
            creationPanel.SetActive(isOn);
         });

         isLockedToggle.onValueChanged.AddListener(_ => {
            if (draggedObjList.Count > 0) {
               foreach (DraggableContent draggableContent in draggedObjList) {
                  draggableContent.cachedSpriteTemplate.spriteTemplateData.isLocked = _;
               }
            }
         });

         zOffsetSlider.onValueChanged.AddListener(currLayer => {
            if (draggedObjList.Count > 0) {
               zOffsetText.text = currLayer.ToString();
               foreach (DraggableContent draggableContent in draggedObjList) {
                  draggableContent.cachedSpriteTemplate.spriteTemplateData.zAxisOffset = (int) currLayer;

                  int layerIndex = draggableContent.cachedSpriteTemplate.spriteTemplateData.layerIndex;
                  float zOffset = layerIndex + (currLayer * .1f);

                  Vector3 localPos = draggableContent.cachedSpriteTemplate.transform.localPosition;
                  draggableContent.cachedSpriteTemplate.transform.localPosition = new Vector3(localPos.x, localPos.y, -zOffset);
               }
            }
         });

         layerTypeSlider.onValueChanged.AddListener(updateLayer);

         toggleAllLayers.onValueChanged.AddListener(_ => {
            foreach (Toggle toggle in layerTogglerList) {
               toggle.isOn = _;
            }
         });

         layerTogglerList = new List<Toggle>();
         for (int i = 1; i < Z_AXIS_MAX_DIST; i++) {
            Toggle togglerInstance = Instantiate(layerTogglerPrefab, layerTogglerParent).GetComponentInChildren<Toggle>();
            Text togglerLabel = togglerInstance.GetComponentInChildren<Text>();
            togglerLabel.text = ((LayerType) i).ToString();
            int cachedID = i;
            layerTogglerList.Add(togglerInstance);

            togglerInstance.onValueChanged.AddListener(_ => {
               setToggledLayers(cachedID, _);
            });
         }
      }

      private void updateLayer (float currLayer) {
         if (draggedObjList.Count > 0) {
            layerTypeText.text = ((LayerType) currLayer).ToString();
            foreach (DraggableContent draggableContent in draggedObjList) {
               float zAxisOffset = draggableContent.cachedSpriteTemplate.spriteTemplateData.zAxisOffset * .1f;

               Vector3 currPos = draggableContent.cachedSpriteTemplate.transform.localPosition;
               draggableContent.cachedSpriteTemplate.spriteTemplateData.layerIndex = (int) currLayer;
               draggableContent.cachedSpriteTemplate.spriteRender.sortingOrder = (int) currLayer;
               draggableContent.cachedSpriteTemplate.transform.localPosition = new Vector3(currPos.x, currPos.y, -(currLayer + zAxisOffset));
            }
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
         resetDraggedGroup();
         draggedObjList.Clear();
         isDragging = false;
      }

      #region Mouse Behavior

      public void beginDragSelectionGroup (List<SpriteSelectionTemplate> spriteTempGroup, bool isSingleDrag) {
         singleDrag = isSingleDrag;
         resetDraggedGroup();
         Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);

         foreach (SpriteSelectionTemplate spriteTemp in spriteTempGroup) {
            SpriteTemplate templateObj = createInstance(spriteTemp.spriteIcon.sprite, spriteTemp.spritePath, false).GetComponent<SpriteTemplate>();
            templateObj.transform.position = spriteTemp.transform.position;

            Vector3 initOffset = templateObj.transform.localPosition - pos;
            templateObj.highlightObj(true);

            DraggableContent newDragContent = new DraggableContent {
               spriteObject = templateObj.gameObject,
               cachedSpriteTemplate = templateObj,
               spriteOffset = initOffset
            };
            draggedObjList.Add(newDragContent);

            if (isSingleDrag) {
               break;
            }
         }

         isDragging = true;
         isSpawning = true;
      }

      public void beginDragSpawnedGroup (List<SpriteTemplate> spriteTempGroup, bool isSingleDrag) {
         singleDrag = isSingleDrag;
         resetDraggedGroup();
         Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);

         foreach (SpriteTemplate spriteTemp in spriteTempGroup) {
            Vector3 initOffset = spriteTemp.transform.localPosition - pos;
            spriteTemp.highlightObj(true);

            DraggableContent dragContent = new DraggableContent { 
               cachedSpriteTemplate = spriteTemp,
               spriteObject = spriteTemp.gameObject,
               spriteOffset = initOffset
            };
            draggedObjList.Add(dragContent);

            if (isSingleDrag) {
               isLockedToggle.isOn = spriteTemp.spriteTemplateData.isLocked;
               layerTypeSlider.value = spriteTemp.spriteTemplateData.layerIndex;
               zOffsetSlider.value = spriteTemp.spriteTemplateData.zAxisOffset;
            }
         }
      }

      private void resetDraggedGroup () {
         foreach (DraggableContent dragContent in draggedObjList) {
            dragContent.cachedSpriteTemplate.highlightObj(false);
         }
         draggedObjList.Clear();
      }

      public void stopDrag () {
         resetDraggedGroup();
         scrollRect.enabled = true;
         isDragging = false;
         disableHighlighting = false;
      }

      #endregion
      
      public void endClick () {
         if (isSpawning) {
            foreach (DraggableContent draggable in draggedObjList) {
               GameObject gameObj = createInstance(draggable.cachedSpriteTemplate.spriteRender.sprite, draggable.cachedSpriteTemplate.spriteTemplateData.spritePath, false, draggable.cachedSpriteTemplate.spriteTemplateData);
               gameObj.transform.position = draggable.cachedSpriteTemplate.transform.position;
               draggable.cachedSpriteTemplate.highlightObj(false);
            }
         } else {
            foreach (DraggableContent draggableContent in draggedObjList) {
               draggableContent.cachedSpriteTemplate.createdFromPanel = false;
            }

            if (isNewlySpawned) {
               isNewlySpawned = false;
               creationPanel.SetActive(true);
            }
            stopDrag();
         }
      }

      private void Update () {
         bool dragSpawnableGroup = draggedObjList.Count > 0 && isDragging;
         bool dragHighlightedSpawnedGroup = !isSpawning && draggedObjList.Count > 0 && Input.GetKey(KeyCode.Mouse0) && isHoveringHighlight;

         if (dragHighlightedSpawnedGroup && Input.GetKeyDown(KeyCode.Mouse0)) {
            Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);
            foreach (DraggableContent draggableContent in draggedObjList) {
               Vector3 initOffset = draggableContent.cachedSpriteTemplate.transform.localPosition - pos;
               draggableContent.spriteOffset = initOffset;
            }
         }

         if (dragSpawnableGroup || dragHighlightedSpawnedGroup) {
            Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);

            foreach (DraggableContent draggableContent in draggedObjList) {
               if (!draggableContent.cachedSpriteTemplate.spriteTemplateData.isLocked) {
                  float newZOffset = draggableContent.cachedSpriteTemplate.spriteTemplateData.layerIndex + draggableContent.cachedSpriteTemplate.spriteTemplateData.zAxisOffset * .1f;

                  draggableContent.spriteObject.transform.localPosition = new Vector3(draggableContent.spriteOffset.x + pos.x, draggableContent.spriteOffset.y + pos.y, -newZOffset);
                  draggableContent.cachedSpriteTemplate.spriteTemplateData.localPosition = draggableContent.spriteObject.transform.localPosition;
               }
            }

            if (Input.GetKeyDown(KeyCode.Mouse0) && !singleDrag && !dragHighlightedSpawnedGroup) {
               isHoveringHighlight = false;
               endClick();
            }
         }

         // Cancel spawn sprite mode
         if (Input.GetKeyDown(KeyCode.Mouse1)) {
            if (isSpawning) {
               isSpawning = false;
               foreach (DraggableContent draggableContent in draggedObjList) {
                  Destroy(draggableContent.cachedSpriteTemplate.gameObject);
               }
            }
            endClick();
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
            spriteTemplate.highlightObj(false);
            isNewlySpawned = true;
         }

         spriteTemplate.gameObject.name = spriteContent.name + "_" +spawnCount;
         spriteTemplate.gameObject.SetActive(true);
         spawnCount++;
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
            spriteTemplate.spriteTemplateData.isLocked = spriteData.isLocked;
            spriteTemplate.spriteTemplateData.layerIndex = spriteData.layerIndex;
            spriteTemplate.spriteTemplateData.zAxisOffset = spriteData.zAxisOffset;
            spriteTemplate.spriteTemplateData.spritePath = spriteData.spritePath;
            spriteTemplate.spriteTemplateData.localPosition = spriteData.localPosition;
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