using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LidgrenClient;
using Lidgren.Network;
using System.Threading;

public class Client : MonoBehaviour
{
    public static NetClient client { get; set; }

    public static float timer;
    public static float timeBetweenTicks;
    public static int currentTick;
    
    public static string LocalPlayerId { get; set; }
    public static Dictionary<string, GameObject> allPlayers { get; set; }

    private void Awake()
    {
        var config = new NetPeerConfiguration(Constant.APP_IDENTIFIER);
        config.AutoFlushSendQueue = false;

        client = new NetClient(config);
        client.Start();
        client.Connect(Constant.SERVER_IP, Constant.PORT);
    }

    private void Start()
    {
        timeBetweenTicks = 1f / Constant.SERVER_TICK_RATE;
        LocalPlayerId = "";
        allPlayers = new Dictionary<string, GameObject>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        while (timer >= timeBetweenTicks)
        {
            timer -= timeBetweenTicks;
            HandleTick();
            HandleInputMovementForAllPlayer();
            currentTick++;
        }
    }

    private void OnApplicationQuit()
    {
        SendDisconnect();
    }

    private void HandleTick()
    {
        NetIncomingMessage message;
        while ((message = client.ReadMessage()) != null)
        {
            Debug.Log("Message received from server");
            switch (message.MessageType)
            {
                case NetIncomingMessageType.Data:
                    //Get Packet Type
                    byte packetType = message.ReadByte();

                    //Create packet
                    IPacket packet;

                    switch (packetType)
                    {
                        case (byte)PacketTypes.StatePayloadPacket:
                            packet = new StatePayloadPacket();
                            packet.NetIncomingMessageToPacket(message);
                            allPlayers[((StatePayloadPacket)packet).playerId].GetComponent<Movement>().OnServerMovementState((StatePayloadPacket)packet);
                            break;
                        case (byte)PacketTypes.LocalPlayerPacket:
                            packet = new LocalPlayerPacket();
                            packet.NetIncomingMessageToPacket(message);
                            ExtractLocalPlayerInformation((LocalPlayerPacket)packet);
                            break;
                        case (byte)PacketTypes.SpawnPacket:
                            packet = new SpawnPacket();
                            packet.NetIncomingMessageToPacket(message);
                            SpawnPlayer((SpawnPacket)packet);
                            break;
                        case (byte)PacketTypes.PlayerDisconnectPacket:
                            packet = new PlayerDisconnectPacket();
                            packet.NetIncomingMessageToPacket(message);
                            DisconnectPlayer((PlayerDisconnectPacket)packet);
                            break;
                        case (byte)PacketTypes.MovementInputPacket:
                            packet = new MovementInputPacket();
                            packet.NetIncomingMessageToPacket(message);
                            UpdateMovementInputPacket((MovementInputPacket)packet);
                            break;
                        default:
                            Debug.LogWarning("Unhandled Packet Type");
                            break;
                    }
                    break;
                case NetIncomingMessageType.DebugMessage:
                case NetIncomingMessageType.ErrorMessage:
                case NetIncomingMessageType.WarningMessage:
                case NetIncomingMessageType.VerboseDebugMessage:
                    string text = message.ReadString();
                    Debug.LogWarning(text);
                    break;
                case NetIncomingMessageType.StatusChanged:
                    NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();
                    string reason = message.ReadString();
                    Debug.LogWarning(status.ToString() + ": " + reason);
                    break;
                default:
                    Debug.LogError("Unhandled message type");
                    break;
            }
            client.Recycle(message);
        }
    }

    public void HandleInputMovementForAllPlayer()
    {
        foreach (var player in allPlayers)
        {
            player.Value.GetComponent<Movement>().HandleInputMovement();
        }
    }

    public void ExtractLocalPlayerInformation(LocalPlayerPacket localPlayerPacket)
    {
        Debug.Log("Local ID is " + localPlayerPacket.id);

        LocalPlayerId = localPlayerPacket.id;
    }

    public void SpawnPlayer(SpawnPacket packet)
    {
        Debug.Log("Spawning player " + packet.playerId);

        GameObject player = (GameObject)Resources.Load("Player");
        Vector3 position = new Vector3(packet.x, packet.y);
        GameObject _player = Instantiate(player, position, new Quaternion(), GameObject.Find("Players").transform);
        if (packet.playerId == LocalPlayerId)
        {
            _player.GetComponent<Movement>().isLocal = true;
            _player.transform.name = "LocalPlayer";
        }
        else
        {
            _player.transform.name = "Player-" + packet.playerId;
        }
        allPlayers.Add(packet.playerId, _player);
    }

    public void UpdateMovementInputPacket(MovementInputPacket packet)
    {
        allPlayers[packet.playerId].GetComponent<Movement>().Velocity = new Vector2(packet.velX, packet.velY);
        allPlayers[packet.playerId].GetComponent<Movement>().Position = new Vector2(packet.posX, packet.posY);
    }

    public void SendDisconnect()
    {
        NetOutgoingMessage message = client.CreateMessage();
        IPacket playerDisconnectPacket = new PlayerDisconnectPacket() { playerId = LocalPlayerId };
        playerDisconnectPacket.PacketToNetOutGoingMessage(message);
        client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
        client.FlushSendQueue();
        client.Disconnect("Bye!");
    }

    public void DisconnectPlayer(PlayerDisconnectPacket packet)
    {
        Debug.Log("Removing player " + packet.playerId);

        DestroyImmediate(allPlayers[packet.playerId]);
        allPlayers.Remove(packet.playerId);
    }
}
