using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using MasterLibrary.Ethernet.Frames;
using MasterLibrary.Misc;
using System.Collections.Generic;
using System.ComponentModel;
using MasterLibrary.Datasave.Serializers;
using MasterLibrary.Ethernet;

namespace Client
{
    public partial class Form1 : Form
    {

        MyClient client = new MyClient();
        public Form1()
        {
            InitializeComponent();
            listBox1.DataSource = client.otherClients;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            client.Connect();

        }

        private void Button1_Click(object sender, EventArgs e)
        {
            client.SendObject("meh");
        }
    }





    public class MyClient : OthClient
    {
        public ThreadedBindingList<OthClient> otherClients { get; private set; }

        private JSONIgnore serializer = new JSONIgnore();
        TcpSocketClientEscaped socket = new TcpSocketClientEscaped();
        private Timer sendChangesTimer = new Timer();

        public MyClient()
        {
            sendChangesTimer.Interval = 500;
            sendChangesTimer.Tick += SendChangesTimer_Tick;
        }


        public void Connect()
        {
            socket.BeginConnect("127.0.0.1", 1000, 1000);
            socket.SetTcpKeepAlive(true, 1000, 100);
            socket.OnPackageRecieved += Socket_OnPackageRecieved;
        }

        private void Socket_OnPackageRecieved(object sender, byte[] data)
        {
            IFrame f = serializer.Deserialize<IFrame>(data);

            switch (f)
            {
                case SendId frame:
                    //Yaay we got an ID from the server.
                    this.ID = frame.SenderID;
                    sendChangesTimer.Start();
                    break;

                case SendClientJoined frame:
                    //Someone joined the party.
                    otherClients.Add(new OthClient { ID = frame.ClientId });
                    break;

                case SendClientList frame:
                    foreach (int id in frame.Clients)
                    {
                        if (!otherClients.Exists(c => c.ID == id))
                            otherClients.Add(new OthClient { ID = id });
                    }
                    break;

                case SendClientLeft frame:
                    otherClients.RemoveWhere(c => c.ID == frame.SenderID);
                    break;

                case SendParameterUpdate frame:
                    RecievedUpdates(frame);
                    break;

                case SendObject frame:
                    int ind = otherClients.FindIndex(c => c.ID == frame.SenderID);
                    if (ind != -1)
                        otherClients[ind].InvokeObjectRecieved(serializer.Deserialize(frame.serializedObject));
                    break;

                default:
                    throw new NotImplementedException();
                    break;

            }
        }

        private void RecievedUpdates(SendParameterUpdate ud)
        {

            int ind = otherClients.FindIndex(c => c.ID == ud.SenderID);
            if (ind == -1)
            {
                //Add client or not???
            }
            else
            {
                foreach (KeyValuePair<string, object> par in ud.Parameters)
                {

                    otherClients[ind].GetType().GetProperty(par.Key).SetValue(otherClients[ind], par.Value);
                }
            }

        }

        private void SendChangesTimer_Tick(object sender, EventArgs e)
        {
            SendFrame(new SendParameterUpdate(ID, GetChangedPars()));
        }

        private void SendFrame(IFrame dataFrame)
        {
            socket.SendPackage(serializer.Serialize(dataFrame));
        }

        public void SendObject(object obj)
        {
            SendFrame(new SendObject(ID, serializer.Serialize(obj)));
        }
    }

    public class OthClient : Sync
    {
        public event Action<object> ObjectRecieved;
        public int ID { get; set; }
        public string Username { get => GetPar<string>(); set => SetPar(value); }


        public void InvokeObjectRecieved(object o)
        {
            ObjectRecieved?.Invoke(o);
        }

        public override string ToString()
        {
            return ID.ToString("##") + " " + Username;
        }
    }



    public class Sync : PropertySensitive
    {
        //Keep a list of changed parameters, so we can send them periodically
        private Dictionary<string, object> changedPars = new Dictionary<string, object>();
        private object SendParLock = new object();

        public Sync()
        {
            if (this.GetType() == typeof(MyClient))
                this.PropertyChanged += Client_PropertyChanged;
        }

        private void Client_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            changedPars[e.PropertyName] = this.GetType().GetProperty(e.PropertyName).GetValue(this);
        }

        public Dictionary<string, object> GetChangedPars()
        {
            Dictionary<string, object> cpy = new Dictionary<string, object>();
            lock (SendParLock)
            {
                foreach (KeyValuePair<string, object> kvp in changedPars)
                    cpy[kvp.Key] = kvp.Value;
                changedPars.Clear();
            }
            return cpy;
        }
    }

}
