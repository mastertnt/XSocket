using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using AwesomeSockets.Domain;
using AwesomeSockets.Domain.Sockets;
using AwesomeSockets.Sockets;
using XScriptedObject;
using AweBuffer = AwesomeSockets.Buffers.Buffer;

namespace XSocket
{
    /// <summary>
    /// A command handler.
    /// </summary>
    /// <param name="pSender">The sender.</param>
    /// <param name="pClientView">The client view.</param>
    /// <param name="pClientView2">A second client view.</param>
    public delegate void ServerCommandHandler(Server pSender, ClientView pClientView, ClientView pClientView2);

    /// <summary>
    /// This class is responsible to keep all informations about an agent.
    /// </summary>
    public class Server : INetworkPoint
    {
        /// <summary>
        /// This class store the main thread.
        /// </summary>
        private Thread mThread;

        /// <summary>
        /// This class stores the thread handler for writing / reading on the socket.
        /// </summary>
        private AgentServerHandler mHandler;

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
        /// This field stores the listen socket. 
        /// </summary>
        public ISocket ListenSocket
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets all the agents.
        /// </summary>
        public ConcurrentDictionary<ClientView, ClientView> Clients { get; } = new ConcurrentDictionary<ClientView, ClientView>();

        /// <summary>
        /// Gets the commands.
        /// </summary>
        /// <value>
        /// The commands.
        /// </value>
        public ConcurrentDictionary<ANetworkCommand, ANetworkCommand> PendingCommands { get; } = new ConcurrentDictionary<ANetworkCommand, ANetworkCommand>();

        /// <summary>
        /// Occurs when a command is received.
        /// </summary>
        public event NetworkPointCommandHandler CommandReceived;

        /// <summary>
        /// Occurs when a command is sent.
        /// </summary>
        public event NetworkPointCommandHandler CommandSent;

        /// <summary>
        /// Occurs when [client connected].
        /// </summary>
        public event ServerCommandHandler ClientConnected;

        /// <summary>
        /// Occurs when [client declared].
        /// </summary>
        public event ServerCommandHandler ClientDeclared;

        /// <summary>
        /// Occurs when [client lost].
        /// </summary>
        public event ServerCommandHandler ClientLost;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        public Server()
        {
            this.CommandInterpreter = new ObjectFactory();
            this.CommandInterpreter.RegisterType("DeclareClient", typeof(DeclareClient));
        }
        
        /// <summary>
        /// Starts the agent.
        /// </summary>
        public void Start(int pPort)
        {
            this.ListenSocket = AweSock.TcpListen(pPort);
            AweSock.TcpAccept(this.ListenSocket, SocketCommunicationTypes.NonBlocking, this.NewClient);
            
            this.mHandler = new AgentServerHandler(this);
            this.mThread = new Thread(this.mHandler.Update);
            this.mThread.Start();
        }

        /// <summary>
        /// Shutdown the agent.
        /// </summary>
        public void Shutdown()
        {
            if (this.ListenSocket != null)
            {
                // Stop the thread.
                this.StopThread();

                // Close all remote connections.
                foreach (var lClient in this.Clients)
                {
                    lClient.Value.Socket?.Close();
                }

                this.Clients.Clear();

                // Close the socket.
                this.ListenSocket.Close();
                this.ListenSocket = null;
            }
            
        }

        /// <summary>
        /// Decodes the specified received buffer.
        /// </summary>
        /// <param name="pReceivedBuffer">The p received buffer.</param>
        /// <param name="pClient">The client.</param>
        internal void Decode(string pReceivedBuffer, ClientView pClient)
        {
            IEnumerable<ANetworkCommand> lCommands = this.CommandInterpreter.Parse(pReceivedBuffer).Cast<ANetworkCommand>();
            foreach (var lCommand in lCommands)
            {
                lCommand.ClientView = pClient;
                if (lCommand is DeclareClient)
                {
                    lCommand.Execute(this);                    
                }
                lCommand.ClientId = lCommand.ClientView.Id;
                this.CommandReceived?.Invoke(this, lCommand);
                Console.WriteLine("[" + lCommand.ClientId + "] = " + lCommand.Encode());
            }
        }

        /// <summary>
        /// Sends the specified network command.
        /// </summary>
        /// <param name="pCommand">The network command.</param>
        public void Send(ANetworkCommand pCommand)
        {
            this.PendingCommands.TryAdd(pCommand, pCommand);
        }

        /// <summary>
        /// Notifies when a command sent.
        /// </summary>
        /// <param name="pCommand">The sent command.</param>
        internal void NotifyCommandSent(ANetworkCommand pCommand)
        {
            this.CommandSent?.Invoke(this, pCommand);
            ANetworkCommand lRemoveCommand;
            this.PendingCommands.TryRemove(pCommand, out lRemoveCommand);
        }

        /// <summary>
        /// This method is called each time a new client is connected.
        /// </summary>
        /// <param name="pClientSocket">The client socket.</param>
        /// <param name="pError">The error during connection.</param>
        /// <returns></returns>
        private Socket NewClient(ISocket pClientSocket, Exception pError)
        {
            if (pClientSocket?.GetSocket() != null)
            {
                AweSock.TcpAccept(this.ListenSocket, SocketCommunicationTypes.NonBlocking, this.NewClient);
                ClientView lClientView = new ClientView() {Socket = pClientSocket};
                this.Clients.TryAdd(lClientView, lClientView);
                this.ClientConnected?.Invoke(this, lClientView, null);
                return pClientSocket.GetSocket();
            }
            
            return null;
        }

        /// <summary>
        /// A client is lost.
        /// </summary>
        /// <param name="pClient">The lost client</param>
        internal void LostClient(ClientView pClient)
        {
            pClient.Socket = null;
            pClient.Status = Status.Lost;
            this.ClientLost?.Invoke(this, pClient, null);
        }

        /// <summary>
        /// Stops the thread.
        /// </summary>
        private void StopThread()
        {
            if (this.mHandler != null)
            {
                this.mHandler.Stop();
                this.mThread.Join(500);
            }
        }

        /// <summary>
        /// Notifies the client declared.
        /// </summary>
        /// <param name="pClientView">The p client view.</param>
        /// <param name="pPending">A pending client which has been removed because a previous client (in state Lost) already exists .</param>
        internal void NotifyClientDeclared(ClientView pClientView, ClientView pPending)
        {
            this.ClientDeclared?.Invoke(this, pClientView, pPending);
        }
    }

    /// <summary>
    /// This class handles the server thread.
    /// </summary>
    public class AgentServerHandler
    {
        /// <summary>
        /// This field stores the server to handle.
        /// </summary>
        private readonly Server mServer;

        /// <summary>
        /// This field stores all output buffers.
        /// </summary>
        private readonly Dictionary<string, AweBuffer> mNetBuffers = new Dictionary<string, AweBuffer>();
        
        /// <summary>
        /// This field the input buffer.
        /// </summary>
        private readonly AweBuffer mInBuffer;

        /// <summary>
        /// This flag is set to true when the thread is running.
        /// </summary>
        private bool mIsRunning = true;

        /// <summary>
        /// Agent server handler.
        /// </summary>
        public AgentServerHandler(Server pServer)
        {
            this.mServer = pServer;
            this.mInBuffer = AweBuffer.New();
        }

        /// <summary>
        /// This method is called by the thread.
        /// </summary>
        public void Update()
        {
            while (this.mIsRunning)
            {
                // Check if the server must send data.
                if (this.mServer.PendingCommands.Any())
                {
                    // Clear all buffers.
                    foreach (var lNetBuffer in this.mNetBuffers)
                    {
                        AweBuffer.ClearBuffer(lNetBuffer.Value);
                    }

                    // Encode all commands.
                    foreach (var lCommand in this.mServer.PendingCommands)
                    {
                        if (string.IsNullOrWhiteSpace(lCommand.Value.ClientId))
                        {
                            foreach (var lClientView in this.mServer.Clients)
                            {
                                if (string.IsNullOrWhiteSpace(lClientView.Value.Id) == false)
                                {
                                    if (this.mNetBuffers.ContainsKey(lClientView.Value.Id) == false)
                                    {
                                        this.mNetBuffers.Add(lClientView.Value.Id, AweBuffer.New());
                                    }

                                    lCommand.Value.HasBeenEncoded = true;
                                    AweBuffer.Add(this.mNetBuffers[lClientView.Value.Id], lCommand.Value.Encode() + ";");
                                }
                                
                            }
                        }
                        else
                        {
                            if (this.mNetBuffers.ContainsKey(lCommand.Value.ClientId) == false)
                            {
                                this.mNetBuffers.Add(lCommand.Value.ClientId, AweBuffer.New());
                            }
                            lCommand.Value.HasBeenEncoded = true;
                            AweBuffer.Add(this.mNetBuffers[lCommand.Value.ClientId], lCommand.Value.Encode() + ";");
                        }

                       
                    }

                    foreach (var lNetBuffer in this.mNetBuffers)
                    {
                        AweBuffer.FinalizeBuffer(lNetBuffer.Value);
                    }

                    // Send data to each client.
                    bool lErrorOccured = false;
                    foreach (var lNetBuffer in this.mNetBuffers)
                    {
                        var lClient = this.mServer.Clients.FirstOrDefault(pClient => pClient.Value.Id == lNetBuffer.Key);
                        if (lClient.Value != null && lClient.Value.Status != Status.Lost)
                        {
                            try
                            {
                                AweSock.SendMessage(lClient.Value.Socket, lNetBuffer.Value);
                            }
                            catch
                            {
                                lErrorOccured = true;
                                Console.WriteLine("Lost client");
                                this.mServer.LostClient(lClient.Value);
                            }
                        }

                    }

                    // Notifies all commands.
                    if (lErrorOccured == false)
                    {
                        foreach (var lCommand in this.mServer.PendingCommands)
                        {
                            if (lCommand.Value.HasBeenEncoded)
                            {
                                this.mServer.NotifyCommandSent(lCommand.Value);
                            }
                        }
                    }
                }

                // Try to receive some data.
                
                foreach (var lClientView in this.mServer.Clients)
                {
                    try
                    {
                        StringBuilder lInBuffer = new StringBuilder();
                        if (lClientView.Value.Socket != null && lClientView.Value.Socket.GetBytesAvailable() != 0)
                        {
                            AweBuffer.ClearBuffer(this.mInBuffer);
                            Tuple<int, EndPoint> lReceived = AweSock.ReceiveMessage(lClientView.Value.Socket, this.mInBuffer);
                            if (lReceived.Item1 != 0)
                            {
                                lInBuffer.Append(AweBuffer.Get<string>(this.mInBuffer));

                                if (lInBuffer.Length != 0)
                                {
                                    this.mServer.Decode(lInBuffer.ToString(), lClientView.Value);
                                }
                            }
                        }
                       
                    }
                    catch
                    {
                        Console.WriteLine("Lost client");
                        this.mServer.LostClient(lClientView.Value);
                    }
                }
                

                //Console.WriteLine("[SERVER] Sleep");
                Thread.Sleep(1000);
            }

            //Console.WriteLine("[SERVER] LeaveThread");
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
