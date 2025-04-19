using UnityEngine;
using UnityEngine.UI;

public class RegisterUIScript : MonoBehaviour
{
    public InputField nameInputField;
    public GameObject panel1;
    public GameObject panel2;
    public Text displayText;

    private string playerName;

    public void OnSaveButtonClicked()
    {
        playerName = nameInputField.text;
        panel1.SetActive(false);
        panel2.SetActive(true);
        displayText.text = "Welcome, " + playerName + "!";
    }
}
