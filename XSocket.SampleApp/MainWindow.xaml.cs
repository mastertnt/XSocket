using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using XSocket.SampleApp.ViewModels;
using XTreeListView.ViewModel;

namespace XSocket.SampleApp
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        /// <summary>
        /// This field stores the server information.
        /// </summary>
        private readonly Server mServer;

        /// <summary>
        /// This field stores the server information.
        /// </summary>
        private readonly List<Client> mClients;

        /// <summary>
        /// This field stores the client index.
        /// </summary>
        private int mClientIndex = 0;

        /// <summary>
        /// This field stores a timer to send ping.
        /// </summary>
        readonly DispatcherTimer mTimer = new DispatcherTimer();

        /// <summary>
        /// Gets or sets the client view.
        /// </summary>
        /// <value>
        /// The client view.
        /// </value>
        public ObservableCollection<ClientView> ClientView
        {
            get;
            set;
        }

        /// <summary>
        /// Raise the event when a property is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <inheritdoc />
        public MainWindow()
        {
            this.InitializeComponent();
            this.mServer = new Server();
            this.mServer.CommandSent += this.OnCommandSent;
            this.mServer.ClientLost += this.OnClientLost;
            this.mServer.ClientDeclared += this.OnClientDeclared;
            this.mServer.ClientConnected += this.OnClientConnected;
            this.mClients = new List<Client>();

            this.ClientView = new ObservableCollection<ClientView>();
            this.DataContext = this;
            this.Clients.ViewModel = new ServerRootViewModel(this.ClientView);
        }

        /// <summary>
        /// Called when [client declared].
        /// </summary>
        /// <param name="pSender">The sender.</param>
        /// <param name="pClientView">The client view.</param>
        /// <param name="pClientView2">The second client view.</param>
        private void OnClientDeclared(Server pSender, ClientView pClientView, ClientView pClientView2)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (pClientView2 != null)
                {
                    if (this.ClientView.FirstOrDefault(pClient => pClient.Id == pClientView2.Id) != null)
                    {
                        this.ClientView.Remove(pClientView2);
                    }
                }
                if (this.ClientView.FirstOrDefault(pClient => pClient.Id == pClientView.Id) == null)
                {
                    this.ClientView.Add(pClientView);
                }

                
            });
        }

        /// <summary>
        /// Called when [client lost].
        /// </summary>
        /// <param name="pSender">The sender.</param>
        /// <param name="pClientView">The client view.</param>
        /// <param name="pClientView2">The second client view.</param>
        private void OnClientLost(Server pSender, ClientView pClientView, ClientView pClientView2)
        {
            this.Dispatcher.Invoke(this.RefreshClientButtonStates);
        }

        /// <summary>
        /// Called when [client connected].
        /// </summary>
        /// <param name="pSender">The sender.</param>
        /// <param name="pClientView">The client view.</param>
        /// <param name="pClientView2">The second client view.</param>
        private void OnClientConnected(Server pSender, ClientView pClientView, ClientView pClientView2)
        {
           
        }

        /// <summary>
        /// Called when a command is sent on the server.
        /// </summary>
        /// <param name="pSender">The sender.</param>
        /// <param name="pCommand">The command.</param>
        private void OnCommandSent(INetworkPoint pSender, ANetworkCommand pCommand)
        {
            Console.WriteLine(@"Command sent " + pCommand.GetType().Name);
            this.mTimer.Start();
        }

        /// <summary>
        /// Called when [timer raised].
        /// </summary>
        /// <param name="pSender">The sender.</param>
        /// <param name="pEventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnTimerRaised(object pSender, EventArgs pEventArgs)
        {
            Console.WriteLine(@"Try to send a Ping");
            this.mServer.Send(new Ping());
            this.mTimer.Stop();
        }

        /// <summary>
        /// Handles the Click event of the Start control.
        /// </summary>
        /// <param name="pSender">The event sender.</param>
        /// <param name="pEventArgs">The event arguments.</param>
        private void Start_Click(object pSender, System.Windows.RoutedEventArgs pEventArgs)
        {
            this.mServer.Start(10004);

            this.mTimer.Tick += this.OnTimerRaised;
            this.mTimer.Interval = new TimeSpan(0, 0, 5);
            this.mTimer.Start();
        }

        /// <summary>
        /// Handles the Click event of the Shutdown control.
        /// </summary>
        /// <param name="pSender">The event sender.</param>
        /// <param name="pEventArgs">The event arguments.</param>
        private void Shutdown_Click(object pSender, System.Windows.RoutedEventArgs pEventArgs)
        {
            this.mServer.Shutdown();
            this.ClientView.Clear();
        }

        /// <summary>
        /// Handles the Click event of the Join control.
        /// </summary>
        /// <param name="pSender">The event sender.</param>
        /// <param name="pEventArgs">The event arguments.</param>
        private void Join_Click(object pSender, System.Windows.RoutedEventArgs pEventArgs)
        {
            this.mClientIndex++;
            Client lClient = new Client();
            lClient.Start("127.0.0.1", 10004, this.mClientIndex.ToString());
            this.mClients.Add(lClient);
        }

        /// <summary>
        /// Handles the Click event of the Leave control.
        /// </summary>
        /// <param name="pSender">The event sender.</param>
        /// <param name="pEventArgs">The event arguments.</param>
        private void Leave_Click(object pSender, System.Windows.RoutedEventArgs pEventArgs)
        {
            foreach (AHierarchicalItemViewModel lChild in this.Clients.SelectedViewModels)
            {
                ClientViewModel lClientViewModel = lChild as ClientViewModel;
                Client lClient = this.mClients.FirstOrDefault(pClient => pClient.Id == lClientViewModel.OwnedObject.Id);
                if (lClient != null)
                {
                    lClient.Shutdown();
                    this.mClients.Remove(lClient);
                }
            }
        }

        /// <summary>
        /// Handles the OnClick event of the Reconnect control.
        /// </summary>
        /// <param name="pEventSender">The source of the event.</param>
        /// <param name="pEventArgs">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Reconnect_OnClick(object pEventSender, RoutedEventArgs pEventArgs)
        {
            foreach (AHierarchicalItemViewModel lChild in this.Clients.SelectedViewModels)
            {
                ClientViewModel lClientViewModel = lChild as ClientViewModel;
                Client lClient = new Client();
                lClient.Start("127.0.0.1", 10004, lClientViewModel.Id);
                this.mClients.Add(lClient);
            }
        }

        /// <summary>
        /// This method is called when the window is closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            this.mServer.Shutdown();
        }

        /// <summary>
        /// Called when [client selection changed].
        /// </summary>
        /// <param name="pEventSender">The psender.</param>
        /// <param name="pEventArgs">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void OnClientSelectionChanged(object pEventSender, SelectionChangedEventArgs pEventArgs)
        {
            this.RefreshClientButtonStates();
        }

        /// <summary>
        /// Refreshes the client button states.
        /// </summary>
        private void RefreshClientButtonStates()
        {
            this.Leave.IsEnabled = false;
            this.Reconnect.IsEnabled = false;
            if (this.Clients.SelectedViewModels.Any())
            {
                foreach (AHierarchicalItemViewModel lChild in this.Clients.SelectedViewModels)
                {
                    ClientViewModel lClientViewModel = lChild as ClientViewModel;
                    if (lClientViewModel.IsSelected && lClientViewModel.OwnedObject.Status == Status.Declared)
                    {
                        this.Leave.IsEnabled = true;
                    }
                    else if (lClientViewModel.IsSelected && lClientViewModel.OwnedObject.Status == Status.Lost)
                    {
                        this.Reconnect.IsEnabled = true;
                    }
                }
            }
        }
    }
}
