namespace Client
{
    public class Client : ClientSync
    {
        public string Username { get => GetPar<string>(); set => SetPar(value); }
        public Client(int id, bool isMe) : base(id, isMe)
        {
        }

        public override string ToString()
        {
            return ID.ToString("##") + " " + Username;
        }
    }
}


