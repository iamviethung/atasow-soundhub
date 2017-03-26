using System;
using System.Collections.Generic;
using System.Text;

using HNet;
using HNet.Converters;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Windows.UI.Core;

namespace TEngine
{
    public enum ConnectionState
    {
        Disconnected,
        Connected,
        LackOfResponse
    }

    public interface IProfile
    {
        Guid Guid { get; }
        string Name { get; }
        ConnectionState State { get; }

        void SetNetworkData(object data);
        void CopyData(IProfile copy);
        void CopyInformation(IProfile copy);
    }

    public class TClientProfile : IProfile, INotifyPropertyChanged
    {
        #region Variables
        ConnectionState state;
        HServerInformation server;

        string name;
        string lastIP;
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        public Guid Guid { get; private set; }
        public string Name { get { return name; } private set { name = value; NotifyPropertyChanged("Name"); } }
        public ConnectionState State
        {
            get { return state; }
            internal set
            {
                state = value;
                NotifyPropertyChanged("State");
            }
        }
        public string LastIPAddress { get { return lastIP; } private set { lastIP = value; NotifyPropertyChanged("LastIPAddress"); } }
        public void SetNetworkData(object data)
        {
            HServerInformation server = (HServerInformation)data;

            this.server = server;
            if (server != null)
            {
                this.Guid = server.Guid;
                this.Name = server.Name;
                State = ConnectionState.Connected;
            }
            else
                State = ConnectionState.Disconnected;
        }

        public TClientProfile(HServerInformation server, string ip = "127.0.0.1")
        {
            LastIPAddress = ip;
            SetNetworkData(server);
        }
        public TClientProfile(Guid guid, string name, string ip = "127.0.0.1")
        {
            Guid = guid;
            Name = name;
            state = ConnectionState.Disconnected;
            LastIPAddress = ip;
        }

        public void CopyData(IProfile copy)
        {
            TClientProfile profile = (TClientProfile)copy;
            LastIPAddress = profile.LastIPAddress;
            SetNetworkData(profile.server);
        }
        public void CopyInformation(IProfile copy)
        {
            TClientProfile profile = (TClientProfile)copy;
            Name = profile.Name;
            LastIPAddress = profile.LastIPAddress;
        }

        public override bool Equals(object obj)
        {
            return ((IProfile)obj).Guid == Guid;
        }
        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }        
    }

    public class TServerProfile : IProfile, INotifyPropertyChanged
    {
        const int VOLUME_BASE = 9;

        #region Variables
        int volume;
        ConnectionState state;
        HClientInformation client;

        string name;

        CoreDispatcher dispatcher;
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Properties
        public Guid Guid { get; private set; }
        public string Name { get { return name; } private set { name = value; NotifyPropertyChanged("Name"); } }
        public int Volume
        {
            get { return volume; }
            set
            {
                volume = value;
                if (VolumeConverter != null)
                    VolumeConverter.Ratio = AmplifyingRatio;
                NotifyPropertyChanged("Volume");
                NotifyPropertyChanged("AmplifyingRatio");
                NotifyPropertyChanged("Gain");
            }
        }
        public float AmplifyingRatio
        {
            get
            {
                return (float)(Math.Pow(VOLUME_BASE, volume / 50.0) - 1) / (VOLUME_BASE - 1);
            }
        }
        public float Gain
        {
            get
            {
                return 10 * (float)Math.Log10(AmplifyingRatio);
            }
        }
        public ConnectionState State
        {
            get { return state; }
            internal set
            {
                state = value;
                NotifyPropertyChanged("State");
            }
        }
        public int LackOfResponseTime { get { return client.LackOfReponseTime; } }
        public float DataRate
        {
            get
            {
                if (State == ConnectionState.Connected)
                    return client.Rate;
                return 0;
            }
        }
        public ScalingConverter VolumeConverter { get; private set; }
        public BranchingConverter DataOutput { get; private set; }
        public void SetNetworkData(object data)
        {
            HClientInformation client = (HClientInformation)data;

            if (this.client != null)
            {
                this.client.RateUpdated -= RateUpdatedHandler;
                this.client.SilentReceived -= SilentSignal;
                this.client.SetConverter(null);
            }
            
            this.client = client;
            if (client != null)
            {
                this.Guid = client.Guid;
                this.Name = client.Name;
                State = ConnectionState.Connected;
                client.RateUpdated += RateUpdatedHandler;
                client.SilentReceived += SilentSignal;
                client.SetConverter(new PCMToFloatConverter(VolumeConverter));
            }
            else
                State = ConnectionState.Disconnected;
        }
        #endregion

        public TServerProfile(HClientInformation client, Mixer mixer)
        {
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            DataOutput = new BranchingConverter(mixer);
            VolumeConverter = new ScalingConverter(DataOutput, 0.5f);
            Volume = 50;

            SetNetworkData(client);
        }
        public TServerProfile(Guid guid, string name, Mixer mixer) : this(null, mixer)
        {
            this.Guid = guid;
            this.Name = name;
        }
        public void CopyData(IProfile copy)
        {
            TServerProfile profile = (TServerProfile)copy;

            DataOutput = profile.DataOutput;
            VolumeConverter = profile.VolumeConverter;
            Volume = Volume;

            SetNetworkData(profile.client);
        }
        public void CopyInformation(IProfile copy)
        {
            TServerProfile profile = (TServerProfile)copy;
            Name = profile.Name;
        }

        public override bool Equals(object obj)
        {
            return ((IProfile)obj).Guid == Guid;
        }
        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
        public async void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PropertyChanged(this,
                        new PropertyChangedEventArgs(propertyName));
                });
            }
        }

        public void AttachOutput(FloatConverter converter)
        {
            DataOutput.Branches.Add(converter);
        }
        public void DetachOutput()
        {
            DataOutput.Branches.Clear();
        }

        public void RateUpdatedHandler(object sender, EventArgs e) 
        {
            if (client.LackOfResponse)
                State = ConnectionState.LackOfResponse;
            else
                State = ConnectionState.Connected;
            NotifyPropertyChanged("DataRate");
        }
        public void SilentSignal(object sender, EventArgs e)
        {
            foreach (FloatConverter converter in DataOutput.Branches)
                converter.Write(new float[0]);
        }
    }

    public class ProfileFolder : ObservableCollection<IProfile>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        string name;
        bool selected;

        public string Name { get { return name; } private set { name = value; NotifyPropertyChanged("Name"); } }
        public bool Selected { get { return selected; } set { selected = value; NotifyPropertyChanged("Selected"); } }

        public ProfileFolder(string name, ObservableCollection<IProfile> content)
            : base(content)
        {
            Name = name;
        }
        public ProfileFolder(string name) : base()
        {
            Name = name;
        }
        
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }  

        public IProfile Find(Func<IProfile, bool> predicate) {
            foreach (IProfile profile in this)
                if (predicate(profile))
                    return profile;
            return null;
        }
    }
}
