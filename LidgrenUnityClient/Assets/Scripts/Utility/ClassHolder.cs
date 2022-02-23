using UnityEngine;

namespace LidgrenClient
{

    public struct ClientState
    {
        public Vector3 position;
        public float rotation;
    }
    
    public struct Inputs
    {
        public bool up;
        public bool down;
        public bool left;
        public bool right;
        public bool jump;
    }
    
}