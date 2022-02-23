using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        int playerLayer = 3;
        Physics2D.IgnoreLayerCollision(playerLayer, playerLayer);
    }
}
