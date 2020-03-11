using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;
using System;
using System.Linq;

public class TextMeshProLinkOpener : MonoBehaviour, IPointerClickHandler {
   #region Public Variables

   // The text that has the link
   public TextMeshProUGUI text;

   // Wether the color should change on hover
   public bool chageColorOnHover;

   // The color for the text when it's hovered over
   public Color hoverColor;

   #endregion

   public void OnPointerClick (PointerEventData eventData) {
      int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, Input.mousePosition, null);
      if (linkIndex != -1) {
         // The link was clicked
         TMP_LinkInfo linkInfo = text.textInfo.linkInfo[linkIndex];

         // Open the link id as a url
         Application.OpenURL(linkInfo.GetLinkID());
      }
   }

   void Update () {
      // Is the cursor in the correct region (above the text area) and furthermore, in the link region?
      bool isHoveringOver = TMP_TextUtilities.IsIntersectingRectTransform(text.rectTransform, Input.mousePosition, null);
      int linkIndex = isHoveringOver ? TMP_TextUtilities.FindIntersectingLink(text, Input.mousePosition, null) : -1;

      // Clear previous link selection if one existed.
      if (_currentLink != -1 && linkIndex != _currentLink) {
         setLinkToColor(_currentLink, (linkIdx, vertIdx) => _originalVertexColors[linkIdx][vertIdx]);
         _originalVertexColors.Clear();
         _currentLink = -1;
      }

      // Handle new link selection.
      if (linkIndex != -1 && linkIndex != _currentLink) {
         _currentLink = linkIndex;
         if (chageColorOnHover) {
            _originalVertexColors = setLinkToColor(linkIndex, (_linkIdx, _vertIdx) => hoverColor);
         }
      }
   }

   private List<Color32[]> setLinkToColor (int linkIndex, Func<int, int, Color32> colorForLinkAndVert) {
      TMP_LinkInfo linkInfo = text.textInfo.linkInfo[linkIndex];

      // Store the old character colors
      List<Color32[]> oldVertColors = new List<Color32[]>();

      for (int i = 0; i < linkInfo.linkTextLength; i++) {
         // The character index into the entire text
         int characterIndex = linkInfo.linkTextfirstCharacterIndex + i;
         TMP_CharacterInfo charInfo = text.textInfo.characterInfo[characterIndex];

         // The index of the material used by this character
         int meshIndex = charInfo.materialReferenceIndex;

         // The index of the first vertex of this character
         int vertexIndex = charInfo.vertexIndex;

         // The colors for this character
         Color32[] vertexColors = text.textInfo.meshInfo[meshIndex].colors32;
         oldVertColors.Add(vertexColors.ToArray());

         if (charInfo.isVisible) {
            vertexColors[vertexIndex + 0] = colorForLinkAndVert(i, vertexIndex + 0);
            vertexColors[vertexIndex + 1] = colorForLinkAndVert(i, vertexIndex + 1);
            vertexColors[vertexIndex + 2] = colorForLinkAndVert(i, vertexIndex + 2);
            vertexColors[vertexIndex + 3] = colorForLinkAndVert(i, vertexIndex + 3);
         }
      }

      // Update Geometry
      text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);

      return oldVertColors;
   }


   #region Private Variables

   // The index of the current link
   private int _currentLink = -1;

   // The list of colors of the original text
   private List<Color32[]> _originalVertexColors = new List<Color32[]>();

   #endregion
}
