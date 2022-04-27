using System;
using UnityEngine;

[Serializable]
public class TileAttributesMatrix
{
   #region Public Variables

   // Maximum size that the matrix can be
   public const int MAX_WIDTH = 256;
   public const int MAX_HEIGHT = 256;

   // Current size of attributes matrix
   public Vector2Int attributesMatrixSize
   {
      get
      {
         if (_attributesMatrixHeight == 0) {
            return Vector2Int.zero;
         }
         return new Vector2Int(_attributesMatrix.Length / _attributesMatrixHeight, _attributesMatrixHeight);
      }
   }

   #endregion

   public TileAttributes getTileAttributesMatrixElement (Vector2Int index) {
      if (index.x >= MAX_WIDTH || index.y >= MAX_HEIGHT) {
         return null;
      }

      if (index.x < 0 || index.y < 0 || index.x >= attributesMatrixSize.x || index.y >= attributesMatrixSize.y) {
         return null;
      }

      return _attributesMatrix[index.x + index.y * attributesMatrixSize.x];
   }

   public bool hasAttribute (Vector2Int index, TileAttributes.Type type) {
      TileAttributes a = getTileAttributesMatrixElement(index);
      if (a != null) {
         if (a.hasAttribute(type)) {
            return true;
         }
      }

      return false;
   }

   public void logAttributes (Vector2Int index, string prefix) {
      TileAttributes a = getTileAttributesMatrixElement(index);
      if (a == null) {
         prefix += "null";
      } else {
         for (int i = 0; i < a.attributes.Count; i++) {
            prefix += a.attributes[i].ToString() + " ";
         }
      }
      Debug.Log(prefix);
   }

   public int getAttributes (Vector2Int index, TileAttributes.Type[] attributeBuffer) {
      TileAttributes a = getTileAttributesMatrixElement(index);
      if (a == null) {
         return 0;
      }
      for (int i = 0; i < a.attributes.Count && i < attributeBuffer.Length; i++) {
         attributeBuffer[i] = a.attributes[i];
      }
      return Mathf.Min(a.attributes.Count, attributeBuffer.Length);
   }

   public void addAttributes (Vector2Int index, TileAttributes attributes) {
      foreach (TileAttributes.Type t in attributes.attributes) {
         addAttribute(index, t);
      }
   }

   public bool addAttribute (Vector2Int index, TileAttributes.Type attribute) {
      if (index.x >= MAX_WIDTH || index.y >= MAX_HEIGHT || index.x < 0 || index.y < 0) {
         return false;
      }

      bool expanded = encapsulate(index.x, index.y);
      bool modified = getTileAttributesMatrixElement(index).addAttribute(attribute);

      return expanded || modified;
   }

   public bool removeAttribute (Vector2Int index, TileAttributes.Type attribute) {
      if (index.x >= MAX_WIDTH || index.y >= MAX_HEIGHT || index.x < 0 || index.y < 0) {
         return false;
      }

      bool expanded = encapsulate(index.x, index.y);
      bool modified = getTileAttributesMatrixElement(index).removeAttribute(attribute);

      return expanded || modified;
   }

   private bool encapsulate (int x, int y) {
      if (x < 0 || y < 0 || x >= MAX_HEIGHT || y >= MAX_HEIGHT || (x < attributesMatrixSize.x && y < attributesMatrixSize.y)) {
         return false;
      }

      resize(Mathf.Max(x + 1, attributesMatrixSize.x), Mathf.Max(y + 1, attributesMatrixSize.y));
      return true;
   }

   private void resize (int width, int height) {
      if (width > MAX_WIDTH || height > MAX_HEIGHT) {
         D.warning("Tile Attribute Matrix is being forced to be bigger than max: " + width + ", " + height);
      }

      TileAttributes[] old = _attributesMatrix.Clone() as TileAttributes[];
      int oldHeight = attributesMatrixSize.y;
      int oldWidth = attributesMatrixSize.x;

      _attributesMatrixHeight = height;
      int matrixWidth = width;
      _attributesMatrix = new TileAttributes[_attributesMatrixHeight * matrixWidth];

      for (int i = 0; i < _attributesMatrix.Length; i++) {
         // Extract matrix x and y coors
         Vector2Int coors = new Vector2Int(i % matrixWidth, i / matrixWidth);

         if (coors.x < oldWidth && coors.y < oldHeight) {
            // Apply old element
            _attributesMatrix[i] = old[coors.x + oldWidth * coors.y];
         }
      }

      for (int i = 0; i < _attributesMatrix.Length; i++) {
         if (_attributesMatrix[i] == null) {
            _attributesMatrix[i] = new TileAttributes();
         }
      }
   }

   #region Private Variables

   // What is the height of the tile attributes matrix - calculate length from it
   [SerializeField, HideInInspector]
   private int _attributesMatrixHeight;

   // Assigned tile attributes
   [SerializeField, HideInInspector]
   private TileAttributes[] _attributesMatrix = new TileAttributes[0];

   #endregion
}
