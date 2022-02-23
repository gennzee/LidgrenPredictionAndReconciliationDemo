using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using UnityEngine;

namespace LidgrenClient
{
    public enum PacketTypes
    {
        LocalPlayerPacket,
        SpawnPacket,
        InputPayloadPacket,
        StatePayloadPacket,
        PlayerDisconnectPacket,
        MovementInputPacket,
        ClientInputMessagePacket,
        ClientStatePacket
    }

    public interface IPacket
    {
        public void PacketToNetOutGoingMessage(NetOutgoingMessage message);
        public void NetIncomingMessageToPacket(NetIncomingMessage message);
    }
    public abstract class Packet : IPacket
    {
        public abstract void NetIncomingMessageToPacket(NetIncomingMessage message);
        public abstract void PacketToNetOutGoingMessage(NetOutgoingMessage message);
    }

    public class ClientStatePacket : Packet
    {
        public string playerId;
        public int tick;
        public Vector2 velocity;
        public Vector2 position;
        public float rotation;

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            playerId = message.ReadString();
            tick = message.ReadInt32();
            float velX = message.ReadFloat();
            float velY = message.ReadFloat();
            velocity = new Vector2(velX, velY);
            float posX = message.ReadFloat();
            float posY = message.ReadFloat();
            position = new Vector2(posX, posY);
            rotation = message.ReadFloat();
        }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.ClientStatePacket);
            message.Write(playerId);
            message.Write(tick);
            message.Write(velocity.x);
            message.Write(velocity.y);
            message.Write(position.x);
            message.Write(position.y);
            message.Write(rotation);
        }
    }

    public class InputMessagePacket : Packet
    {
        public string playerId;
        public int tick;
        public Inputs inputs;

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            playerId = message.ReadString();
            tick = message.ReadInt32();
            inputs = new Inputs()
            {
                up = message.ReadBoolean(),
                down = message.ReadBoolean(),
                left = message.ReadBoolean(),
                right = message.ReadBoolean()
            };
        }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.ClientInputMessagePacket);
            message.Write(playerId);
            message.Write(tick);
            message.Write(inputs.up);
            message.Write(inputs.down);
            message.Write(inputs.left);
            message.Write(inputs.right);
        }
    }

    public class InputPayloadPacket : Packet
    {
        public string playerId { get; set; }
        public int tick { get; set; }
        public float horizontal { get; set; }
        public float vertical { get; set; }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            playerId = message.ReadString();
            tick = message.ReadInt32();
            horizontal = message.ReadFloat();
            vertical = message.ReadFloat();
        }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.InputPayloadPacket);
            message.Write(playerId);
            message.Write(tick);
            message.Write(horizontal);
            message.Write(vertical);
        }
    }

    public class StatePayloadPacket : Packet
    {
        public string playerId { get; set; }
        public int tick { get; set; }
        public float posX { get; set; }
        public float posY { get; set; }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            playerId = message.ReadString();
            tick = message.ReadInt32();
            posX = message.ReadFloat();
            posY = message.ReadFloat();
        }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.StatePayloadPacket);
            message.Write(playerId);
            message.Write(tick);
            message.Write(posX);
            message.Write(posY);
        }
    }

    public class LocalPlayerPacket : Packet
    {
        public string id { get; set; }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            id = message.ReadString();
        }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.LocalPlayerPacket);
            message.Write(id);
        }
    }

    public class SpawnPacket : Packet
    {
        public string playerId { get; set; }
        public float x { get; set; }
        public float y { get; set; }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            playerId = message.ReadString();
            x = message.ReadFloat();
            y = message.ReadFloat();
        }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.SpawnPacket);
            message.Write(playerId);
            message.Write(x);
            message.Write(y);
        }
    }

    public class PlayerDisconnectPacket : Packet
    {
        public string playerId { get; set; }

        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            playerId = message.ReadString();
        }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.PlayerDisconnectPacket);
            message.Write(playerId);
        }
    }
}
