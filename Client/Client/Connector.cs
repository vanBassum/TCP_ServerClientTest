using System;
using System.Collections.Generic;
using MasterLibrary.Ethernet;
using MasterLibrary.Ethernet.Frames;
using MasterLibrary.Misc;
using MasterLibrary.Datasave.Serializers;
using System.Reflection;

namespace Client
{
    public class Connector
    {
        public Client MyClient { get; set; }
        public ThreadedBindingList<Client> OtherClients { get; private set; }
        public event Action<Client, object> ObjectRecieved;

        private JSONIgnore serializer = new JSONIgnore();
        TcpSocketClientEscaped socket = new TcpSocketClientEscaped();

        public Connector()
        {
            OtherClients = new ThreadedBindingList<Client>();
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
                    MyClient = new Client(frame.SenderID, true);
                    break;
                case SendClientJoined frame:
                    OtherClients.Add(new Client(frame.SenderID, false));
                    break;
                case SendClientList frame:
                    foreach(int id in frame.Clients)
                    {
                        if(!OtherClients.Exists(cl=>cl.ID == id))
                            OtherClients.Add(new Client(id, false));
                    }
                    
                    break;
                case SendClientLeft frame:
                    OtherClients.RemoveWhere(c => c.ID == frame.SenderID);
                    break;
                case SendParameterUpdate frame:
                    RecievedUpdates(frame);
                    break;
                case SendObject frame:
                    int ind = OtherClients.FindIndex(c => c.ID == frame.SenderID);
                    if(ind != -1)
                        ObjectRecieved?.Invoke(OtherClients[ind], serializer.Deserialize(frame.serializedObject));
                    break;
                default:
                    throw new NotImplementedException();
                    break;

            }

        }

        private void RecievedUpdates(SendParameterUpdate ud)
        {

            int ind = OtherClients.FindIndex(c => c.ID == ud.SenderID);
            if (ind == -1)
            {
                //Add client or not???
            }
            else
            {
                foreach (KeyValuePair<string, object> par in ud.Parameters)
                {
                    PropertyInfo pi = typeof(Client).GetProperty(par.Key);
                    pi.SetValue(OtherClients[ind], par.Value);
                }
            }
            
        }

        //Call periodically to send changed parameters to the others
        public void SendUpdates()
        {
            if(MyClient != null)
                SendFrame(new SendParameterUpdate(MyClient.ID, MyClient.GetChangedPars()));
        }


        private void SendFrame(IFrame dataFrame)
        {
            socket.SendPackage(serializer.Serialize(dataFrame));

        }

        public void SendObject(object obj)
        {
            SendFrame(new SendObject(MyClient.ID, serializer.Serialize(obj)));

        }
    }

}
