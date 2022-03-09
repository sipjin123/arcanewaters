using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorldMapPanelHoveredAreaSelectorContainer : MonoBehaviour
{
   #region Public Variables

   // Prefab used to create cells
   public GameObject cellPrefab;


   #endregion

   private void clearCells () {
      foreach (Button cell in _cells.Values) {
         destroyCell(cell);
      }

      _cells.Clear();
   }

   public void fill () {
      clearCells();

      for (int row = 0; row < WorldMapPanel.self.mapDimensions.y; row++) {
         for (int col = 0; col < WorldMapPanel.self.mapDimensions.x; col++) {
            createCell(new Vector2Int(col, row));
         }
      }
   }

   private Button createCell (Vector2Int position) {
      GameObject cellGo = Instantiate(cellPrefab);
      Button cell = cellGo.GetComponent<Button>();
      parentCell(cell);
      resizeCell(cell);
      positionCell(position, cell);
      registerCell(position, cell);
      setupCell(cell);

      return cell;
   }

   private bool findCell (Vector2Int location, out Button cell) {
      if (_cells.TryGetValue(location, out  Button c)) {
         cell = c;
         return true;
      }

      cell = null;
      return false;
   }

   private bool findKey (Button cell, out Vector2Int key) {
      foreach (KeyValuePair<Vector2Int, Button> cellPairs in _cells) {
         if (cellPairs.Value == cell) {
            key = cellPairs.Key;
            return true;
         }
      }

      key = new Vector2Int();
      return false;
   }

   private bool registerCell (Vector2Int position, Button cell) {
      if (_cells.ContainsKey(position)) {
         return false;
      }

      _cells.Add(position, cell);
      cell.gameObject.name = $"WorldMapPanelCell_{position.x}_{position.y}";
      return true;
   }

   private bool positionCell (Vector2Int position, Button cell) {
      RectTransform rectTransform = cell.transform.GetComponent<RectTransform>();

      if (rectTransform == null) {
         return false;
      }

      rectTransform.localPosition = new Vector3(position.x * WorldMapPanel.self.cellSize.x, -position.y * WorldMapPanel.self.cellSize.y, 0);
      return true;
   }

   private void parentCell (Button cell) {
      cell.transform.SetParent(transform);
   }

   private bool resizeCell (Button cell) {
      RectTransform rectTransform = cell.GetComponent<RectTransform>();

      if (rectTransform == null) {
         return false;
      }

      rectTransform.sizeDelta = new Vector2(WorldMapPanel.self.cellSize.x, WorldMapPanel.self.cellSize.y);
      return true;
   }

   private void setupCell(Button cell) {
      cell.onClick.AddListener(() => { 
         onCellClicked(cell);
      });
   }

   private void destroyCell (Button cell) {
      cell.onClick.RemoveAllListeners();
      Destroy(cell.gameObject);
   }

   private void onCellClicked(Button cell) {
      if (findKey(cell, out Vector2Int cellKey)) {
         WorldMapPanel.self.onMapCellClicked(cellKey);
      }
   }

   #region Private Variables

   // Cells registry
   public Dictionary<Vector2Int, Button> _cells = new Dictionary<Vector2Int, Button>(); 

   #endregion
}
