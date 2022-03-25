using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderboardPlayer : MonoBehaviour
{
    public TMP_Text playerNameText, killsText, deathText;

    public void SetDetails(string name,int kills, int deaths)
    {
        playerNameText.text = name;
        killsText.text = kills.ToString();
        deathText.text = deaths.ToString();
    }

}
