using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ChatManager : NetworkBehaviour
{
  public GameObject aircrafts;
  public GameObject messagePrefab;

  public LBSingleton lbSingleton;
  public TextMeshProUGUI lbText;
  private BulletManager bulletManager;
  public GameObject VertLayoutGroup;

  private ChatManager chat;

  void Start(){

      Debug.Log(messagePrefab);
      lbSingleton = FindObjectOfType<LBSingleton>();
  }


    [ClientRpc]
    public void RpcSendLeaderboard(string fullLeaderboard){

        lbText.text = fullLeaderboard;

        var scroll = lbText.transform.parent.GetComponentInParent<ScrollRect>();
        if (scroll != null){
            scroll.verticalNormalizedPosition = 1f;
        }
  }

    // One ChatManager for each client and one in the server
    [Command(requiresAuthority = false)]
    public void CmdSendMessage(string message, string username){
        lbSingleton.ServerBroadcastMessage($"{username}: {message}", this); // or pass username, etc.

    }

    [Server]
    public void AISendMessage(string message, string username){
        lbSingleton.ServerBroadcastMessage($"{username}: {message}", this); // or pass username, etc.
    }


    [ClientRpc]
    public void RpcAddMessage(string message){

            // Only continue if this player is using PlaneControl
          if (GetComponent<GliderControl>()?.enabled == true){
              print(" GliderControl wont receive messages");
              return; // skip if it's a glider player
          }

          int count = 0;
          messagePrefab = Resources.Load<GameObject>("messagePrefab");

          if (VertLayoutGroup == null)
          {
              Debug.LogError("VertLayoutGroups is null or empty!");
          }


          if (messagePrefab == null) {
            Debug.LogError("Message Prefab is not assigned!");
          }
          if (VertLayoutGroup == null) {
            Debug.LogError("Vertical Layout Group is not assigned!");
          }

          GameObject newMessage = Instantiate(messagePrefab, VertLayoutGroup.transform);
          TextMeshProUGUI messageText = newMessage.GetComponent<TextMeshProUGUI>();
          if (messageText != null) {
              messageText.text = message;  // Set the message text
          } else {
              Debug.LogError("The instantiated message prefab does not have a TextMeshProUGUI component.");
          }

          // If the message has a TextMeshProUGUI component, set its text
          if (messageText != null){
              messageText.text = message;  // Set the message text
          }

          for (int i = 0; i < VertLayoutGroup.transform.childCount; i++){
              Transform child = VertLayoutGroup.transform.GetChild(i);
              //Debug.Log($"  Child {i}: {child.name} - Text: {child.GetComponent<TextMeshProUGUI>()?.text}");
          }
        }


}
