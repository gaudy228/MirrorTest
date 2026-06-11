using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MatchManager : NetworkBehaviour
{
    [SerializeField] int maxScore = 3;
    [SerializeField] float roundEndDelay = 1f;

    [SerializeField] TextMeshProUGUI redScoreText;
    [SerializeField] TextMeshProUGUI greenScoreText;
    [SerializeField] TextMeshProUGUI roundEndMessageText;
    [SerializeField] TextMeshProUGUI matchWinnerText;

    [SyncVar(hook = nameof(OnRedScoreChanged))]
    int redScore = 0;

    [SyncVar(hook = nameof(OnGreenScoreChanged))]
    int greenScore = 0;

    [SyncVar(hook = nameof(OnCurrentRoundChanged))]
    int currentRound = 0;

    private List<Player> redTeamPlayers = new List<Player>();
    private List<Player> greenTeamPlayers = new List<Player>();

    private bool isMatchActive = false;
    private bool isRoundActive = false;
    private bool isWaitingForNextRound = false;

    public static MatchManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        UpdateScoreUI();
    }

    void Update()
    {
        if (isServer && !isMatchActive && Input.GetKeyDown(KeyCode.R))
        {
            StartMatch();
        }

        if (isServer && isMatchActive && isRoundActive && !isWaitingForNextRound)
        {
            CheckRoundEnd();
        }
    }

    void OnRedScoreChanged(int oldValue, int newValue)
    {
        if (redScoreText != null)
        {
            redScoreText.text = newValue.ToString();
        }
    }

    void OnGreenScoreChanged(int oldValue, int newValue)
    {
        if (greenScoreText != null)
        {
            greenScoreText.text = newValue.ToString();
        }
    }

    void OnCurrentRoundChanged(int oldValue, int newValue)
    {
        if (roundEndMessageText != null)
        {
            roundEndMessageText.text = $"Current round: {newValue}";
        }
    }

    [Server]
    public void StartMatch()
    {
        if (!isServer) return;

        isMatchActive = true;
        currentRound = 0;
        redScore = 0;
        greenScore = 0;
        RpcShowMatchEnd("");

        CheckStartGame();
    }


    void CheckStartGame()
    {
        if (!isServer) return;
        if (!isMatchActive) return;

        CleanupNullPlayers();


        if (redTeamPlayers.Count > 0 && greenTeamPlayers.Count > 0)
        {
            StartNewRound();
        }
        else
        {
            Invoke(nameof(CheckStartGame), 1f);
        }
    }

    public void CheckRoundEnd()
    {
        if (!isServer) return;
        if (!isMatchActive || !isRoundActive || isWaitingForNextRound) return;

        int aliveRed = GetAlivePlayersCount(redTeamPlayers);
        int aliveGreen = GetAlivePlayersCount(greenTeamPlayers);

        if (aliveRed == 0 && aliveGreen > 0)
        {
            EndRound();
            greenScore++;
        }
        else if (aliveRed > 0 && aliveGreen == 0)
        {
            EndRound();
            redScore++;
        }
        else if (aliveRed == 0 && aliveGreen == 0)
        {
            EndRound();
        }
    }

    int GetAlivePlayersCount(List<Player> teamPlayers)
    {
        int alive = 0;
        foreach (Player player in teamPlayers)
        {
            if (player != null && player.gameObject != null && player.gameObject.activeSelf && player.GetHealth() > 0)
            {
                alive++;
            }
        }
        return alive;
    }

    void StartNewRound()
    {
        if (!isServer)
        {
            return;
        }
        if (!isMatchActive)
        {
            return;
        }

        currentRound++;
        isRoundActive = true;
        isWaitingForNextRound = false;

        RespawnAllPlayers();
    }

    void EndRound()
    {
        if (!isServer)
        {
            return;
        }

        isRoundActive = false;
        isWaitingForNextRound = true;

        if (redScore >= maxScore)
        {
            EndMatch(TeamType.Red);
        }
        else if (greenScore >= maxScore)
        {
            EndMatch(TeamType.Green);
        }
        else
        {
            Invoke(nameof(StartNewRound), roundEndDelay);
        }
    }

    void UpdateScoreUI()
    {
        if (redScoreText != null)
        {
            redScoreText.text = redScore.ToString();
        }

        if (greenScoreText != null)
        {
            greenScoreText.text = greenScore.ToString();
        }
    }

    void EndMatch(TeamType winner)
    {
        if (!isServer)
        {
            return;
        }

        isMatchActive = false;
        isRoundActive = false;

        string winnerName = winner == TeamType.Red ? "RED TEAM" : "GREEN TEAM";


        RpcShowMatchEnd(winnerName);
    }

    void RespawnAllPlayers()
    {
        foreach (Player player in redTeamPlayers)
        {
            if (player != null)
            {
                player.Respawn();
            }
        }

        foreach (Player player in greenTeamPlayers)
        {
            if (player != null)
            {
                player.Respawn();
            }
        }
    }

    void CleanupNullPlayers()
    {
        redTeamPlayers.RemoveAll(p => p == null);
        greenTeamPlayers.RemoveAll(p => p == null);
    }

    [Server]
    public void RegisterPlayer(Player player)
    {
        if (player == null) return;

        if (player.GetTeam() == TeamType.Red && !redTeamPlayers.Contains(player))
        {
            redTeamPlayers.Add(player);
        }
        else if (player.GetTeam() == TeamType.Green && !greenTeamPlayers.Contains(player))
        {
            greenTeamPlayers.Add(player);
        }

        if (isServer && isMatchActive && !isRoundActive && !isWaitingForNextRound)
        {
            CheckStartGame();
        }
    }

    [ClientRpc]
    void RpcShowMatchEnd(string winnerName)
    {
        matchWinnerText.text = $"Win: {winnerName}";
    }
}
