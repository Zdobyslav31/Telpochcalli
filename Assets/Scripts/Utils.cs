using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CodeMonkey.Utils;

public static class Utils {
    public static void ChangeButtonText(GameObject button, string text) {
        TextMeshProUGUI buttonText = button.transform.GetComponentInChildren<TextMeshProUGUI>();
        buttonText.text = text;
    }

    public static void CreateWorldTextPopup(string text, Vector3 localPosition, Color color) {
        UtilsClass.CreateWorldTextPopup(null, text, localPosition, 16, color, localPosition + new Vector3(0, 20), 1f);
    }
}
