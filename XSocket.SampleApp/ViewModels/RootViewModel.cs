using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using XTreeListView.ViewModel.Generic;

namespace XSocket.SampleApp.ViewModels
{
    /// <summary>
    /// This class defines the view model of the multi column tree view.
    /// </summary>
    /// <!-- DPE -->
    internal class ServerRootViewModel : ARootHierarchicalItemViewModel<object>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerRootViewModel"/> class.
        /// </summary>
        public ServerRootViewModel(ObservableCollection<ClientView> pModel)
        {
            pModel.CollectionChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Called when [model changed].
        /// </summary>
        /// <param name="pEventSender">The p event sender.</param>
        /// <param name="pEventsArgs">The <see cref="NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void OnModelChanged(object pEventSender, NotifyCollectionChangedEventArgs pEventsArgs)
        {
            switch (pEventsArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    foreach (var lItem in pEventsArgs.NewItems)
                    {
                        this.AddChild(new ClientViewModel(lItem as ClientView));
                    }
                    }
                break;

                case NotifyCollectionChangedAction.Remove:
                {
                    foreach (var lItem in pEventsArgs.OldItems)
                    {
                        this.RemoveChild(this.Children.FirstOrDefault(pChild => pChild.UntypedOwnedObject == lItem));
                    }
                }
                break;

            }
        }

        #endregion // Constructors.
    }
}
