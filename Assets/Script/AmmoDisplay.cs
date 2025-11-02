using UnityEngine;
using TMPro; // <-- 1. IMPORT TEXTMESHPRO

// You should rename this file to "AmmoDisplay.cs"
public class AmmoDisplay : MonoBehaviour
{
    // --- 2. CHANGED 'Text' to 'TMP_Text' ---
    private TMP_Text ammoText;

    // Drag your player's gun object here in the Inspector
    public WeaponController gunController;

    void Start()
    {
        // --- 3. CHANGED 'GetComponent<Text>()' to 'GetComponent<TMP_Text>()' ---
        ammoText = GetComponent<TMP_Text>();

        if (ammoText == null)
        {
            Debug.LogError("ERROR: Could not find TextMeshPro (TMP_Text) component on this GameObject!", this);
            this.enabled = false;
            return;
        }

        if (gunController == null)
        {
            Debug.LogError("ERROR: GunController not assigned in the AmmoDisplay script!", this);
            // Disable this script if the gun isn't assigned to prevent errors.
            this.enabled = false;
        }
    }

    void Update()
    {
        // If the gunController is assigned, update the text every frame.
        if (gunController != null)
        {
            // This line works for both Text and TMP_Text
            ammoText.text = gunController.currentAmmo + " / " + gunController.maxAmmo;
        }
    }
}
