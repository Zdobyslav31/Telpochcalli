using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static class Utils
{
    public static void ChangeButtonText(GameObject button, string text) {
        TextMeshProUGUI buttonText = button.transform.GetComponentInChildren<TextMeshProUGUI>();
        buttonText.text = text;
    }
}
