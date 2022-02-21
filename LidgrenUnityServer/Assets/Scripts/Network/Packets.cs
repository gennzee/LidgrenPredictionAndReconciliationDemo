using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace LidgrenServer
{
    public enum PacketTypes
    {
        LocalPlayerPacket,
        SpawnPacket,
        InputPayloadPacket,
        StatePayloadPacket,
        PlayerDisconnectPacket,
        MovementInputPacket
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

    public class MovementInputPacket : Packet
    {
        public string playerId { get; set; }
        public float horizontal { get; set; }
        public float vertical { get; set; }
        public float posX { get; set; }
        public float posY { get; set; }
        public float velX { get; set; }
        public float velY { get; set; }
        public override void NetIncomingMessageToPacket(NetIncomingMessage message)
        {
            playerId = message.ReadString();
            horizontal = message.ReadFloat();
            vertical = message.ReadFloat();
            posX = message.ReadFloat();
            posY = message.ReadFloat();
            velX = message.ReadFloat();
            velY = message.ReadFloat();
        }

        public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
        {
            message.Write((byte)PacketTypes.MovementInputPacket);
            message.Write(playerId);
            message.Write(horizontal);
            message.Write(vertical);
            message.Write(posX);
            message.Write(posY);
            message.Write(velX);
            message.Write(velY);
        }
    }
}