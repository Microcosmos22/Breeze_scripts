using UnityEngine;
using UnityEngine.UI;

public class UsernamePrompt : MonoBehaviour
{
    public InputField usernameInput;
    public Button submitButton;
    public GameObject panel;

    private void Start()
    {
        panel.SetActive(true); // Show the panel when the game starts
        submitButton.onClick.AddListener(OnSubmit);
    }

    private void OnSubmit()
    {
        string username = usernameInput.text;
        Debug.Log("Username entered: " + username);

        // Add logic here to handle the entered username
        // For example, save it to a game manager or player prefs

        panel.SetActive(false); // Hide the panel after submitting
    }
}
