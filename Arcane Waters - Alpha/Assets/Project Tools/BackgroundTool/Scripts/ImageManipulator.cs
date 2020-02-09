using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Text;
using System;
using static BackgroundTool.ImageLoader;

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
         Foreground = 3,
         Overlay = 4,
         PlaceHolders = 5
      }

      #endregion

      #region Initialize Setup

      private void Start () {
         self = this;
         spriteTemplateDataList = new List<SpriteTemplateData>();

         deleteSpriteTemplate.onClick.AddListener(() => {
            if (draggedObjList.Count > 0) {
               foreach (DraggableContent draggableContent in draggedObjList) {
                  spriteTemplateDataList.Remove(draggableContent.cachedSpriteTemplate.spriteData);
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
                  draggableContent.cachedSpriteTemplate.spriteData.isLocked = _;
               }
            }
         });

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

      private void setToggledLayers (int layerID, bool isEnabled) {
         foreach (Transform spriteTempObj in contentHolder) {
            SpriteTemplate spriteTemp = spriteTempObj.GetComponent<SpriteTemplate>();
            if (spriteTemp.spriteData.layerIndex == layerID) {
               spriteTemp.gameObject.SetActive(isEnabled);
            }
         }
      }

      public void clearCache () {
         resetDraggedGroup();
         draggedObjList.Clear();
         isDragging = false;
      }

      #endregion

      #region Mouse Behavior

      public void beginDragSelectionGroup (List<SpriteSelectionTemplate> spriteTempGroup, bool isSingleDrag) {
         singleDrag = isSingleDrag;
         resetDraggedGroup();
         Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);

         foreach (SpriteSelectionTemplate spriteTemp in spriteTempGroup) {
            SpriteTemplate spawnedSprite = createInstance(spriteTemp.spriteIcon.sprite, spriteTemp.spritePath, false, spriteTemp.contentCategory).GetComponent<SpriteTemplate>();
            spawnedSprite.spriteData.layerIndex = (int) spriteTemp.layerType;
            spawnedSprite.spriteRender.sortingOrder = (int) spriteTemp.layerType;
            spawnedSprite.transform.position = spriteTemp.transform.position;
            
            Vector3 initOffset = spawnedSprite.transform.localPosition - pos;
            spawnedSprite.highlightObj(true);

            // Compute Z axis offset
            int layerOffset = getLayerOffset(spawnedSprite.spriteData.layerIndex);
            float zAxisOffset = getZAxisOffset(spawnedSprite);
            float computedZAxis = layerOffset - zAxisOffset;

            // Modify local position after Z offset computation
            Vector3 newLocalPos = new Vector3(spawnedSprite.transform.localPosition.x, spawnedSprite.transform.localPosition.y, computedZAxis);
            spawnedSprite.transform.localPosition = newLocalPos;

            DraggableContent newDragContent = new DraggableContent {
               spriteObject = spawnedSprite.gameObject,
               cachedSpriteTemplate = spawnedSprite,
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
               isLockedToggle.isOn = spriteTemp.spriteData.isLocked;
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

      public void endClick () {
         if (isSpawning) {
            // Create a sprite instance if spawnable sprite was clicked from the panel
            foreach (DraggableContent draggable in draggedObjList) {
               BGContentCategory currentCategory = draggable.cachedSpriteTemplate.spriteData.contentCategory;

               // Overwrite category if it is a defender spawn point sprite
               if (currentCategory == BGContentCategory.SpawnPoints_Attackers) {
                  if (draggable.cachedSpriteTemplate.spriteRender.sprite.name.Contains(BackgroundGameManager.BATTLE_POS_KEY_LEFT)) {
                     currentCategory = BGContentCategory.SpawnPoints_Defenders;
                  }
               }

               SpriteTemplate spawnedSprite = createInstance(draggable.cachedSpriteTemplate.spriteRender.sprite, 
                  draggable.cachedSpriteTemplate.spriteData.spritePath, 
                  false,
                  currentCategory, 
                  null).GetComponent<SpriteTemplate>();

               spawnedSprite.transform.position = draggable.cachedSpriteTemplate.transform.position;
               spawnedSprite.spriteData.layerIndex = draggable.cachedSpriteTemplate.spriteData.layerIndex;

               // Compute z axis offset
               int layerOffset = getLayerOffset(spawnedSprite.spriteData.layerIndex);
               float zAxisOffset = getZAxisOffset(spawnedSprite);
               float computedZAxis = layerOffset - zAxisOffset;

               // Modify local position after Z offset computation
               Vector3 newLocalPos = new Vector3(spawnedSprite.transform.localPosition.x, spawnedSprite.transform.localPosition.y, computedZAxis);
               spawnedSprite.transform.localPosition = newLocalPos;

               // Cache Data
               spawnedSprite.spriteData.zAxisOffset = zAxisOffset;
               spawnedSprite.spriteData.localPositionData = newLocalPos;
               spawnedSprite.highlightObj(false);
            }
         } else {
            // Stop Dragging a spawned sprite
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

      #endregion

      private void Update () {
         bool dragSpawnableGroup = draggedObjList.Count > 0 && isDragging;
         bool dragHighlightedSpawnedGroup = !isSpawning && draggedObjList.Count > 0 && Input.GetKey(KeyCode.Mouse0) && isHoveringHighlight;

         // Cache initial mouse position upon click
         if (dragHighlightedSpawnedGroup && Input.GetKeyDown(KeyCode.Mouse0)) {
            Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);
            foreach (DraggableContent draggableContent in draggedObjList) {
               Vector3 initOffset = draggableContent.cachedSpriteTemplate.transform.localPosition - pos;
               draggableContent.spriteOffset = initOffset;
            }
         }

         // Drag sprite objects only if a spawnable group is active or a highlighted spawn group
         if (dragSpawnableGroup || dragHighlightedSpawnedGroup) {
            Vector3 mousePos = _mainCam.ScreenToWorldPoint(Input.mousePosition);

            foreach (DraggableContent draggableContent in draggedObjList) {
               if (!draggableContent.cachedSpriteTemplate.spriteData.isLocked) {
                  int layerOffset = getLayerOffset(draggableContent.cachedSpriteTemplate.spriteData.layerIndex);
                  float zAxisOffset = getZAxisOffset(draggableContent.cachedSpriteTemplate);
                  float computedZAxis = layerOffset - zAxisOffset;
                  draggableContent.cachedSpriteTemplate.spriteData.zAxisOffset = zAxisOffset;

                  Vector3 newPosition = new Vector3(draggableContent.spriteOffset.x + mousePos.x, draggableContent.spriteOffset.y + mousePos.y, computedZAxis);
                  draggableContent.cachedSpriteTemplate.transform.localPosition = newPosition;
                  draggableContent.cachedSpriteTemplate.spriteData.localPositionData = newPosition;
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
                  spriteTemplateDataList.Remove(draggableContent.cachedSpriteTemplate.spriteData);
                  Destroy(draggableContent.cachedSpriteTemplate.gameObject);
               }
            }
            endClick();
         }
      }

      public float getZAxisOffset (SpriteTemplate spriteTemp) {
         Transform objectTransform = spriteTemp.transform;

         // Initialize our new Z position to a truncated version of the Y position
         float newZ = objectTransform.localPosition.y;
         newZ = Util.TruncateTo100ths(newZ);

         // Adjust our Z position based on our collider's Y position
         Vector3 newLocalPosition = new Vector3(
             objectTransform.localPosition.x,
             objectTransform.localPosition.y,
             newZ / 100f
         );

         return -newLocalPosition.z;
      }

      public int getLayerOffset (int layerIndex) {
         LayerType layerType = (LayerType) layerIndex;
         int zOffset = 0;

         switch (layerType) {
            case LayerType.Background:
               zOffset = 0;
               break;
            case LayerType.Midground:
               zOffset = -5;
               break;
            case LayerType.Foreground:
               zOffset = -10;
               break;
            case LayerType.Overlay:
               zOffset = -12;
               break;
            case LayerType.PlaceHolders:
               zOffset = -14;
               break;
         }

         return zOffset;
      }

      #region Sprite Generation

      public GameObject createInstance (Sprite spriteContent, string newSpritePath, bool selectImmediately, BGContentCategory category, SpriteTemplateData newSpritedata = null) {
         SpriteTemplate spriteTemplate = createTemplate(newSpritedata, newSpritePath, spriteContent, category);

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

      private SpriteTemplate createTemplate (SpriteTemplateData spriteData, string spritePath, Sprite spriteContent, BGContentCategory category) {
         GameObject obj = Instantiate(spritePrefab, contentHolder);
         SpriteTemplate spriteTemplate = obj.GetComponent<SpriteTemplate>();

         spriteTemplate.spriteRender.sprite = spriteContent;
         spriteTemplate.gameObject.AddComponent<BoxCollider2D>();

         if (spriteData == null) {
            spriteTemplate.spriteData.layerIndex = (int) LayerType.Foreground;
            spriteTemplate.spriteData.zAxisOffset = 0;
            spriteTemplate.spriteData.isLocked = false;
            spriteTemplate.spriteData.spritePath = spritePath;
         } else {
            spriteTemplate.spriteData.isLocked = spriteData.isLocked;
            spriteTemplate.spriteData.layerIndex = spriteData.layerIndex;
            spriteTemplate.spriteData.zAxisOffset = spriteData.zAxisOffset;
            spriteTemplate.spriteData.spritePath = spriteData.spritePath;
            spriteTemplate.spriteData.localPositionData = spriteData.localPositionData;
            spriteTemplate.setTemplate();
         }

         spriteTemplate.spriteData.contentCategory = category;
         spriteTemplate.spriteRender.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
         spriteTemplate.spriteRender.sortingOrder = spriteTemplate.spriteData.layerIndex;
         spriteTemplateDataList.Add(spriteTemplate.spriteData);

         return spriteTemplate;
      }

      public void generateSprites (List<SpriteTemplateData> spriteDataList) {
         spriteTemplateDataList = new List<SpriteTemplateData>();
         contentHolder.gameObject.DestroyChildren();

         foreach (SpriteTemplateData spriteData in spriteDataList) {
            Sprite loadedSprite = ImageManager.getSprite(spriteData.spritePath);
            createInstance(loadedSprite, spriteData.spritePath, false, spriteData.contentCategory, spriteData);
         }
      }

      #endregion

      #region Private Variables
#pragma warning disable CS0649
      // Reference to the main camera
      [SerializeField] private Camera _mainCam;
#pragma warning restore CS0649
      #endregion
   }

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
}