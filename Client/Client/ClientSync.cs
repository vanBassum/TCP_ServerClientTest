using System.Collections.Generic;
using System.ComponentModel;
using MasterLibrary.Misc;

namespace Client
{
    public class ClientSync : PropertySensitive
    {
        //Keep a list of changed parameters, so we can send them periodically
        private Dictionary<string, object> changedPars = new Dictionary<string, object>();
        private object SendParLock = new object();
        public int ID { get; }

        public ClientSync(int id, bool isMe)
        {
            ID = id;

            if (isMe)
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
