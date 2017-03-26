using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using Windows.UI.Core;

namespace TEngine
{
    public abstract class ProfileManager
    {
        protected ProfileFolder connected;
        protected List<IProfile> saved;
        protected ObservableCollection<ProfileFolder> folders;
        protected CoreDispatcher dispatcher;

        protected ProfileManager(CoreDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            connected = new ProfileFolder("Connected");
            saved = new List<IProfile>();
            folders = new ObservableCollection<ProfileFolder>();
            folders.Add(connected);
        }

        #region Serialize methods
        static string Serialize<T>(T value)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StringBuilder stringBuilder = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Indent = true,
                OmitXmlDeclaration = true,
            };

            using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings))
            {
                serializer.Serialize(xmlWriter, value);
            }
            return stringBuilder.ToString();
        }
        static T Deserialize<T>(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            T value;
            using (StringReader stringReader = new StringReader(xml))
            {
                object deserialized = serializer.Deserialize(stringReader);
                value = (T)deserialized;
            }

            return value;
        }
        #endregion

        protected virtual async void NewProfile(IProfile newProfile) {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (!saved.Contains(newProfile))
                {
                    if (!connected.Contains(newProfile))
                        connected.Add(newProfile);
                    else
                    {
                        IProfile existedProfile = connected.Find((profile) => profile.Equals(newProfile));
                        existedProfile.CopyInformation(newProfile);
                    }
                }
                else
                {
                    IProfile savedProfile = saved.Find((profile) => profile.Equals(newProfile));
                    if (!connected.Contains(newProfile))
                    {
                        savedProfile.CopyData(newProfile);
                        connected.Add(savedProfile);
                    }
                    else
                        savedProfile.CopyInformation(newProfile);
                }
            });
        }
        protected async void DeleteProfile(Guid e)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                IProfile targetProfile = connected.Find((profile) => profile.Guid == e);
                targetProfile.SetNetworkData(null);
                connected.Remove(targetProfile);
            });
        }

        public ProfileFolder ConnectedFolder
        {
            get
            {
                return connected;
            }
        }
        public ObservableCollection<ProfileFolder> Folders
        {
            get
            {
                return folders;
            }
        }
    }
}
