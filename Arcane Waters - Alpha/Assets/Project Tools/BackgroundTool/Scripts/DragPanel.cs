using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BackgroundTool
{
   public class DragPanel : MonoBehaviour
   {
      #region Public Variables

      // Start position of the mouse drag
      public Vector3 startPosition;

      // End position of the mouse drag
      public Vector3 endPosition;

      // Box to indicate the drag window
      public Transform draggableBox;

      // Determines if the drag action is active
      public bool isDragging;

      // Determines the panel type
      public DragPanelType panelType;

      public enum DragPanelType
      {
         None = 0,
         SpriteSelection = 1,
         SpritesSpawned = 2
      }

      // Determines the hold duration
      public float dragCounter;

      // Max count while holding before processing the drag
      public static float holdTimerMax = .15f;

      #endregion

      public void OnMouseDown () {
         if (ImageManipulator.self.draggedObjList.Count > 0) {
            ImageManipulator.self.isHoveringHighlight = false;
         }

         if (!EventSystem.current.IsPointerOverGameObject() &&
            EventSystem.current.currentSelectedGameObject == null &&
            !ImageManipulator.self.disableHighlighting) {
            Vector3 pos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            pos.z = 0;
            dragCounter = 0;

            startPosition = pos;
            draggableBox.position = pos;
            draggableBox.gameObject.SetActive(true); 
            isDragging = true;
         }
      }

      private void Update () {
         if (isDragging && !ImageManipulator.self.disableHighlighting) {
            dragCounter += Time.deltaTime;
            if (dragCounter >= holdTimerMax) {
               Vector3 currentPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
               Vector3 selectionAreaSize = currentPos - startPosition;
               selectionAreaSize.x *= 100;
               selectionAreaSize.y *= 100;
               draggableBox.localScale = new Vector3 { x = selectionAreaSize.x, y = selectionAreaSize.y, z = 1 };

               Vector3 lowerLeftPos = new Vector3(Mathf.Min(startPosition.x, currentPos.x), Mathf.Min(startPosition.y, currentPos.y), 0);
               Vector3 upperRightPos = new Vector3(Mathf.Max(startPosition.x, currentPos.x), Mathf.Max(startPosition.y, currentPos.y), 0);

               foreach (Transform child in ImageLoader.self.spriteParent) {
                  if (child.position.x >= lowerLeftPos.x &&
                     child.position.y >= lowerLeftPos.y &&
                     child.position.x <= upperRightPos.x &&
                     child.position.y <= upperRightPos.y) {
                     child.GetComponent<SpriteRenderer>().color = Color.red;
                  } else {
                     child.GetComponent<SpriteRenderer>().color = Color.white;
                  }
               }
            }
         }
      }

      public void OnMouseUp () {
         if (isDragging) {
            List<SpriteTemplate> spawnedSpriteList = new List<SpriteTemplate>();
            List<SpriteSelectionTemplate> spriteSelectionList = new List<SpriteSelectionTemplate>();
            endPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            dragCounter = 0;

            Vector3 lowerLeftPos = new Vector3(Mathf.Min(startPosition.x, endPosition.x), Mathf.Min(startPosition.y, endPosition.y), 0);
            Vector3 upperRightPos = new Vector3(Mathf.Max(startPosition.x, endPosition.x), Mathf.Max(startPosition.y, endPosition.y), 0);

            Transform spriteParent = null;
            if (panelType == DragPanelType.SpriteSelection) {
               spriteParent = ImageLoader.self.spriteParent;
            } else if (panelType == DragPanelType.SpritesSpawned) {
               spriteParent = ImageManipulator.self.contentHolder;
            }

            foreach (Transform child in spriteParent) {
               if (child.position.x >= lowerLeftPos.x && child.position.y >= lowerLeftPos.y &&
                  child.position.x <= upperRightPos.x && child.position.y <= upperRightPos.y) {
                  // Gather the template lists within the dragged box
                  if (panelType == DragPanelType.SpriteSelection) {
                     spriteSelectionList.Add(child.GetComponent<SpriteSelectionTemplate>());
                  } else if (panelType == DragPanelType.SpritesSpawned) {
                     spawnedSpriteList.Add(child.GetComponent<SpriteTemplate>());
                  }
                  child.GetComponent<SpriteRenderer>().color = Color.white;
               }
            }

            // Send to image manipulator the list of sprites within the dragged box
            if (panelType == DragPanelType.SpriteSelection && spriteSelectionList.Count > 0) {
               ImageManipulator.self.beginDragSelectionGroup(spriteSelectionList, false);
            } else if (panelType == DragPanelType.SpritesSpawned && spawnedSpriteList.Count > 0) {
               ImageManipulator.self.beginDragSpawnedGroup(spawnedSpriteList, false);
            }

            draggableBox.localScale = Vector3.zero;
            draggableBox.gameObject.SetActive(false);
            isDragging = false;
         }
      }

      #region Private Variables

      #endregion
   }
}