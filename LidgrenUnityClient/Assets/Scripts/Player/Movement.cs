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

        private Rigidbody2D rigid;

        private Inputs[] clientInputBuffer;
        private ClientStatePacket[] clientStateBuffer;
        private Inputs inputs;

        private void Awake()
        {
            rigid = GetComponent<Rigidbody2D>();
            clientStateBuffer = new ClientStatePacket[Constant.BUFFER_SIZE];
            clientInputBuffer = new Inputs[Constant.BUFFER_SIZE];
        }

        public void HandleInputMovement()
        {
            if (!isLocal) return;
            inputs = GetPlayerInputs();

            InputMessagePacket inputMsg = GetInputMessage();

            PrePhysicsStep(rigid, inputs);

            Physics2D.Simulate(Time.fixedDeltaTime);

            int bufferIndex = Client.currentTick % Constant.BUFFER_SIZE;
            clientInputBuffer[bufferIndex] = inputs;
            clientStateBuffer[bufferIndex] = new ClientStatePacket()
            {
                position = rigid.position,
                rotation = rigid.rotation
            };

            SendInputMessageToServer(inputMsg);
        }

        private InputMessagePacket GetInputMessage()
        {
            return new InputMessagePacket()
            {
                playerId = Client.LocalPlayerId,
                tick = Client.currentTick,
                inputs = inputs
            };
        }

        private Inputs GetPlayerInputs()
        {
            Inputs inputs;
            inputs.up = Input.GetKey(KeyCode.W);
            inputs.down = Input.GetKey(KeyCode.S);
            inputs.left = Input.GetKey(KeyCode.A);
            inputs.right = Input.GetKey(KeyCode.D);
            inputs.jump = Input.GetKey(KeyCode.Space);
            return inputs;
        }

        public void PrePhysicsStep(Rigidbody2D rigid, Inputs inputs)
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

        public void SendInputMessageToServer(InputMessagePacket inputMsg)
        {
            NetOutgoingMessage message = Client.client.CreateMessage();
            IPacket inputMessagePacket = inputMsg;
            inputMessagePacket.PacketToNetOutGoingMessage(message);
            Client.client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
            Client.client.FlushSendQueue();
        }

        public void OnServerMovementState(ClientStatePacket packet)
        {
            if (!isLocal)
            {
                rigid.position = packet.position;
            }
            else
            {
                int bufferIndex = packet.tick % Constant.BUFFER_SIZE;
                Vector2 positionError = packet.position - clientStateBuffer[bufferIndex].position;
                if (positionError.sqrMagnitude > 0.00001f)
                {
                    Debug.LogError("Rewind  & Replay");
                    // rewind and replay
                    rigid.velocity = packet.velocity;
                    rigid.position = packet.position;
                    rigid.rotation = packet.rotation;

                    int rewindTick = packet.tick + 1;
                    while (rewindTick < Client.currentTick)
                    {
                        bufferIndex = rewindTick % packet.tick;
                        clientInputBuffer[bufferIndex] = inputs;
                        clientStateBuffer[bufferIndex] = new ClientStatePacket() { position = rigid.position, rotation = rigid.rotation };

                        PrePhysicsStep(rigid, inputs);
                        rewindTick++;
                    }
                }
            }
        }
    }
}
