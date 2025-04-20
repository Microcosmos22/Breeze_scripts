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

  private float updatePlayersTimer = 2f, updatePlayersTime;
  private int nPlayers;

  void Start(){

      print("msg prefab");
      Debug.Log(messagePrefab);
  }

  void Update(){
      updatePlayersTime += Time.deltaTime;
      if (updatePlayersTime > updatePlayersTimer){
          updatePlayersTime = 0f;
          SetupAircrafts();
      }

      GetKills();


  }

  void SetupAircrafts(){
    chatText = new List<TextMeshProUGUI>();
    lbText = new List<TextMeshProUGUI>();
    bulletManagers = new List<BulletManager>();
    VertLayoutGroups = new List<GameObject>();
    nPlayers = 0;

    foreach (var netId in NetworkServer.spawned){
        var obj = netId.Value.gameObject;
        var plane = obj.GetComponent<PlaneControl>();

        if (plane != null){
            if (plane.networkIdentity.isOwned){ // found a Player PlaneControl
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
            }
      }


      }
      print($"ChatManager found {nPlayers} players");
  }

  void GetKills(){
      for (int i = 0; i < nPlayers; i++){
          kills[i] = bulletManagers[i].kills;
          deaths[i] = bulletManagers[i].deaths;
      }
  }
  void MakeLeaderboard(){
      List<int> score = new List<int>();       // Score list (kills - deaths)
      List<string> lbStrings = new List<string>();  // Unsorted leaderboard lines

      // Build scores and leaderboard strings
      for (int i = 0; i < nPlayers; i++){
          score.Add(kills[i] - deaths[i]);
          lbStrings.Add($" S: {kills[i] - deaths[i]} | K: {kills[i]} | D: {deaths[i]} | {Username[i]}");
      }

      // Get sorted indices
      List<int> sortedI = score
          .Select((s, index) => new { s, index })
          .OrderByDescending(pair => pair.s)
          .Select(pair => pair.index)
          .ToList();

      // Create final leaderboard string in sorted order
      string fullLeaderboard = string.Join("\n", sortedI.Select(i => lbStrings[i]));

      // Update the UI
      foreach (var lb in lbText){
          lb.text = fullLeaderboard;

          var scroll = lb.transform.parent.GetComponentInParent<ScrollRect>();
          if (scroll != null){
              scroll.verticalNormalizedPosition = 1f;
          }
      }
  }

    [Command(requiresAuthority = false)]
    public void CmdSendMessage(string message, string username)
    {
      RpcAddMessage($"{username}: {message}");
    }

    [ClientRpc]
    public void RpcAddMessage(string message)
    {
      // Only continue if this player is using PlaneControl
    if (GetComponent<GliderControl>()?.enabled == true)
    {
        return; // skip if it's a glider player
    }


      int count = 0;
      //print($"Writing RpcAddmessage: {message}");

      messagePrefab = Resources.Load<GameObject>("messagePrefab");

      foreach (var vert in VertLayoutGroups)
      {
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
