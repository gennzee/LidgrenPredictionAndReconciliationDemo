using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Wake()
    {
        int playerLayer = 3;
        Physics2D.IgnoreLayerCollision(playerLayer, playerLayer);
    }
}
