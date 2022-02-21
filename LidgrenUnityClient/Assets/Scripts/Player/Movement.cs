using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Lidgren.Network;

namespace LidgrenClient
{
    public class Movement : MonoBehaviour
    {
        public bool isLocal;

        private float horizontal;
        private float vertical;
        private Rigidbody2D rigid;
        private Vector2 position;
        public Vector2 Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }
        private Vector2 velocity;
        public Vector2 Velocity
        {
            get
            {
                return velocity;
            }
            set
            {
                velocity = value;
            }
        }

        private StatePayloadPacket[] stateBuffer;
        private InputPayloadPacket[] inputBuffer;
        private StatePayloadPacket latestServerState;
        private StatePayloadPacket lastProcessedState;

        private void Awake()
        {
            rigid = GetComponent<Rigidbody2D>();
            stateBuffer = new StatePayloadPacket[Constant.BUFFER_SIZE];
            inputBuffer = new InputPayloadPacket[Constant.BUFFER_SIZE];
        }

        private void Update()
        {
            if (!isLocal) return;
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
        }

        public void OnServerMovementState(StatePayloadPacket packet)
        {
            if (!isLocal) transform.position = new Vector2(packet.posX, packet.posY);
            latestServerState = packet;
        }

        public void HandleInputMovement()
        {
            if (!isLocal) return;
            if (latestServerState != null && (lastProcessedState == null || !latestServerState.Equals(lastProcessedState)))
            {
                HandleServerReconciliation();
            }
            int bufferIndex = Client.currentTick % Constant.BUFFER_SIZE;

            // Add payload to inputBuffer
            InputPayloadPacket inputPayload = new InputPayloadPacket() { playerId = Client.LocalPlayerId, tick = Client.currentTick, horizontal = horizontal, vertical = vertical };
            inputBuffer[bufferIndex] = inputPayload;

            // Add payload to stateBuffer
            stateBuffer[bufferIndex] = ProcessMovement(inputPayload);

            // Send input to server
            if (horizontal != 0 || vertical != 0) SendInputPayloadPacketToServer(inputPayload);
        }

        StatePayloadPacket ProcessMovement(InputPayloadPacket inputPayload)
        {
            Vector3 inputVector = new Vector3(inputPayload.horizontal, inputPayload.vertical, 0);
            // Should always be in sync with same function on Server
            transform.position += inputVector * Constant.MOVE_SPEED * Client.timeBetweenTicks;

            return new StatePayloadPacket()
            {
                playerId = inputPayload.playerId,
                tick = inputPayload.tick,
                posX = transform.position.x,
                posY = transform.position.y
            };
        }

        private void SendInputPayloadPacketToServer(InputPayloadPacket inputPayload)
        {
            NetOutgoingMessage message = Client.client.CreateMessage();
            IPacket inputPayloadPacket = inputPayload;
            inputPayloadPacket.PacketToNetOutGoingMessage(message);
            Client.client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
            Client.client.FlushSendQueue();
        }

        void HandleServerReconciliation()
        {
            lastProcessedState = latestServerState;

            int serverStateBufferIndex = latestServerState.tick % Constant.BUFFER_SIZE;
            Vector3 tempLatestServerState = new Vector3(latestServerState.posX, latestServerState.posY, 0);
            Vector3 tempServerState = new Vector3(stateBuffer[serverStateBufferIndex].posX, stateBuffer[serverStateBufferIndex].posY, 0);
            float positionError = Vector3.Distance(tempLatestServerState, tempServerState);

            if (positionError > 0.001f)
            {
                Debug.Log("We have to reconcile bro");
                // Rewind & Replay
                transform.position = tempLatestServerState;

                // Update buffer at index of latest server state
                stateBuffer[serverStateBufferIndex] = latestServerState;

                // Now re-simulate the rest of the ticks up to the current tick on the client
                int tickToProcess = latestServerState.tick + 1;

                while (tickToProcess < Client.currentTick)
                {
                    // Process new movement with reconciled state
                    StatePayloadPacket statePayload = ProcessMovement(inputBuffer[tickToProcess]);

                    // Update buffer with recalculated state
                    int bufferIndex = tickToProcess % Constant.BUFFER_SIZE;
                    stateBuffer[bufferIndex] = statePayload;

                    tickToProcess++;
                }
            }
        }

        private void SendMovementToServer()
        {
            NetOutgoingMessage message = Client.client.CreateMessage();
            IPacket movementInputPacket = new MovementInputPacket()
            {
                playerId = Client.LocalPlayerId,
                horizontal = horizontal,
                vertical = vertical,
                posX = rigid.position.x,
                posY = rigid.position.y,
                velX = rigid.velocity.x,
                velY = rigid.velocity.y
            };
            movementInputPacket.PacketToNetOutGoingMessage(message);
            Client.client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
            Client.client.FlushSendQueue();
        }
    }
}
