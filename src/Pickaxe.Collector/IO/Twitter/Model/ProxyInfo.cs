using System;

namespace Pickaxe.Collector.IO.Twitter.Model
{
    public class ProxyInfo
    {
        public string Address { get; private set; }
        public int Port { get; private set; }
        public string FullAddress
        {
            get
            {
                return string.Format("{0}:{1}", this.Address, this.Port);
            }
        }
        public string User { get; private set; }
        public string Password { get; private set; }

        public ProxyInfo(string connectionString)
        {
            try
            {
                var values = connectionString.Split('@');

                var accountValues = values[0].Split(':');
                if (accountValues.Length == 2)
                {
                    this.User = accountValues[0];
                    this.Password = accountValues[1];
                }

                var addressValues = values[1].Split(':');
                if (addressValues.Length == 2)
                {
                    this.Address = addressValues[0];
                    this.Port = int.Parse(addressValues[1]);
                }
            }
            catch (FormatException ex)
            {
                throw new Exception("Proxy format invalid.");
            }
        }
    }
}
