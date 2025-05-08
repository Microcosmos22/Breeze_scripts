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

  private List<TextMeshProUGUI> chatText = new List<TextMeshProUGUI>();
  private List<TextMeshProUGUI> lbText = new List<TextMeshProUGUI>();
  private List<BulletManager> bulletManagers = new List<BulletManager>();
  private List<GameObject> VertLayoutGroups = new List<GameObject>();
  private List<int> kills = new List<int>();
  private List<int> score = new List<int>();
  private List<int> deaths = new List<int>();
  private List<string> Username = new List<string>();
  private List<int> sortedI = new List<int>();
  private List<string> lbStrings = new List<string>();

  private float updatePlayersTimer = 1f, updatePlayersTime;
  private int nPlayers = 0, nAIs = 0;
  private ChatManager chat;

  void Start(){

      Debug.Log(messagePrefab);
  }

  void Update(){
      updatePlayersTime += Time.deltaTime;
      if (updatePlayersTime > updatePlayersTimer){
          updatePlayersTime = 0f;
          if (isServer){
              SetupAircrafts();
              GetKills();
              UpdateLeaderboard(Username.ToArray(), kills.ToArray(), deaths.ToArray());}
      }

  }

  [Server]
  void SetupAircrafts(){
    // Update list of player ChatText, LBText etc. references regularly.

    chatText = new List<TextMeshProUGUI>();
    lbText = new List<TextMeshProUGUI>();
    bulletManagers = new List<BulletManager>();
    VertLayoutGroups = new List<GameObject>();
    kills = new List<int>();
    nPlayers = 0;
    nAIs = 0;

    foreach (var netId in NetworkServer.spawned){
        var obj = netId.Value.gameObject;
        var plane = obj.GetComponent<PlaneControl>();

        if (plane != null){
            if (!plane.isAI){ // found a Player
                bulletManagers.Add(plane.bulletManager);
                chatText.Add(plane.bulletManager.chatText);
                lbText.Add(plane.bulletManager.lbText);
                Username.Add(plane.Username);
                VertLayoutGroups.Add(plane.bulletManager.VertLayoutGroup);
                score.Add(0);
                sortedI.Add(0);
                lbStrings.Add("");

                nPlayers += 1;
                kills.Add(0);
                deaths.Add(0);
            }else{
                bulletManagers.Add(plane.bulletManager);
                Username.Add(plane.Username);
                nAIs += 1;
                kills.Add(0);
                deaths.Add(0);
            }
      }
      }
      print($"ChatManager found {nPlayers} players and {nAIs} AIs");
  }

  void GetKills(){

      for (int i = 0; i < (nPlayers+nAIs); i++){
          if (i < kills.Count && i < bulletManagers.Count){
              kills[i] = bulletManagers[i].kills;
              deaths[i] = bulletManagers[i].deaths;}
      }
  }

  void UpdateLeaderboard(string[] usernames, int[] kills, int[] deaths){
      List<int> score = new List<int>();       // Score list (kills - deaths)
      List<string> lbStrings = new List<string>();  // Unsorted leaderboard lines

      print("Send leaderboard");

      // Build scores and leaderboard strings
      for (int i = 0; i < (nPlayers+nAIs); i++){
          score.Add(kills[i] - deaths[i]);
          lbStrings.Add($" {kills[i]} | {deaths[i]}| {kills[i] - deaths[i]} | {Username[i]}");
          //print($" {kills[i]} | {deaths[i]}| {kills[i] - deaths[i]} | {Username[i]}");

      }

      // Get sorted indices
      List<int> sortedI = score
          .Select((s, index) => new { s, index })
          .OrderByDescending(pair => pair.s)
          .Select(pair => pair.index)
          .ToList();


        string firstFiveLB = string.Join("\n", sortedI.Take(5).Select(i => lbStrings[i]));
        RpcSendLeaderboard(firstFiveLB);
    }


      [ClientRpc]
      void RpcSendLeaderboard(string fullLeaderboard){
      // Update the UI
      foreach (var lb in lbText){
          lb.text = fullLeaderboard;

          var scroll = lb.transform.parent.GetComponentInParent<ScrollRect>();
          if (scroll != null){
              scroll.verticalNormalizedPosition = 1f;
          }
      }
  }

    // One ChatManager for each client and one in the server
    [Command(requiresAuthority = false)]
    public void CmdSendMessage(string message, string username){
        ChatManager chat = FindObjectOfType<ChatManager>();
        if (chat != null){
            chat.ServerBroadcastMessage($"{username}: {message}", this); // or pass username, etc.
        }
    }

    [Server]
    public void ServerBroadcastMessage(string message, NetworkBehaviour sender)
    {
        // Do server logic here
        RpcAddMessage(message);
    }


    [ClientRpc]
    public void RpcAddMessage(string message)
    {
      // Only continue if this player is using PlaneControl
    if (GetComponent<GliderControl>()?.enabled == true){
        return; // skip if it's a glider player
    }

      int count = 0;
      messagePrefab = Resources.Load<GameObject>("messagePrefab");

      foreach (var vert in VertLayoutGroups){
          if (messagePrefab == null) {
            Debug.LogError("Message Prefab is not assigned!");
          }
          if (vert == null) {
            Debug.LogError("Vertical Layout Group is not assigned!");
          }

          GameObject newMessage = Instantiate(messagePrefab, vert.transform);
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

      }
    }


}
