using System.ComponentModel;
using System.Windows.Media;
using XTreeListView.ViewModel.Generic;

namespace XSocket.SampleApp.ViewModels
{
    /// <summary>
    /// A client view.
    /// </summary>
    /// <seealso cref="ClientView" />
    public class ClientViewModel : AHierarchicalItemViewModel<ClientView>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientViewModel"/> class.
        /// </summary>
        /// <param name="pOwnedObject">The owned object.</param>
        public ClientViewModel(ClientView pOwnedObject)
            : base(pOwnedObject)
        {
            pOwnedObject.PropertyChanged += this.OnPropertyChanged;
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="pEventSender">The sender.</param>
        /// <param name="pEventArgs">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void OnPropertyChanged(object pEventSender, PropertyChangedEventArgs pEventArgs)
        {
            this.NotifyPropertyChanged(pEventArgs.PropertyName);
        }

        /// <summary>
        /// Gets the id of client.
        /// </summary>
        public string Id
        {
            get
            {
                return this.OwnedObject.Id;
            }
        }

        /// <summary>
        /// Gets the status of client.
        /// </summary>
        public object Status
        {
            get
            {
                return this.OwnedObject.Status;
            }
        }

        /// <summary>
        /// Gets the icon to display in the item.
        /// </summary>
        public override ImageSource IconSource
        {
            get { return null; }
        }
    }
}
