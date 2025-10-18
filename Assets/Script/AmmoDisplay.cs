using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// You should rename this file to "AmmoDisplay.cs"
public class AmmoDisplay : MonoBehaviour
{
    private Text ammoText;
    // Drag your player's gun object here in the Inspector
    public GunController gunController;

    void Start()
    {
        // Get the Text component from the GameObject this script is attached to.
        ammoText = GetComponent<Text>();

        if (gunController == null)
        {
            Debug.LogError("ERROR: GunController not assigned in the AmmoDisplay script!");
            // Disable this script if the gun isn't assigned to prevent errors.
            this.enabled = false;
        }
    }

    void Update()
    {
        // If the gunController is assigned, update the text every frame.
        if (gunController != null)
        {
            ammoText.text = gunController.currentAmmo + " / " + gunController.maxAmmo;
        }
    }
}
