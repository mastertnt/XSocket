using System.ComponentModel;
using AwesomeSockets.Domain.Sockets;

namespace XSocket
{
    /// <summary>
    /// This enumertion lists all the client status.
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// A connection occured but the first transation doesn't occurent (client declaration). 
        /// </summary>
        Connected,    
        /// <summary>
        /// The client declaration has been done.
        /// </summary>
        Declared,

        /// <summary>
        /// The client has been lost.
        /// </summary>
        Lost,       
    }

    /// <summary>
    /// This class stores all the information about a client.
    /// </summary>
    public class ClientView : INotifyPropertyChanged
    {
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
        /// 
        /// </summary>
        public ISocket Socket
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public Status Status
        {
            get;
            set;
        }

        /// <summary>
        /// Raise the event when a property is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This class stores the agent.
        /// </summary>
        public ClientView()
        {
            this.Id = "undefined";
        }
    }
}
