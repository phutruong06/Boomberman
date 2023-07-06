using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class LobbyManager : MonoBehaviour
{
    [Header("Mani Manu")]
    [SerializeField] private GameObject mainmenuPanel;
    [SerializeField] private Button getLobbiesListBtn;
    [SerializeField] private GameObject lobbyInfoPrefab;
    [SerializeField] private GameObject lobbyInfoContent;
    [SerializeField] private TMP_InputField playerNameIF;

   
    [Space(10)]
    [Header("Create Room Panel")]
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private TMP_InputField roomNameIF;
    [SerializeField] private TMP_InputField maxPlayerIF;
    [SerializeField] private Button createRoomBtn;
    [SerializeField] private Toggle isPrivateToggle;


    [Space(10)]
    [Header("Room Panel")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TextMeshProUGUI roomName;
    [SerializeField] private TextMeshProUGUI roomCode;
    [SerializeField] private GameObject playerInfoContent;
    [SerializeField] private GameObject playerInfoPrefab;
    [SerializeField] private Button leaveRoomButton;

    [Space(10)]
    [Header("Join Room With Code")]
    [SerializeField] private GameObject joinRoomPanel;
    [SerializeField] private TMP_InputField roomCodeIF;
    [SerializeField] private Button joinRoomBtn;


    private Lobby currentLobby;

    private string playerId;

    // Start is called before the first frame update
    async void Start()
    {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () =>
         {
             playerId = AuthenticationService.Instance.PlayerId;
             Debug.Log("Signed In :" + playerId);
         };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        createRoomBtn.onClick.AddListener(CreateLobby);
        getLobbiesListBtn.onClick.AddListener(ListPublicLobby);

        playerNameIF.onValueChanged.AddListener(delegate
        {
            PlayerPrefs.SetString("Name", playerNameIF.text);
        });
        playerNameIF.text = PlayerPrefs.GetString("Name");

        leaveRoomButton.onClick.AddListener(LeaveRoom);
    }

    // Update is    called once per frame
    void Update()
    {
        HandleLobbyHeartBeat();
        HandleRoomUpdate();
    }

    private async void CreateLobby()
    {
        try
        {
            string lobbyName = roomNameIF.text;
            int.TryParse(maxPlayerIF.text, out int maxPlayers);
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = isPrivateToggle.isOn,   
                Player = GetPlayer()
            };
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            EnterRoom();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

   
    private void EnterRoom()
    {
        mainmenuPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(true);
        roomName.text = currentLobby.Name;
        roomCode.text = currentLobby.LobbyCode;

        VisualizeRoomDetails();
    }

    private float roomUpdateTimer = 2f;
   private async void HandleRoomUpdate()
    {
        if(currentLobby != null)
        {
            roomUpdateTimer -= Time.deltaTime;
            if (roomUpdateTimer <= 0)
                roomUpdateTimer = 2f;
                try
                {
                if(IsinLobby())
                    {
                    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                    VisualizeRoomDetails(); }
                }
                catch (LobbyServiceException e)
                {
                    Debug.Log(e);
                }
        }
    }

    private bool IsinLobby()
    {
        foreach (Player _player in currentLobby.Players)
        {
            if(_player.Id == playerId)
            {
                return true;
            }
        }
        currentLobby = null;
        return false;
    }

    private void VisualizeRoomDetails()
    {
        for (int i=0; i< playerInfoContent.transform.childCount; i++)
        {
            Destroy(playerInfoContent.transform.GetChild(i).gameObject);
        }
        if(IsinLobby())
        {
            foreach (Player player in currentLobby.Players)
            {
                GameObject newplayerInfo = Instantiate(playerInfoPrefab, playerInfoContent.transform);
                newplayerInfo.GetComponentInChildren<TextMeshProUGUI>().text = player.Data["PlayerName"].Value;
                if (IsHost())
                {
                    Button kickBtn = newplayerInfo.GetComponentInChildren<Button>(true);
                    kickBtn.onClick.AddListener(() => KickPlayer(player.Id));
                    kickBtn.gameObject.SetActive(true);
                }
               
            }
        }
        else
        {
            ExitRoom();
        }
        
    }

    private async void ListPublicLobby()
    {
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
            VisualizeLobbyList(response.Results);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }


    }

    private void VisualizeLobbyList(List<Lobby> _publicLoobbies)
    {
        //We need to clear provious info
        for (int i= 0; i < lobbyInfoContent.transform.childCount; i++)
        {
            Destroy(lobbyInfoContent.transform.GetChild(i).gameObject);
        }
        foreach (Lobby _lobby in _publicLoobbies)
        {
           GameObject newLobbyInfo= Instantiate(lobbyInfoPrefab,lobbyInfoContent.transform);
            var lobbyDetailsTexts = newLobbyInfo.GetComponentsInChildren<TextMeshProUGUI>();
            lobbyDetailsTexts[0].text = _lobby.Name;
            lobbyDetailsTexts[1].text = (_lobby.MaxPlayers - _lobby.AvailableSlots).ToString() + "/" + _lobby.MaxPlayers.ToString();
            newLobbyInfo.GetComponentInChildren<Button>().onClick.AddListener(()=> JoinLobby (_lobby.Id));
        }

    }

    private async void JoinLobby(string _lobbyId)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
            {
                Player = GetPlayer()
            };
           currentLobby=  await LobbyService.Instance.JoinLobbyByIdAsync(
               _lobbyId,
               options);
            EnterRoom();
            Debug.Log("Players In Room :" + currentLobby.Players.Count);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
       
    }


    private float heartbeattimer = 15f;
    private async void HandleLobbyHeartBeat()
    {
        if (currentLobby != null)
        {
            heartbeattimer -= Time.deltaTime;
            if (heartbeattimer <= 0)
            {
                heartbeattimer = 15f;
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }
    }

    private bool IsHost()
    {
        if(currentLobby!= null && currentLobby.HostId == playerId)
        {
            return true;
        }
        return false;
    }

    private Player GetPlayer()
    {
        string playerName = PlayerPrefs.GetString("Name");
        if (playerName == null || playerName == "")
            playerName = playerId;
        Player player = new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                {"PlayerName",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,playerName) }
            }
        };

        return player;
    }

    private async void LeaveRoom()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
            ExitRoom();
        }catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void KickPlayer(string _playerId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, _playerId);
        }catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private void ExitRoom()
    {
        mainmenuPanel.SetActive(true);
        createRoomPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
        roomPanel.SetActive(false);
        ListPublicLobby();
    }

}

