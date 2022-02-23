using System;
using System.Collections.Generic;
using UnityEngine;
using Lidgren.Network;
using UnityEngine.SceneManagement;

namespace LidgrenServer
{
    public class PlayerMovement : MonoBehaviour
    {
        public StatePayloadPacket[] stateBuffer;
        public Queue<InputPayloadPacket> inputQueue;

        private void Awake()
        {
            stateBuffer = new StatePayloadPacket[Constant.BUFFER_SIZE];
            inputQueue = new Queue<InputPayloadPacket>();            
        }
        
        public void OnClientInput(InputPayloadPacket inputPayload)
        {
            inputQueue.Enqueue(inputPayload);
        }

        public void HandleTick()
        {
            // Process the input queue
            int bufferIndex = -1;
            while (inputQueue.Count > 0)
            {
                InputPayloadPacket inputPayload = inputQueue.Dequeue();

                bufferIndex = inputPayload.tick % Constant.BUFFER_SIZE;

                StatePayloadPacket statePayload = ProcessMovement(inputPayload);
                stateBuffer[bufferIndex] = statePayload;
            }

            if (bufferIndex != -1)
            {
                SendStateMovementPayloadPacket(stateBuffer[bufferIndex]);
            }
        }

        public StatePayloadPacket ProcessMovement(InputPayloadPacket inputPayload)
        {
            Vector3 inputVector = new Vector3(inputPayload.horizontal, inputPayload.vertical, 0);
            // Should always be in sync with same function on Client
            transform.position += inputVector * Constant.MOVE_SPEED * Server.timeBetweenTicks;
            // Set data for playerDatas
            /*Server.playerDatas[inputPayload.playerId].posX = transform.position.x;
            Server.playerDatas[inputPayload.playerId].posX = transform.position.y;*/

            return new StatePayloadPacket()
            {
                playerId = inputPayload.playerId,
                tick = inputPayload.tick,
                posX = transform.position.x,
                posY = transform.position.y
            };
        }
        
        public void SendStateMovementPayloadPacket(StatePayloadPacket packet)
        {
            Debug.LogWarning("Send StateMovementPayloadPacket for " + packet.playerId);

            NetOutgoingMessage outgoingMessage = Server.server.CreateMessage();
            IPacket statePayloadPacket = packet;
            statePayloadPacket.PacketToNetOutGoingMessage(outgoingMessage);
            Server.server.SendMessage(outgoingMessage, Server.server.Connections, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }
    
}