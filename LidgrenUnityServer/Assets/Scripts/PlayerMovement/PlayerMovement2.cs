using System;
using System.Collections.Generic;
using UnityEngine;
using Lidgren.Network;
using UnityEngine.SceneManagement;

namespace LidgrenServer
{
    public class PlayerMovement2 : MonoBehaviour
    {
        private Rigidbody2D rigid;
        private ClientStatePacket clientStatePacket;

        private void Awake()
        {
            rigid = GetComponent<Rigidbody2D>();
            clientStatePacket = new ClientStatePacket();
        }

        public void HandleTick(InputMessagePacket packet)
        {
            PrePhysicsStep(rigid, packet.inputs);
            Physics2D.Simulate(Time.fixedDeltaTime);
            DataSendToClientAfterSimulatePhysic(packet);
        }

        public void DataSendToClientAfterSimulatePhysic(InputMessagePacket packet)
        {
            clientStatePacket.playerId = packet.playerId;
            clientStatePacket.tick = packet.tick;
            clientStatePacket.velocity = rigid.velocity;
            clientStatePacket.position = rigid.position;
            clientStatePacket.rotation = rigid.rotation;
            SendClientStateToClient(clientStatePacket);
        }

        public void SendClientStateToClient(ClientStatePacket packet)
        {
            Debug.LogWarning("Send SendClientStateToClient for " + packet.playerId);

            NetOutgoingMessage outgoingMessage = Server.server.CreateMessage();
            IPacket statePayloadPacket = packet;
            statePayloadPacket.PacketToNetOutGoingMessage(outgoingMessage);
            Server.server.SendMessage(outgoingMessage, Server.server.Connections, NetDeliveryMethod.ReliableOrdered, 0);
        }

        private void PrePhysicsStep(Rigidbody2D rigid, Inputs inputs)
        {
            if (inputs.up)
            {
                rigid.velocity = new Vector2(rigid.velocity.x, Constant.MOVE_SPEED);
            }
            if (inputs.down)
            {
                rigid.velocity = new Vector2(rigid.velocity.x, -Constant.MOVE_SPEED);
            }
            if (inputs.left)
            {
                rigid.velocity = new Vector2(-Constant.MOVE_SPEED, rigid.velocity.y);
            }
            if (inputs.right)
            {
                rigid.velocity = new Vector2(Constant.MOVE_SPEED, rigid.velocity.y);
            }
        }
    }

}