using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

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
      public List<SpriteTemplateData> spriteTemplateList;

      #endregion

      private void Start () {
         self = this;
         spriteTemplateList = new List<SpriteTemplateData>();

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
               scaleText.text = scale.ToString("f1");
               cachedTemplate.spriteTemplateData.scaleAlteration = scale;
               cachedTemplate.transform.localScale = new Vector3(scale, scale, scale);
            }
         });

         rotationSlider.onValueChanged.AddListener(rotation => {
            if (cachedTemplate != null) {
               rotationText.text = rotation.ToString("f1");
               cachedTemplate.spriteTemplateData.rotationAlteration = rotation;
               cachedTemplate.transform.localEulerAngles = new Vector3(0, 0, rotation);
            }
         });

         layerSlider.onValueChanged.AddListener(currLayer => {
            if (cachedTemplate != null) {
               layerText.text = currLayer.ToString();
               cachedTemplate.spriteTemplateData.layerIndex = (int) currLayer;
               cachedTemplate.spriteRender.sortingOrder = (int) currLayer;
            }
         });
      }

      public void beginHoverObj (SpriteTemplate spriteTemp) {
         if (!isDragging) {
            summaryContentHolder.gameObject.SetActive(true);
            summaryContentHolder.position = new Vector3(spriteTemp.transform.position.x, spriteTemp.transform.position.y, 0);
         }
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
         _initialOffset = spriteTemp.transform.position - pos;

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
            if (!cachedTemplate.spriteTemplateData.isLocked) {
               Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);
               draggedObj.transform.position = new Vector3(_initialOffset.x + pos.x, _initialOffset.y + pos.y, 0);
            }

            if (Input.GetKeyUp(KeyCode.Mouse0)) {
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

      public void createInstance (Sprite spriteContent, string newSpritePath) {
         creationCanvas.gameObject.SetActive(false);

         Vector3 pos = _mainCam.ScreenToWorldPoint(Input.mousePosition);
         GameObject obj = Instantiate(spritePrefab, contentHolder);
         obj.transform.position = pos;

         SpriteTemplate spriteTemplate = obj.GetComponent<SpriteTemplate>();
         spriteTemplate.spriteRender.sprite = spriteContent;
         spriteTemplate.gameObject.AddComponent<BoxCollider2D>();

         spriteTemplate.spriteTemplateData.scaleAlteration = 1;
         spriteTemplate.spriteTemplateData.rotationAlteration = 0;
         spriteTemplate.spriteTemplateData.layerIndex = 0;
         spriteTemplate.spriteTemplateData.isLocked = false;
         spriteTemplate.spriteTemplateData.spritePath = newSpritePath;

         spriteTemplateList.Add(spriteTemplate.spriteTemplateData);

         spriteTemplate.OnMouseDown();
         isNewlySpawned = true;
         obj.SetActive(true);
         stopHoverObj();
      }

      #region Private Variables

      // Initial offset of the sprite location
      private Vector3 _initialOffset;

      // Reference to the main camera
      [SerializeField] private Camera _mainCam;

      #endregion
   }
}