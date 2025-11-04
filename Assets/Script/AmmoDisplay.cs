using UnityEngine;
using TMPro; // <-- 1. Make sure this line is at the top

public class AmmoDisplay : MonoBehaviour
{
    // 2. This is the public variable.
    // It will show up as a slot in the Inspector.
    public TextMeshProUGUI ammoText;

    void Start()
    {
        // 3. We DELETED the line that said "GetComponent"
        // That line caused the error.
    }

    // Your Update() method or other functions that use
    // 'ammoText' will now work.
    //
    // For example, if you have a function to update the ammo:
    public void UpdateAmmoText(int currentAmmo)
    {
        if (ammoText != null)
        {
            ammoText.text = currentAmmo.ToString();
        }
    }
}