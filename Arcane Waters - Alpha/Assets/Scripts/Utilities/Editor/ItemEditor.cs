using UnityEngine;
using UnityEditor;
using ItemEditor.Layout.Style;
using System;

// Christopher Palacios

namespace ItemEditor
{
   /// <summary>
   /// This class just holds wrapper methods so that we do not clutter too much code when actually building
   /// a custom editor window.
   /// </summary>
   public static class ItemEditorLayout
   {
      public static void horizontal (Action block, params GUILayoutOption[] options) {
         EditorGUILayout.BeginHorizontal(options);
         block();
         EditorGUILayout.EndHorizontal();
      }

      public static void vertical (Action block, params GUILayoutOption[] options) {
         EditorGUILayout.BeginVertical(options);
         block();
         EditorGUILayout.EndVertical();
      }

      public static void horizontalBox (Action block, params GUILayoutOption[] options) {
         GUILayout.BeginHorizontal(ItemEditorStyle.getCustomStyle(ItemEditorLayoutStyles.BoxSub));
         block();
         GUILayout.EndHorizontal();
      }

      public static void horizontallyCentered (Action block) {
         horizontal(() => {
            GUILayout.FlexibleSpace();
            block();
            GUILayout.FlexibleSpace();
         });
      }

      public static void horizontalHelpbox (Action block, params GUILayoutOption[] options) {
         GUILayout.BeginHorizontal(EditorStyles.helpBox, options);
         block();
         GUILayout.EndHorizontal();
      }

      public static void header (string title, Color color = default(Color)) {
         EditorGUILayout.Space();

         color = color == default(Color) ? Color.white : color;
         horizontalBox(() => horizontallyCentered(() => Label.bold(title, color)));

         EditorGUILayout.Space();
      }

      public static void centeredLabel (string txt, Color color = default(Color)) {
         color = color == default(Color) ? Color.white : color;

         Label.centered(txt, color);
      }

      /* COMPONENTS */
      /* Label component */

      public static class Label
      {
         public static void centered (string label, Color labelColor = default(Color), params GUILayoutOption[] layouts) {
            custom(label, ItemEditorLayoutStyles.CenteredLabel, labelColor, layouts);
         }

         public static void H2 (string label, Color labelColor = default(Color), params GUILayoutOption[] layouts) {
            custom(label, ItemEditorLayoutStyles.H2, labelColor, layouts);
         }

         public static void H3 (string label, Color labelColor = default(Color), params GUILayoutOption[] layouts) {
            custom(label, ItemEditorLayoutStyles.H3, labelColor, layouts);
         }

         public static void H4 (string label, Color labelColor = default(Color), params GUILayoutOption[] layouts) {
            custom(label, ItemEditorLayoutStyles.H4, labelColor, layouts);
         }

         public static void bold (string label, params GUILayoutOption[] layouts) {
            custom(label, ItemEditorLayoutStyles.BoldLabel, default(Color), layouts);
         }

         public static void bold (string label, Color labelColor = default(Color), params GUILayoutOption[] layouts) {
            custom(label, ItemEditorLayoutStyles.BoldLabel, labelColor, layouts);
         }

         public static void custom (string label, params GUILayoutOption[] layouts) {
            custom(label, ItemEditorLayoutStyles.Label, default(Color), layouts);
         }

         public static void custom (string label, Color labelColor = default(Color), params GUILayoutOption[] layouts) {
            custom(label, ItemEditorLayoutStyles.Label, labelColor, layouts);
         }

         public static void custom (GUIContent content, Color color = default(Color), params GUILayoutOption[] layouts) {
            custom(content, ItemEditorLayoutStyles.Label, color, layouts);
         }

         public static void custom (GUIContent content, params GUILayoutOption[] layouts) {
            custom(content, ItemEditorLayoutStyles.Label, default(Color), layouts);
         }

         public static void bold (GUIContent content, params GUILayoutOption[] layouts) {
            custom(content, ItemEditorLayoutStyles.BoldLabel, default(Color), layouts);
         }

         public static void bold (GUIContent content, Color labelColor = default(Color),
             params GUILayoutOption[] layouts) {
            custom(content, ItemEditorLayoutStyles.BoldLabel, labelColor, layouts);
         }

         public static void custom (string label, ItemEditorLayoutStyles style = ItemEditorLayoutStyles.Label,
             Color labelColor = default(Color),
             params GUILayoutOption[] layouts) {
            var customSyle = ItemEditorStyle.getCustomStyle(style);
            customSyle.normal.textColor = labelColor != default(Color) ? labelColor : customSyle.normal.textColor;
            GUILayout.Label(label, customSyle, layouts);
         }

         public static void custom (GUIContent content, ItemEditorLayoutStyles style = ItemEditorLayoutStyles.Label,
             Color labelColor = default(Color), params GUILayoutOption[] layouts) {
            var customSyle = ItemEditorStyle.getCustomStyle(style);
            customSyle.normal.textColor = labelColor != default(Color) ? labelColor : customSyle.normal.textColor;
            GUILayout.Label(content, customSyle, layouts);
         }
      }
   }
}
