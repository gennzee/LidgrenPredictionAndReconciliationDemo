using UnityEngine;
using LidgrenServer;
using System.Collections.Generic;
using Lidgren.Network;

public class Server : MonoBehaviour
{
    public static NetServer server;
    
    public static float timer;
    public static float timeBetweenTicks;
    public static int currentTick;
    

    public static Dictionary<string, PlayerData> playerDatas;
    public List<string> players;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
    
        NetPeerConfiguration config = new NetPeerConfiguration(Constant.APP_IDENTIFIER);
        config.MaximumConnections = Constant.MAX_CONNECTIONS;
        config.Port = Constant.PORT;

        server = new NetServer(config);
        server.Start();
        Debug.Log("Listening for clients...");
    }

    private void Start()
    {
        timeBetweenTicks = 1f / Constant.SERVER_TICK_RATE;
        playerDatas = new Dictionary<string, PlayerData>();
        players = new List<string>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        while (timer >= timeBetweenTicks)
        {
            timer -= timeBetweenTicks;
            HandleTick();
            HandleTickForAllPlayer();
            currentTick++;
        }
    }

    private void HandleTick()
    {
        NetIncomingMessage message;
        while ((message = server.ReadMessage()) != null)
        {
            Debug.Log("Message received.");
            // Get a list of all users
            List<NetConnection> all = server.Connections;
            switch (message.MessageType)
            {
                case NetIncomingMessageType.Data:
                    // Get packet type
                    byte packetType = message.ReadByte();
                    // Create Packet
                    IPacket packet;
                    switch (packetType)
                    {
                        case (byte)PacketTypes.InputPayloadPacket:
                            packet = new InputPayloadPacket();
                            packet.NetIncomingMessageToPacket(message);
                            playerDatas[((InputPayloadPacket)packet).playerId].obj.GetComponent<PlayerMovement>().OnClientInput((InputPayloadPacket)packet);
                            break;
                        case (byte)PacketTypes.PlayerDisconnectPacket:
                            packet = new PlayerDisconnectPacket();
                            packet.NetIncomingMessageToPacket(message);
                            SendPlayerDisconnectPacket(all, (PlayerDisconnectPacket)packet);
                            break;
                        case (byte)PacketTypes.MovementInputPacket:
                            packet = new MovementInputPacket();
                            packet.NetIncomingMessageToPacket(message);
                            SendMovementInputPacket(all, (MovementInputPacket)packet);
                            break;
                        default:
                            Debug.LogError("Unhandled data / packet type: " + packetType);
                            break;
                    }
                    break;
                case NetIncomingMessageType.StatusChanged:
                    NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();
                    string reason = message.ReadString();
                    Debug.LogWarning(NetUtility.ToHexString(message.SenderConnection.RemoteUniqueIdentifier) + " " + status + ": " + reason);
                    if (status == NetConnectionStatus.Connected)
                    {
                        var playerId = NetUtility.ToHexString(message.SenderConnection.RemoteUniqueIdentifier);
                        // Send player their ID
                        SendLocalPlayerPacket(message.SenderConnection, playerId);
                        // Send User Spawn Message
                        SpawnPlayers(all, message.SenderConnection, playerId);
                    }
                    break;
                case NetIncomingMessageType.DebugMessage:
                case NetIncomingMessageType.ErrorMessage:
                case NetIncomingMessageType.WarningMessage:
                case NetIncomingMessageType.VerboseDebugMessage:
                    string text = message.ReadString();
                    Debug.LogWarning(text);
                    break;
                default:
                    Debug.LogError("Unhandled type: " + message.MessageType + " " + message.LengthBytes + " bytes " + message.DeliveryMethod + "|" + message.SequenceChannel);
                    break;
            }
            server.Recycle(message);
        }
    }

    public void HandleTickForAllPlayer()
    {
        foreach (var playerObj in playerDatas)
        {
            playerObj.Value.obj.GetComponent<PlayerMovement>().HandleTick();
        }
    }

    public void SendLocalPlayerPacket(NetConnection local, string playerId)
    {
        Debug.LogWarning("Sending player their user Id: " + playerId);

        NetOutgoingMessage outgoingMessage = server.CreateMessage();
        new LocalPlayerPacket() { id = playerId }.PacketToNetOutGoingMessage(outgoingMessage);
        server.SendMessage(outgoingMessage, local, NetDeliveryMethod.ReliableOrdered, 0);
    }

    public void SpawnPlayers(List<NetConnection> all, NetConnection local, string playerId)
    {
        // Instantite player object and add to dictionary and spawn new player on server
        GameObject playerObj = (GameObject) Instantiate(Resources.Load("Player"), Vector3.zero, new Quaternion(), GameObject.Find("Players").transform);
        playerObj.name = playerId;
        playerDatas[playerId] = new PlayerData() { horizontal = 0, vertical = 0, obj = playerObj };
        // Add player to list to show up on inspector
        players.Add(playerId);
        // Spawn all the clients on the local player
        all.ForEach(p =>
        {
            string _playerId = NetUtility.ToHexString(p.RemoteUniqueIdentifier);
            if (playerId != _playerId)
                SendSpawnPacketToLocal(local, _playerId, playerDatas[_playerId]);
        });
        // Spawn the local player on all clients
        SendSpawnPacketToAll(all, playerId);
    }

    public void SendSpawnPacketToLocal(NetConnection local, string playerId, PlayerData playerData)
    {
        Debug.LogWarning("Sending user spawn message for player " + playerId);

        playerDatas[playerId].posX = playerData.posX;
        playerDatas[playerId].posY = playerData.posY;
        
        NetOutgoingMessage outgoingMessage = server.CreateMessage();
        new SpawnPacket() { playerId = playerId, x = playerData.posX, y = playerData.posY }.PacketToNetOutGoingMessage(outgoingMessage);
        server.SendMessage(outgoingMessage, local, NetDeliveryMethod.ReliableOrdered, 0);
    }

    public void SendSpawnPacketToAll(List<NetConnection> all, string playerId)
    {
        Debug.LogWarning("Sending user spawn message for player " + playerId);

        playerDatas[playerId].posX = 0;
        playerDatas[playerId].posY = 0;
        
        NetOutgoingMessage outgoingMessage = server.CreateMessage();
        new SpawnPacket() { playerId = playerId, x = 0, y = 0 }.PacketToNetOutGoingMessage(outgoingMessage);
        server.SendMessage(outgoingMessage, all, NetDeliveryMethod.ReliableOrdered, 0);
    }

    public void SendPlayerDisconnectPacket(List<NetConnection> all, PlayerDisconnectPacket packet)
    {
        Debug.LogWarning("Disconnecting for " + packet.playerId);
        DestroyImmediate(playerDatas[packet.playerId].obj);
        playerDatas.Remove(packet.playerId);
        players.Remove(packet.playerId);

        NetOutgoingMessage outgoingMessage = server.CreateMessage();
        packet.PacketToNetOutGoingMessage(outgoingMessage);
        server.SendMessage(outgoingMessage, all, NetDeliveryMethod.ReliableOrdered, 0);
    }

    public void SendMovementInputPacket(List<NetConnection> all, MovementInputPacket packet)
    {
        Debug.LogWarning("Sending movement input for player " + packet.playerId);

        playerDatas[packet.playerId].posX = packet.posX;
        playerDatas[packet.playerId].posY = packet.posY;
        
        NetOutgoingMessage outgoingMessage = server.CreateMessage();
        new MovementInputPacket() { playerId = packet.playerId, posX = packet.posX, posY = packet.posY }.PacketToNetOutGoingMessage(outgoingMessage);
        server.SendMessage(outgoingMessage, all, NetDeliveryMethod.ReliableOrdered, 0);
    }
}
