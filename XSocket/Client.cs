using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;
using AwesomeSockets.Domain.Sockets;
using AwesomeSockets.Sockets;
using XScriptedObject;
using AweBuffer = AwesomeSockets.Buffers.Buffer;

namespace XSocket
{
    /// <summary>
    /// This class is responsible to find a server on the network.
    /// After the lookup, some commands can be sent by the server to execute local actions.
    /// Some informations are sent from the agent to the server.
    /// </summary>
    public class Client : INetworkPoint
    {
        /// <summary>
        /// This class store the main thread.
        /// </summary>
        private Thread mThread;

        /// <summary>
        /// This class stores the server adress.
        /// </summary>
        private string mServerAdress;

        /// <summary>
        /// This class stores the server port.
        /// </summary>
        private int mServerPort;

        /// <summary>
        /// This class stores the thread handler for writing / reading on the socket.
        /// </summary>
        private AgentClientHandler mHandler;

        /// <summary>
        /// This field stores an index of reconnection.
        /// </summary>
        private int mReconnectIndex;

        /// <summary>
        /// This field stores reconnection timer.
        /// </summary>
        private readonly System.Timers.Timer mReconnectionTimer;
        
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// This field stores the socket of the client.
        /// </summary>
        public ISocket Socket
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the command interpreter.
        /// </summary>
        /// <value>
        /// The command interpreter.
        /// </value>
        public ObjectFactory CommandInterpreter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the commands.
        /// </summary>
        /// <value>
        /// The commands.
        /// </value>
        public ConcurrentStack<ANetworkCommand> PendingCommands { get; } = new ConcurrentStack<ANetworkCommand>();

        /// <summary>
        /// Occurs when a command is received.
        /// </summary>
        public event NetworkPointCommandHandler CommandReceived;

        /// <summary>
        /// Occurs when a command is sent.
        /// </summary>
        public event NetworkPointCommandHandler CommandSent;

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        public Client()
        {
            this.CommandInterpreter = new ObjectFactory();
            this.mReconnectionTimer = new System.Timers.Timer(2000);
            this.mReconnectionTimer.Elapsed += this.OnReconnectionElasped;
            this.mReconnectionTimer.AutoReset = true;
        }

        /// <summary>
        /// Starts the agent.
        /// </summary>
        /// <param name="pServerAdress">The server adress.</param>
        /// <param name="pServerPort">The server port.</param>
        /// <param name="pClientId">The client identifier.</param>
        public void Start(string pServerAdress, int pServerPort, string pClientId)
        {
            try
            {
                this.mServerAdress = pServerAdress;
                this.mServerPort = pServerPort;
                this.Id = pClientId;
                this.Socket = AweSock.TcpConnect(pServerAdress, pServerPort);

                this.mHandler = new AgentClientHandler(this, this.mReconnectIndex);
                this.mThread = new Thread(this.mHandler.Update) { Name = "Client " + this.mReconnectIndex };
                this.mThread.Start();
                this.mReconnectIndex++;
                this.mReconnectionTimer.Enabled = false;

                this.Send(new DeclareClient(pClientId));
            }
            catch
            {
                Console.WriteLine("["+ this.Id +"] Try to reconnect");
                this.mReconnectionTimer.Enabled = true;
            }
        }

        /// <summary>
        /// Called when [reconnection elasped].
        /// </summary>
        /// <param name="pÊventSender">The sender.</param>
        /// <param name="pEventArgs">The <see cref="System.Timers.ElapsedEventArgs"/> instance containing the event data.</param>
        private void OnReconnectionElasped(object pÊventSender, ElapsedEventArgs pEventArgs)
        {
            this.Start(this.mServerAdress, this.mServerPort, this.Id);
        }

        /// <summary>
        /// Sends the specified network command.
        /// </summary>
        /// <param name="pCommand">The network command.</param>
        public void Send(ANetworkCommand pCommand)
        {
            this.PendingCommands.Push(pCommand);
        }

        /// <summary>
        /// Shutdown the agent.
        /// </summary>
        public void Shutdown()
        {
            try
            {
                // Stop the reconnection.
                this.mReconnectionTimer.Enabled = false;

                // Stop the thread.
                this.StopThread();

                // Close the socket.
                this.Socket.Close();
                this.Socket = null;
            }
            catch (Exception lException)
            {
                Console.WriteLine(lException);
            }
        }

        /// <summary>
        /// The server is lost.
        /// </summary>
        internal void LostServer()
        {
            Console.WriteLine("[" + this.Id + "] Lost server");

            this.Shutdown();
            this.Start(this.mServerAdress, this.mServerPort, this.Id);
        }

        /// <summary>
        /// Decodes the specified received buffer.
        /// </summary>
        /// <param name="pReceivedBuffer">The p received buffer.</param>
        internal void Decode(string pReceivedBuffer)
        {
            IEnumerable<ANetworkCommand> lCommands = this.CommandInterpreter.Parse(pReceivedBuffer).Cast<ANetworkCommand>();
            foreach (var lCommand in lCommands)
            {
                this.CommandReceived?.Invoke(this, lCommand);
            }
        }

        /// <summary>
        /// Notifies when a command sent.
        /// </summary>
        /// <param name="pCommand">The sent command.</param>
        internal void NotifyCommandSent(ANetworkCommand pCommand)
        {
            this.CommandSent?.Invoke(this, pCommand);
        }

        /// <summary>
        /// Stops the thread.
        /// </summary>
        private void StopThread()
        {
            if (this.mHandler != null)
            {
                this.mHandler.Stop();
                this.mThread.Join(2000);
            }
        }
    }

    /// <summary>
    /// This class handles the client thread.
    /// </summary>
    public class AgentClientHandler
    {
        /// <summary>
        /// This field stores the client to handle.
        /// </summary>
        private readonly Client mClient;

        /// <summary>
        /// This flag is set to true when the thread is running.
        /// </summary>
        private bool mIsRunning = true;

        /// <summary>
        /// This field stores the index of the thread.
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly int mIndex;

        /// <summary>
        /// This field stores all output buffers.
        /// </summary>
        private readonly AweBuffer mOutBuffer;

        /// <summary>
        /// This field the input buffer.
        /// </summary>
        private readonly AweBuffer mInBuffer;

        /// <summary>
        /// Agent client handler.
        /// </summary>
        public AgentClientHandler(Client pClient, int pIndex)
        {
            this.mClient = pClient;
            this.mIndex = pIndex;
            this.mOutBuffer = AweBuffer.New();
            this.mInBuffer = AweBuffer.New();
            Console.WriteLine("[" + this.mClient.Id + "] Thead started " + this.mIndex);
        }

        /// <summary>
        /// This method is called by the thread.
        /// </summary>
        public void Update()
        {
            while (this.mIsRunning)
            {
                if (this.mClient.Socket != null)
                {
                    // Clear output buffers.
                    AweBuffer.ClearBuffer(this.mOutBuffer);
                    AweBuffer.ClearBuffer(this.mInBuffer);

                    // Encode all commands.
                    StringBuilder lBuffer = new StringBuilder();
                    foreach (var lCommand in this.mClient.PendingCommands)
                    {
                        lCommand.ClientId = this.mClient.Id;
                        AweBuffer.Add(this.mOutBuffer, lCommand.Encode());
                        lBuffer.AppendLine(";"); // End of command.
                        lCommand.HasBeenEncoded = true;
                    }

                    AweBuffer.FinalizeBuffer(this.mOutBuffer);

                    // Send data to the server.
                    try
                    {
                        AweSock.SendMessage(this.mClient.Socket, this.mOutBuffer);
                    }
                    catch (Exception lEx)
                    {
                        this.mClient.LostServer();
                        break;
                    }

                    // Notifies all commands.
                    foreach (var lCommand in this.mClient.PendingCommands)
                    {
                        if (lCommand.HasBeenEncoded)
                        {
                            this.mClient.NotifyCommandSent(lCommand);
                        }
                    }

                    this.mClient.PendingCommands.Clear();

                    // Try to receive some data.
                    StringBuilder lInBuffer = new StringBuilder();
                    try
                    {
                        if (this.mClient.Socket.GetBytesAvailable() != 0)
                        {
                            Tuple<int, EndPoint> lReceived = AweSock.ReceiveMessage(this.mClient.Socket, this.mInBuffer);
                            if (lReceived.Item1 == 0)
                            {
                                lInBuffer.Append(AweBuffer.Get<string>(this.mInBuffer));
                            }
                        }

                    }
                    catch (Exception lEx)
                    {
                        this.mClient.LostServer();
                        break;
                    }

                    if (lInBuffer.Length != 0)
                    {
                        this.mClient.Decode(lInBuffer.ToString());
                    }
                }


                Console.WriteLine("[" + this.mClient.Id + "] Sleep");
                Thread.Sleep(1000);
            }

            Console.WriteLine("[" + this.mClient.Id + "] LeaveThread");
        }

        /// <summary>
        /// Stop the thread handler.
        /// </summary>
        internal void Stop()
        {
            this.mIsRunning = false;
        }
    }
}
