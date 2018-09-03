namespace XSocket
{
    /// <summary>
    /// A command handler.
    /// </summary>
    /// <param name="pSender">The sender.</param>
    /// <param name="pCommand">The command.</param>
    public delegate void NetworkPointCommandHandler(INetworkPoint pSender, ANetworkCommand pCommand);

    /// <summary>
    /// A network point.
    /// </summary>
    public interface INetworkPoint
    {
        /// <summary>
        /// Occurs when a command is received.
        /// </summary>
        event NetworkPointCommandHandler CommandReceived;

        /// <summary>
        /// Occurs when a command is sent.
        /// </summary>
        event NetworkPointCommandHandler CommandSent;
    }
}
