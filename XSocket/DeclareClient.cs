using System.Linq;

namespace XSocket
{
    /// <summary>
    /// A client declaration command.
    /// </summary>
    /// <seealso cref="ANetworkCommand" />
    public class DeclareClient : ANetworkCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeclareClient"/> class.
        /// </summary>
        /// <param name="pClientId">Name of the p client.</param>
        public DeclareClient(string pClientId)
        {
            this.ClientId = pClientId;
        }

        /// <summary>
        /// Make the execution code.
        /// </summary>
        /// <param name="pContext">The context.</param>
        /// <returns>
        /// Empty if succed, an error message in case of failure.
        /// </returns>
        protected override string DoExecute(INetworkPoint pContext)
        {
            Server lServer = pContext as Server;
            if (lServer.Clients.ContainsKey(this.ClientView))
            {
                var lPreviousClient = lServer.Clients.FirstOrDefault(pClient => pClient.Value.Id == this.ClientId);
                if (lPreviousClient.Value != null)
                {
                    //lPreviousClient.Value.Status = Status.Declared;
                    ClientView lRemoved;
                    lServer.Clients.TryRemove(lPreviousClient.Key, out lRemoved);
                    lServer.Clients[this.ClientView].Id = this.ClientId;
                    lServer.Clients[this.ClientView].Status = Status.Declared;
                    lServer.NotifyClientDeclared(this.ClientView, lPreviousClient.Value);
                }
                else
                {
                    lServer.Clients[this.ClientView].Id = this.ClientId;
                    lServer.Clients[this.ClientView].Status = Status.Declared;
                    lServer.NotifyClientDeclared(this.ClientView, null);
                }
               
            }
            return "";
        }

        /// <summary>
        /// Encores this instance.
        /// </summary>
        /// <returns></returns>
        public override string Encode()
        {
            return "DeclareClient(\"" + this.ClientId + "\")";

        }
    }
}
