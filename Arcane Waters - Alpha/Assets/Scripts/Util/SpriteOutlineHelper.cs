using System.Linq;
using UnityEngine;

public class SpriteOutlineHelper : MonoBehaviour
{
   #region Public Variables

   // The color of the outline. It's a property for backwards compatibility reasons. It's white by default.
   public Color color { 
      get {
         return _color;
      }

      set {
         setNewColor(value);
      }
   }
   
   // A reference to a shader that supports outline if the current one doesn't support outlining.
   // We assume the supplied supports outlining.
   public Shader shader;

   // The names of the children that shouldn't be outlined
   public string[] ignoredChildrenNames;

   #endregion

   private void Awake () {
      
      _renderers = GetComponentsInChildren<SpriteRenderer>();
      _materials = new Material[_renderers.Length];

      // Make sure every renderer that should be outlined has a shader that supports outlining
      for (int i = 0; i < _renderers.Length; i++) {
         // Ignore children in the ignoredChildrenNames array
         if (ignoredChildrenNames.Length > 0 && ignoredChildrenNames.Contains(_renderers[i].gameObject.name)) { continue; }

         SpriteRenderer sr = _renderers[i];

         // If the shader doesn't have an "_OutlineEnabled" property, we can assume it doesn't support outlining
         if (!sr.material.HasProperty("_OutlineEnabled")) {
            // Find the default shader of preference if no reference was set in the inspector
            if (shader == null) {
               shader = Shader.Find(DEFAULT_SHADER);
            }

            // Create a material from the shader
            sr.material = new Material(shader);   
         }
         
         _materials[i] = sr.sharedMaterial;
      }
      
      // Set the color to the materials
      setNewColor(color);

      // Disable the outline by default
      disableOutline();
   }

   public void enableOutline () {
      for (int i = 0; i < _materials.Length; i++) {
         if (!_materials[i]) continue;

         _renderers[i].material.SetInt("_OutlineEnabled", 1);
      }
   }

   public void disableOutline () {
      for (int i = 0; i < _materials.Length; i++) {
         if (!_materials[i]) continue;

         _renderers[i].material.SetInt("_OutlineEnabled", 0);
      }
   }

   public void setVisibility (bool visible) {
      if (visible) {
         enableOutline();
      } else if (!_isVisible) {
         disableOutline();
      }
   }

   public void setNewColor(Color color) {
      _color = color;
      setRenderersColor(color);
   }

   [System.Obsolete("This method is no longer required. It exists for backwards compatibility reasons.")]
   public void recreateOutlineIfVisible() {

   }

   [System.Obsolete("This method is obsolete. Please use disableOutline() instead.")]
   public void Hide() {
      disableOutline();
   }

   private void setRenderersColor(Color color) {
      for (int i = 0; i < _materials.Length; i++) {
         if (!_materials[i]) continue;

         _renderers[i].material.SetColor("_OutlineColor", color);
      }
   }

   private void Update() {
      if (_wasVisible != _isVisible) {
         setVisibility(_isVisible);
         _wasVisible = _isVisible;     
      }   
   }

   #region Private Variables

   // References to the renderers of this gameObject and its children
   private SpriteRenderer[] _renderers;

   // References to the materials of every SpriteRenderer in renderers[]
   private Material[] _materials;

   // The outline color (internal use only)
   private Color _color = new Color(1, 1, 1, 1);

   // For testing purposes only. Enable/Disable outline.
   [SerializeField] private bool _isVisible;

   // Was the outline enabled in the previous frame?
   private bool _wasVisible;

   // The default shader to use if the one currently in use doesn't support outlining and no other shader was supplied in the inspector
   private const string DEFAULT_SHADER = "Arcane Waters/Single Sprite Outlined";

   #endregion
}
