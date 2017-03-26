using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Core;

namespace TEngine
{
    public class TServerProfileManager : ProfileManager
    {
        public TServerProfileManager(TServer server, CoreDispatcher dispatcher)
            : base(dispatcher)
        {
            server.NewProfile += server_NewProfile;
            server.DeleteProfile += server_DeleteProfile;
        }

        #region Special folders functions
        void server_NewProfile(object sender, TServerProfile e)
        {
            NewProfile(e);
        }
        void server_DeleteProfile(object sender, Guid e)
        {
            DeleteProfile(e);
        }
        #endregion
    }
}
