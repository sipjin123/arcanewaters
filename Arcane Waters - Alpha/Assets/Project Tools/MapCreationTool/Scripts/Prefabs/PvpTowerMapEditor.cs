﻿using UnityEngine;
using MapCreationTool.Serialization;
using UnityEngine.UI;
using System;

namespace MapCreationTool {
   public class PvpTowerMapEditor : PvpStructureMapEditor, IPrefabDataListener, IHighlightable {
      #region Public Variables

      // The object displaying the range indicator
      public Transform rangeIndicatorObject;

      #endregion

      private void Update () {
         rangeIndicatorObject.gameObject.SetActive(transform.parent.GetComponent<Palette>() == null);
         rangeIndicatorObject.transform.localScale = new Vector3(PvpTower.ATTACK_RANGE, PvpTower.ATTACK_RANGE, 1);
      }

      // TODO: Set pvp base editor specific functionality here

      private void OnDrawGizmos () {
         Gizmos.color = new Color(1, 0, 0, 0.4f);
         Gizmos.DrawWireSphere(transform.position, PvpTower.ATTACK_RANGE);
      }

      #region Private Variables

      #endregion
   }
}