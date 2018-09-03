namespace XSocket
{
    /// <summary>
    /// A ping command.
    /// </summary>
    /// <seealso cref="ANetworkCommand" />
    public class Ping : ANetworkCommand
    {
        /// <summary>
        /// Make the execution code.
        /// </summary>
        /// <param name="pContext">The context.</param>
        /// <returns>
        /// Empty if succed, an error message in case of failure.
        /// </returns>
        protected override string DoExecute(INetworkPoint pContext)
        {
            return "";
        }

        /// <summary>
        /// Encores this instance.
        /// </summary>
        /// <returns></returns>
        public override string Encode()
        {
            return "Ping()";
        }
    }
}
