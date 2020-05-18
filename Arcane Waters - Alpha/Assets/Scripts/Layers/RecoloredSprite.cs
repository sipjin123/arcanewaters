using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class RecoloredSprite : MonoBehaviour {
   #region Public Variables

   #endregion

   public virtual void Awake () {
      _image = GetComponent<Image>();
      _spriteRenderer = GetComponent<SpriteRenderer>();

      // Look up the Sprite based on the type of component we use
      if (_image) {
         _sprite = _image.sprite;
      } else if (_spriteRenderer) {
         _sprite = _spriteRenderer.sprite;
      }
   }

   public void recolor (ColorKey colorKey, ColorType color1, ColorType color2, MaterialType materialType = MaterialType.None, bool isGUI = false) {
      _color1 = color1;
      _color2 = color2;

      // We get a different material for GUI Images
      Material material = (_image != null) ?
         MaterialManager.self.getGUIMaterial(colorKey) : MaterialManager.self.get(colorKey);

      // Some layers, like the Body layer, aren't recolored
      if (material == null && materialType == MaterialType.None) {
         return;
      }

      // Assign the color associated with our color id from the database
      if (!isGUI) {
         Material newMaterial = materialType != MaterialType.None ? MaterialManager.self.translateMaterial(materialType) : material;
         setNewMaterial(newMaterial);
      } else {
         Material newMaterial = materialType != MaterialType.None ? MaterialManager.self.translateGUIMaterial(materialType) : material;
         setNewMaterial(newMaterial);
      }
   }

   public void setNewMaterial (Material oldMaterial) {
      Material newMaterial = new Material(oldMaterial);

      if (_spriteRenderer != null) {
         _spriteRenderer.material = newMaterial;
      } else if (_image != null) {
         _image.material = newMaterial;
      }

      Color primaryColor = ColorDef.get(_color1).color;
      Color secondaryColor = ColorDef.get(_color2).color;
      newMaterial.SetColor("_NewColor", primaryColor);
      newMaterial.SetColor("_NewColor2", secondaryColor);
      newMaterial.SetFloat("_Range", oldMaterial.GetFloat("_Range"));
      newMaterial.SetFloat("_Range2", oldMaterial.GetFloat("_Range2"));
   }

   public Material getMaterial () {
      if (_spriteRenderer != null) {
         return _spriteRenderer.material;
      } else if (_image != null) {
         return _image.material;
      }

      return null;
   }

   public ColorType getColor1 () {
      return _color1;
   }

   public ColorType getColor2 () {
      return _color2;
   }

   public void recolor (ColorType newColor1, ColorType newColor2) {
      recolor(getMaterial(), newColor1, newColor2);
   }

   public void recolor (Material material, ColorType newColor1, ColorType newColor2) {
      _color1 = newColor1;
      _color2 = newColor2;
      
      material.SetColor("_NewColor", ColorDef.get(newColor1).color);
      material.SetColor("_NewColor2", ColorDef.get(newColor2).color);
   }

   #region Private Variables

   // Our Sprite
   protected Sprite _sprite;

   // Our colors
   protected ColorType _color1;
   protected ColorType _color2;

   // Our Sprite Renderer or Image, whichever we have
   protected SpriteRenderer _spriteRenderer;
   protected Image _image;

   #endregion
}
