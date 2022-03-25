using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{

    public static PlayerSpawner instance;
    private void Awake()
    {
        instance = this;
    }

    public GameObject playerPrefab;
    private GameObject player;

    public GameObject deathEffect;

    public float respawnTime = 5f;

    // Start is called before the first frame update
    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        //all player will spawn at spawn point 
        Transform spawnPoint = SpawnManager.instance.GetSpawnPoint();

        // instantiate all player over the network including the player who is playing in this system
        player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }

    //when any player hit the another player than this function will respawn that player again a spawnlocation or spawnpoint
    //this below function is called in PlayerControl.cs script
    public void Die(string damager)
    {
        //deleted because it is now implamented in DieCo() function
       // PhotonNetwork.Instantiate(deathEffect.name, player.transform.position, Quaternion.identity);

        UIController.instance.deathScreenText.text = "You were killed by " + damager;

        //PhotonNetwork.Destroy(player);
        // here insteed of destroying player it will show message, it is commented out before adding implemention of above message functionality
        // it will implament in below DieCo() function
        //SpawnPlayer();

        MatchManager.instance.UpdateStatSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

        if (player != null)
        {
            StartCoroutine(DieCo());
        }
    }

    public IEnumerator DieCo()
    {
        PhotonNetwork.Instantiate(deathEffect.name, player.transform.position, Quaternion.identity);

        PhotonNetwork.Destroy(player);
        player = null;
        UIController.instance.deathScreen.SetActive(true);

        yield return new WaitForSeconds(respawnTime);

        UIController.instance.deathScreen.SetActive(false);

        if (MatchManager.instance.state == MatchManager.GameState.Playing && player == null)
        {
            SpawnPlayer();
        }
    }
}
