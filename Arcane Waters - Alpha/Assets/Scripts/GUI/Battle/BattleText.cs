using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class BattleText : MonoBehaviour {
    #region Public Variables

    // How long we last until we destroy ourself
    public static float LIFETIME = 1.5f;

    // How long it takes us to fully appear
    public static float SCALE_IN_DURATION = .25f;

    #endregion

    void Start() {
        _creationTime = Time.time;
        _text = GetComponent<Text>();
    }

    void Update() {
        float timeSinceCreation = Time.time - _creationTime;

        // Scale our size from 0 to 1 when the text first appears
        if (timeSinceCreation < SCALE_IN_DURATION) {
            transform.localScale = Vector3.one * (timeSinceCreation / SCALE_IN_DURATION);
        } else {
            transform.localScale = Vector3.one;
        }

        // Fade our text out over time
        float fadePercent = 1f - (timeSinceCreation / LIFETIME);
        Util.setAlpha(_text, fadePercent);

        // If enough time has passed, destroy ourself
        if (timeSinceCreation > LIFETIME) {
            Destroy(this.gameObject);
        }
    }

    public void customizeTextForStance(BattlerBehaviour.Stance stance) {
        _text = GetComponent<Text>();

        // Start out scaled completely down
        transform.localScale = Vector3.zero;

        switch (stance) {
            case BattlerBehaviour.Stance.Attack:
                _text.color = Color.red;
                _text.text = "Offense\nStance";
                break;
            case BattlerBehaviour.Stance.Defense:
                _text.color = Color.green;
                _text.text = "Defense\nStance";
                break;
            default:
                _text.color = Color.white;
                _text.text = "Balanced\nStance";
                break;
        }
    }

    public void customizeTextForBlock() {
        _text = GetComponent<Text>();

        // Start out scaled completely down
        transform.localScale = Vector3.zero;

        // Customize text/color
        _text.color = Color.yellow;
        _text.text = "Block!";
    }

    public void customizeTextForCritical() {
        _text = GetComponent<Text>();

        // Start out scaled completely down
        transform.localScale = Vector3.zero;

        // Customize text/color
        _text.color = Color.red;
        _text.text = "Crit!";
        _text.fontSize = (int)(_text.fontSize * 1.25f);
    }

    #region Private Variables

    // Our Text object
    protected Text _text;

    // The time at which we were created
    protected float _creationTime;

    #endregion
}
