using Mirror;
using UnityEngine;

public class MyNet : NetworkManager
{
    public Transform[] redSpawnPoints;
    public Transform[] greenSpawnPoints;
    public MatchManager matchManager;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        TeamType team = ChooseTeamForNewPlayer();

        Transform spawnPoint = GetSpawnPoint(team);

        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        Player playerScript = player.GetComponent<Player>();
        playerScript.SetTeam(team);
        playerScript.Init(this, matchManager);

        NetworkServer.AddPlayerForConnection(conn, player);

        matchManager.RegisterPlayer(playerScript);
    }

    TeamType ChooseTeamForNewPlayer()
    {
        int redCount = 0;
        int greenCount = 0;

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
            {
                Player player = conn.identity.GetComponent<Player>();
                if (player != null)
                {
                    if (player.GetTeam() == TeamType.Red)
                    {
                        redCount++;
                    }
                    else if (player.GetTeam() == TeamType.Green)
                    {
                        greenCount++;
                    }
                }
            }
        }

        return redCount <= greenCount ? TeamType.Red : TeamType.Green;
    }

    public Transform GetSpawnPoint(TeamType team)
    {
        Transform[] spawnPoints = team == TeamType.Red ? redSpawnPoints : greenSpawnPoints;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return null;
        }

        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
