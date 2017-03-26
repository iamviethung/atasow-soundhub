using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Core;

namespace TEngine
{
    public class TClientProfileManager : ProfileManager
    {
        ProfileFolder available;

        public TClientProfileManager(TClient client, CoreDispatcher dispatcher) : base(dispatcher)
        {
            available = new ProfileFolder("Available");
            folders.Insert(0, available);

            client.NewProfile += client_NewProfile;
            client.DeleteProfile += client_DeleteProfile;
            client.NewAvailableProfile += client_NewAvailableProfile;
            client.ScanningStarted += async (o, e) =>
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    available.Clear();
                });
            };
        }

        #region Special folders functions
        void client_NewProfile(object sender, TClientProfile e)
        {
            NewProfile(e);
        }
        void client_DeleteProfile(object sender, Guid e)
        {
            DeleteProfile(e);
        }
        async void client_NewAvailableProfile(object sender, TClientProfile newProfile)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (available.Contains(newProfile))
                    return;

                TClientProfile existedProfile = (TClientProfile)saved.Find((profile) => profile.Equals(newProfile));
                if (existedProfile == null)
                    existedProfile = (TClientProfile)connected.Find((profile) => profile.Equals(newProfile));

                if (existedProfile != null)
                {
                    existedProfile.CopyInformation(newProfile);
                    available.Add(existedProfile);
                }
                else
                    available.Add(newProfile);
            });
        }
        protected override async void NewProfile(IProfile newProfile)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (connected.Contains(newProfile))
                    return;

                IProfile existedProfile = saved.Find((profile) => profile.Equals(newProfile));
                if (existedProfile == null)
                    existedProfile = available.Find((profile) => profile.Equals(newProfile));

                if (existedProfile != null)
                {
                    existedProfile.CopyData(newProfile);
                    connected.Add(existedProfile);
                }
                else
                    connected.Add(newProfile);
            });
        }
        #endregion

        public ProfileFolder AvailableFolder
        {
            get
            {
                return available;
            }
        }
    }
}
