using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapObjectStateVariables;
using System.Linq;
using MapCreationTool.Serialization;

namespace MapCreationTool.MapObjectStateVariables
{
   public class ObjectStateEditorPanel : UIPanel
   {
      #region Public Variables

      // The container which holds all the controls for manipulating state definition
      public Transform stateExpressionContainer = null;

      // Prefabs for various types of expression
      public ExpressionField expressionConstantPrefab = null;
      public ExpressionField expressionReferencePrefab = null;
      public ExpressionField expressionIfElsePrefab = null;
      public ExpressionField expression2MemberPrefab = null;
      public ExpressionField expressionNotPrefab = null;

      [Space(5)]
      // Object's name text
      public Text nameText = null;

      // Object's description text
      public Text descriptionText = null;

      // Object's default state input field
      public InputField defaultStateInputField = null;

      #endregion

      private void Start () {
         defaultStateInputField.onEndEdit.AddListener((i) => {
            defaultStateValueChanged();
         });
      }

      public void setValue (string value) {
         // Get the model
         _model = ObjectStateModel.deserializeOrDefault(value);

         // Clear out all the old controls
         for (int i = stateExpressionContainer.childCount - 1; i >= 0; i--) {
            Destroy(stateExpressionContainer.GetChild(i).gameObject);
         }

         // Instantiate all the controls based on given model
         instantiateExpression(_model.stateExpression, stateExpressionContainer, 0);

         // Set the default state value 
         defaultStateInputField.SetTextWithoutNotify(_model.state);
      }

      private void instantiateExpression (Expression expression, Transform container, int expressionDepth) {
         switch (expression.type) {
            case Expression.Type.Constant:
               instatiateExpression_Constant(expression, container, expressionDepth);
               break;
            case Expression.Type.Reference:
               instantiateExpression_Reference(expression, container, expressionDepth);
               break;
            case Expression.Type.Operation_IF:
               instantiateExpression_IfElse(expression, container, expressionDepth);
               break;
            case Expression.Type.Operation_EQUALS:
            case Expression.Type.Operation_AND:
            case Expression.Type.Operation_OR:
               instatiateExpression_2MemberExpression(expression, container, expressionDepth);
               break;
            case Expression.Type.Operation_NOT:
               instantiateExpression_Not(expression, container, expressionDepth);
               break;
         }
      }

      private void instatiateExpression_Constant (Expression expression, Transform container, int expressionDepth) {
         ExpressionField field = Instantiate(expressionConstantPrefab, container);
         field.GetComponent<Image>().color = getExpressionBackgroundColor(expressionDepth);
         instatiateExpression_SetTypeDropdown(field.expressionTypeDropdown, expression);

         field.constantInputField.SetTextWithoutNotify(expression.constant);
         field.constantInputField.onEndEdit.AddListener((i) => {
            expressionConstantFieldChanged(expression, field.constantInputField);
         });
      }

      private void instantiateExpression_Reference (Expression expression, Transform container, int expressionDepth) {
         ExpressionField field = Instantiate(expressionReferencePrefab, container);
         field.GetComponent<Image>().color = getExpressionBackgroundColor(expressionDepth);
         instatiateExpression_SetTypeDropdown(field.expressionTypeDropdown, expression);

         // Fetch all prefabs that are placed and can have state

         List<(string id, string name)> targetPrefabs = new List<(string, string)>();
         targetPrefabs.Add(("0", "Undefined"));
         foreach (PlacedPrefab p in DrawBoard.instance.getPlacedPrefabs()) {
            if (p.original.TryGetComponent(out PrefabDataDefinition def)) {
               if (def.hasVariableObjectState) {
                  string id = p.getData(DataField.PLACED_PREFAB_ID).ToLower().Trim();
                  targetPrefabs.Add((id, id + ": " + (string.IsNullOrWhiteSpace(def.title) ? def.gameObject.name : def.title)));
               }
            }
         }

         // Fill in options
         field.referenceDropdown.options = targetPrefabs.Select(t => new Dropdown.OptionData {
            text = t.name
         }).ToList();
         field.referenceDropdownIds = targetPrefabs.Select(t => t.id).ToArray();

         // Set value
         field.referenceDropdown.SetValueWithoutNotify(0);
         for (int i = 0; i < field.referenceDropdownIds.Length; i++) {
            if (field.referenceDropdownIds[i].Equals(expression.referenceId)) {
               field.referenceDropdown.SetValueWithoutNotify(i);
               break;
            }
         }

         field.referenceDropdown.onValueChanged.AddListener((i) => {
            expressionReferenceFieldChanged(expression, field);
         });
      }

      private void instantiateExpression_IfElse (Expression expression, Transform container, int expressionDepth) {
         ExpressionField field = Instantiate(expressionIfElsePrefab, container);
         field.GetComponent<Image>().color = getExpressionBackgroundColor(expressionDepth);
         instatiateExpression_SetTypeDropdown(field.expressionTypeDropdown, expression);

         instantiateExpression(expression.conditionExpression, field.childExpressionContainers[0], expressionDepth + 1);
         instantiateExpression(expression.leftSideExpression, field.childExpressionContainers[1], expressionDepth + 1);
         instantiateExpression(expression.rightSideExpression, field.childExpressionContainers[2], expressionDepth + 1);
      }

      private void instantiateExpression_Not (Expression expression, Transform container, int expressionDepth) {
         ExpressionField field = Instantiate(expressionNotPrefab, container);
         field.GetComponent<Image>().color = getExpressionBackgroundColor(expressionDepth);
         instatiateExpression_SetTypeDropdown(field.expressionTypeDropdown, expression);

         instantiateExpression(expression.leftSideExpression, field.childExpressionContainers[0], expressionDepth + 1);
      }

      private void instatiateExpression_2MemberExpression (Expression expression, Transform container, int expressionDepth) {
         ExpressionField field = Instantiate(expression2MemberPrefab, container);
         field.GetComponent<Image>().color = getExpressionBackgroundColor(expressionDepth);
         instatiateExpression_SetTypeDropdown(field.expressionTypeDropdown, expression);

         field.operatorLabel.text = getOperatorLabel(expression.type);
         instantiateExpression(expression.leftSideExpression, field.childExpressionContainers[0], expressionDepth + 1);
         instantiateExpression(expression.rightSideExpression, field.childExpressionContainers[1], expressionDepth + 1);
      }

      private void expressionReferenceFieldChanged (Expression exp, ExpressionField field) {
         // Check that our model hasn't changed and we still have the expression
         if (field != null && Expression.hasExpressionRecursive(exp, _model.stateExpression)) {
            if (field.referenceDropdown.value < field.referenceDropdownIds.Length) {
               exp.referenceId = field.referenceDropdownIds[field.referenceDropdown.value];

               // Notify that our value has changed
               if (_openedBy != null) {
                  _openedBy.mapObjectStateEditor_valueChanged(_model.serialize());
               }
            }
         }
      }

      private void defaultStateValueChanged () {
         // Check that our model hasn't changed and we still have the expression
         if (_model != null) {
            _model.state = defaultStateInputField.text;

            // Notify that our value has changed
            if (_openedBy != null) {
               _openedBy.mapObjectStateEditor_valueChanged(_model.serialize());
            }
         }
      }

      private void expressionConstantFieldChanged (Expression exp, InputField field) {
         // Check that our model hasn't changed and we still have the expression
         if (field != null && Expression.hasExpressionRecursive(exp, _model.stateExpression)) {
            exp.constant = field.text;

            // Notify that our value has changed
            if (_openedBy != null) {
               _openedBy.mapObjectStateEditor_valueChanged(_model.serialize());
            }
         }
      }

      private void expressionTypeDropdownChanged (Expression exp, Dropdown dropdown) {
         // Check that our model hasn't changed and we still have the expression
         if (dropdown != null && Expression.hasExpressionRecursive(exp, _model.stateExpression)) {
            // Change expression accordingly
            Expression.Type newType = getDropdownExpressionType(dropdown);

            // Can't handle none
            if (newType == Expression.Type.None) {
               throw new System.Exception("Can't handle none expression type");
            }

            // Change expression based on the new type, set a nice, user-friendly state
            exp.type = newType;
            exp.constant = "";
            exp.referenceId = "undf";
            exp.conditionExpression = null;
            exp.leftSideExpression = null;
            exp.rightSideExpression = null;

            switch (newType) {
               case Expression.Type.Operation_IF:
                  exp.conditionExpression = new Expression {
                     type = Expression.Type.Constant,
                     constant = "true"
                  };
                  exp.leftSideExpression = new Expression {
                     type = Expression.Type.Constant,
                     constant = "<if-true>"
                  };
                  exp.rightSideExpression = new Expression {
                     type = Expression.Type.Constant,
                     constant = "<if-false>"
                  };
                  break;
               case Expression.Type.Operation_EQUALS:
                  exp.leftSideExpression = new Expression { type = Expression.Type.Constant, constant = "1" };
                  exp.rightSideExpression = new Expression { type = Expression.Type.Constant, constant = "1" };
                  break;
               case Expression.Type.Operation_NOT:
                  exp.leftSideExpression = new Expression {
                     type = Expression.Type.Constant,
                     constant = "true"
                  };
                  break;
               case Expression.Type.Operation_AND:
               case Expression.Type.Operation_OR:
                  exp.leftSideExpression = new Expression {
                     type = Expression.Type.Constant,
                     constant = "true"
                  };
                  exp.rightSideExpression = new Expression {
                     type = Expression.Type.Constant,
                     constant = "false"
                  };
                  break;
                  //case Expression.Type.Operation_IF:
                  //   exp.conditionExpression = new Expression {
                  //      type = Expression.Type.Operation_EQUALS,
                  //      leftSideExpression = new Expression { type = Expression.Type.Constant, constant = "1" },
                  //      rightSideExpression = new Expression { type = Expression.Type.Constant, constant = "1" },
                  //   };
                  //   exp.leftSideExpression = new Expression {
                  //      type = Expression.Type.Constant,
                  //      constant = "<if-true>"
                  //   };
                  //   exp.rightSideExpression = new Expression {
                  //      type = Expression.Type.Constant,
                  //      constant = "<if-false>"
                  //   };
                  //   break;
                  //case Expression.Type.Operation_EQUALS:
                  //   exp.leftSideExpression = new Expression { type = Expression.Type.Constant, constant = "1" };
                  //   exp.rightSideExpression = new Expression { type = Expression.Type.Constant, constant = "1" };
                  //   break;
                  //case Expression.Type.Operation_NOT:
                  //   exp.leftSideExpression = new Expression {
                  //      type = Expression.Type.Operation_EQUALS,
                  //      leftSideExpression = new Expression { type = Expression.Type.Constant, constant = "1" },
                  //      rightSideExpression = new Expression { type = Expression.Type.Constant, constant = "1" },
                  //   };
                  //   break;
                  //case Expression.Type.Operation_AND:
                  //case Expression.Type.Operation_OR:
                  //   exp.leftSideExpression = new Expression {
                  //      type = Expression.Type.Operation_EQUALS,
                  //      leftSideExpression = new Expression { type = Expression.Type.Constant, constant = "1" },
                  //      rightSideExpression = new Expression { type = Expression.Type.Constant, constant = "1" },
                  //   };
                  //   exp.rightSideExpression = new Expression {
                  //      type = Expression.Type.Operation_EQUALS,
                  //      leftSideExpression = new Expression { type = Expression.Type.Constant, constant = "1" },
                  //      rightSideExpression = new Expression { type = Expression.Type.Constant, constant = "0" },
                  //   };
                  //   break;
            }

            // Notify that our value has changed
            if (_openedBy != null) {
               _openedBy.mapObjectStateEditor_valueChanged(_model.serialize());
            }
         }
      }

      private Expression.Type getDropdownExpressionType (Dropdown dropdown) {
         switch (dropdown.value) {
            case 0:
               return Expression.Type.Constant;
            case 1:
               return Expression.Type.Reference;
            case 2:
               return Expression.Type.Operation_IF;
            case 3:
               return Expression.Type.Operation_EQUALS;
            case 4:
               return Expression.Type.Operation_OR;
            case 5:
               return Expression.Type.Operation_AND;
            case 6:
               return Expression.Type.Operation_NOT;
         }
         return Expression.Type.None;
      }

      private void instatiateExpression_SetTypeDropdown (Dropdown dropdown, Expression expr) {
         dropdown.options = new List<Dropdown.OptionData> {
            new Dropdown.OptionData { text = "Constant" },
            new Dropdown.OptionData { text = "Object's state" },
            new Dropdown.OptionData { text = "Conditional" },
            new Dropdown.OptionData { text = "Equals" },
            new Dropdown.OptionData { text = "OR" },
            new Dropdown.OptionData { text = "AND" },
            new Dropdown.OptionData { text = "NOT" }
         };

         dropdown.onValueChanged.AddListener((i) => {
            expressionTypeDropdownChanged(expr, dropdown);
         });

         switch (expr.type) {
            case Expression.Type.None:
               throw new System.Exception("Can't handle 'none' expression");
            case Expression.Type.Constant:
               dropdown.SetValueWithoutNotify(0);
               break;
            case Expression.Type.Reference:
               dropdown.SetValueWithoutNotify(1);
               break;
            case Expression.Type.Operation_IF:
               dropdown.SetValueWithoutNotify(2);
               break;
            case Expression.Type.Operation_EQUALS:
               dropdown.SetValueWithoutNotify(3);
               break;
            case Expression.Type.Operation_OR:
               dropdown.SetValueWithoutNotify(4);
               break;
            case Expression.Type.Operation_AND:
               dropdown.SetValueWithoutNotify(5);
               break;
            case Expression.Type.Operation_NOT:
               dropdown.SetValueWithoutNotify(6);
               break;
         }
      }

      private string getOperatorLabel (Expression.Type type) {
         switch (type) {
            case Expression.Type.Operation_AND:
               return "AND";
            case Expression.Type.Operation_NOT:
               return "NOT";
            case Expression.Type.Operation_OR:
               return "OR";
            case Expression.Type.Operation_EQUALS:
               return "EQUALS";
         }
         return "ERR";
      }

      private Color getExpressionBackgroundColor (int depth) {
         return new Color(
            1 - (depth % 4) * 0.2f,
            1 - (depth % 4) * 0.2f,
            1 - (depth % 4) * 0.2f,
            1f);
      }

      public void open (Field openedBy) {
         _openedBy = openedBy;
         show();
      }

      public void close () {
         hide();
      }

      #region Private Variables

      // Current object state model we are manipulating
      private ObjectStateModel _model = null;

      // Which field has opened us
      private Field _openedBy = null;

      #endregion
   }

}
