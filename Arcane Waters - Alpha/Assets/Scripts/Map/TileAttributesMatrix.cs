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
   public int attributeMatrixWidth
   {
      get
      {
         if (_attributesMatrixHeight == 0) {
            return 0;
         }
         return _attributesMatrix.Length / _attributesMatrixHeight;
      }
   }

   public int attributeMatrixHeight
   {
      get { return _attributesMatrixHeight; }
   }

   #endregion

   public TileAttributesMatrix () {

   }

   public TileAttributesMatrix (int width, int height) {
      if (width > MAX_WIDTH || height > MAX_HEIGHT) {
         D.warning("Tile Attribute Matrix is being forced to be bigger than max: " + width + ", " + height);
      }

      _attributesMatrix = new TileAttributes[width * height];
      _attributesMatrixHeight = height;

      for (int i = 0; i < _attributesMatrix.Length; i++) {
         _attributesMatrix[i] = new TileAttributes();
      }
   }

   public TileAttributes getTileAttributesMatrixElement (int xIndex, int yIndex) {
      if (xIndex >= MAX_WIDTH || yIndex >= MAX_HEIGHT) {
         return null;
      }

      if (xIndex < 0 || yIndex < 0 || xIndex >= attributeMatrixWidth || yIndex >= attributeMatrixHeight) {
         return null;
      }

      return _attributesMatrix[xIndex + yIndex * attributeMatrixWidth];
   }

   public bool hasAttribute (int xIndex, int yIndex, TileAttributes.Type type) {
      TileAttributes a = getTileAttributesMatrixElement(xIndex, yIndex);
      if (a != null) {
         if (a.hasAttribute(type)) {
            return true;
         }
      }

      return false;
   }

   public void logAttributes (int xIndex, int yIndex, string prefix) {
      TileAttributes a = getTileAttributesMatrixElement(xIndex, yIndex);
      if (a == null) {
         prefix += "null";
      } else {
         for (int i = 0; i < a.attributes.Count; i++) {
            prefix += a.attributes[i].ToString() + " ";
         }
      }
      Debug.Log(prefix);
   }

   public int getAttributes (int xIndex, int yIndex, TileAttributes.Type[] attributeBuffer) {
      TileAttributes a = getTileAttributesMatrixElement(xIndex, yIndex);
      if (a == null) {
         return 0;
      }
      for (int i = 0; i < a.attributes.Count && i < attributeBuffer.Length; i++) {
         attributeBuffer[i] = a.attributes[i];
      }
      return Mathf.Min(a.attributes.Count, attributeBuffer.Length);
   }

   public void addAttributes (int xIndex, int yIndex, TileAttributes attributes) {
      for (int i = 0; i < attributes.attributes.Count; i++) {
         addAttribute(xIndex, yIndex, attributes.attributes[i]);
      }
   }

   public bool addAttribute (int xIndex, int yIndex, TileAttributes.Type attribute) {
      if (xIndex >= MAX_WIDTH || yIndex >= MAX_HEIGHT || xIndex < 0 || yIndex < 0) {
         return false;
      }

      bool expanded = encapsulate(xIndex, yIndex);
      bool modified = getTileAttributesMatrixElement(xIndex, yIndex).addAttribute(attribute);

      return expanded || modified;
   }

   public bool removeAttribute (int xIndex, int yIndex, TileAttributes.Type attribute) {
      if (xIndex >= MAX_WIDTH || yIndex >= MAX_HEIGHT || xIndex < 0 || yIndex < 0) {
         return false;
      }

      bool expanded = encapsulate(xIndex, yIndex);
      bool modified = getTileAttributesMatrixElement(xIndex, yIndex).removeAttribute(attribute);

      return expanded || modified;
   }

   private bool encapsulate (int x, int y) {
      if (x < 0 || y < 0 || x >= MAX_HEIGHT || y >= MAX_HEIGHT || (x < attributeMatrixWidth && y < attributeMatrixHeight)) {
         return false;
      }

      resize(Mathf.Max(x + 1, attributeMatrixWidth), Mathf.Max(y + 1, attributeMatrixHeight));
      return true;
   }

   private void resize (int width, int height) {
      if (width > MAX_WIDTH || height > MAX_HEIGHT) {
         D.warning("Tile Attribute Matrix is being forced to be bigger than max: " + width + ", " + height);
      }

      TileAttributes[] old = _attributesMatrix.Clone() as TileAttributes[];
      int oldHeight = attributeMatrixHeight;
      int oldWidth = attributeMatrixWidth;

      _attributesMatrixHeight = height;
      int matrixWidth = width;
      _attributesMatrix = new TileAttributes[_attributesMatrixHeight * matrixWidth];

      for (int i = 0; i < _attributesMatrix.Length; i++) {
         // Extract matrix x and y coors
         (int x, int y) coors = (i % matrixWidth, i / matrixWidth);

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
