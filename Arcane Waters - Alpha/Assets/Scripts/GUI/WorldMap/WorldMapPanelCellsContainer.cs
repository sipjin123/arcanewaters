using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorldMapPanelCellsContainer : MonoBehaviour
{
   #region Public Variables

   // Prefab used to create cells
   public GameObject cellPrefab;

   #endregion

   private void clearCells () {
      foreach (WorldMapPanelCell cell in _cells.Values) {
         destroyCell(cell);
      }

      _cells.Clear();
   }

   public void fill () {
      clearCells();

      for (int row = 0; row < WorldMapPanel.self.mapDimensions.y; row++) {
         for (int col = 0; col < WorldMapPanel.self.mapDimensions.x; col++) {
            createCell(new WorldMapPanelAreaCoords(col, row));
         }
      }
   }

   private WorldMapPanelCell createCell (WorldMapPanelAreaCoords coords) {
      GameObject cellGo = Instantiate(cellPrefab);
      WorldMapPanelCell cell = cellGo.GetComponent<WorldMapPanelCell>();
      parentCell(cell);
      resizeCell(cell);
      positionCell(coords, cell);
      registerCell(coords, cell);
      setupCell(cell);

      return cell;
   }

   private bool findCell (WorldMapPanelAreaCoords coords, out WorldMapPanelCell cell) {
      if (_cells.TryGetValue(coords, out WorldMapPanelCell c)) {
         cell = c;
         return true;
      }

      cell = null;
      return false;
   }

   private bool findKey (WorldMapPanelCell cell, out WorldMapPanelAreaCoords coords) {
      foreach (KeyValuePair<WorldMapPanelAreaCoords, WorldMapPanelCell> cellPairs in _cells) {
         if (cellPairs.Value == cell) {
            coords = cellPairs.Key;
            return true;
         }
      }

      coords = new WorldMapPanelAreaCoords();
      return false;
   }

   private bool registerCell (WorldMapPanelAreaCoords coords, WorldMapPanelCell cell) {
      if (_cells.ContainsKey(coords)) {
         return false;
      }

      _cells.Add(coords, cell);
      cell.coords = coords;
      cell.gameObject.name = $"WorldMapPanelCell_{coords}";
      return true;
   }

   private bool positionCell (WorldMapPanelAreaCoords coords, WorldMapPanelCell cell) {
      RectTransform rectTransform = cell.transform.GetComponent<RectTransform>();

      if (rectTransform == null) {
         return false;
      }

      rectTransform.localPosition = new Vector3(coords.x * WorldMapPanel.self.cellSize.x, -coords.y * WorldMapPanel.self.cellSize.y, 0);
      return true;
   }

   private void parentCell (WorldMapPanelCell cell) {
      cell.transform.SetParent(transform);
   }

   private bool resizeCell (WorldMapPanelCell cell) {
      RectTransform rectTransform = cell.GetComponent<RectTransform>();

      if (rectTransform == null) {
         return false;
      }

      rectTransform.sizeDelta = new Vector2(WorldMapPanel.self.cellSize.x, WorldMapPanel.self.cellSize.y);
      return true;
   }

   private void setupCell (WorldMapPanelCell cell) {
      cell.button.onClick.AddListener(() => {
         onCellClicked(cell);
      });
   }

   private void destroyCell (WorldMapPanelCell cell) {
      cell.button.onClick.RemoveAllListeners();
      Destroy(cell.gameObject);
   }

   private void onCellClicked (WorldMapPanelCell cell) {
      if (findKey(cell, out WorldMapPanelAreaCoords cellCoords)) {
         WorldMapPanel.self.onAreaPressed(cellCoords);
         _selectedCell = cell;
      }
   }

   public void focus () {
      if (_selectedCell != null) {
         _selectedCell.button.Select();
      }
   }

   public void blur () {
      if (_selectedCell != null) {
         _selectedCell = null;
      }

      EventSystem.current.SetSelectedGameObject(null);
   }

   #region Private Variables

   // Cells registry
   private Dictionary<WorldMapPanelAreaCoords, WorldMapPanelCell> _cells = new Dictionary<WorldMapPanelAreaCoords, WorldMapPanelCell>();

   // Currently selected cell
   private WorldMapPanelCell _selectedCell;

   #endregion
}
