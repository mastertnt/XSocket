using System;

namespace XSocket
{
    /// <summary>
    /// A command handler.
    /// </summary>
    /// <param name="pSender">The sender.</param>
    /// <param name="pErrorMessage">The error message.</param>
    public delegate void CommandHander(ANetworkCommand pSender, string pErrorMessage);
    
    /// <summary>
    /// This class defines a command.
    /// </summary>
    public abstract class ANetworkCommand
    {
        /// <summary>
        /// Gets the client identifier.
        /// </summary>
        /// <value>
        /// The client identifier.
        /// </value>
        public string ClientId { get; internal set; }

        /// <summary>
        /// Gets or sets the client view.
        /// </summary>
        /// <value>
        /// The client view.
        /// </value>
        internal ClientView ClientView
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has been encoded.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has been encoded; otherwise, <c>false</c>.
        /// </value>
        internal bool HasBeenEncoded { get; set; }

        /// <summary>
        /// Event raised when the command failed during execution.
        /// </summary>
        public CommandHander Failed;

        /// <summary>
        /// Event raised when the command succeed during execution.
        /// </summary>
        public CommandHander Succeed ;

        /// <summary>
        /// Event raised when the command starts its execution.
        /// </summary>
        public CommandHander Executing;

        /// <summary>
        /// Initializes a new instance of the <see cref="ANetworkCommand"/> class.
        /// </summary>
        protected ANetworkCommand()
        {
            this.HasBeenEncoded = false;
        }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        /// <param name="pContext">The context.</param>
        public void Execute(INetworkPoint pContext)
        {
            try
            {
                this.Executing?.Invoke(this, null);

                string lErrorMessage = this.DoExecute(pContext);
                if (string.IsNullOrWhiteSpace(lErrorMessage) == false)
                {
                    this.Failed?.Invoke(this, lErrorMessage);
                }
                else
                {
                    this.Succeed?.Invoke(this, null);
                }
            }
            catch (Exception lException)
            {

                this.Failed?.Invoke(this, lException.ToString());
            }
        }

        /// <summary>
        /// Make the execution code.
        /// </summary>
        /// <param name="pContext">The context.</param>
        /// <returns>Empty if succed, an error message in case of failure.</returns>
        protected abstract string DoExecute(INetworkPoint pContext);

        /// <summary>
        /// Encores this instance.
        /// </summary>
        /// <returns></returns>
        public abstract string Encode();
    }
}
