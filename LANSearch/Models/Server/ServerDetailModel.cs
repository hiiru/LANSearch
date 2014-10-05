using System.Linq;
using LANSearch.Data;
using System;
using System.Collections.Generic;

namespace LANSearch.Models.Server
{
    public class ServerDetailModel
    {
        public ServerDetailModel()
        {
            Errors = new Dictionary<string, string>();
        }

        public Data.Server.Server Server { get; set; }

        public string OwnerName { get; set; }

        public string OwnerAdminUrl { get; set; }

        public bool IsCreation { get; set; }

        public bool IsAdmin { get; set; }

        public Dictionary<string, string> Errors { get; set; }

        public bool ValidateServer()
        {
            if (Server == null)
                throw new InvalidOperationException("Server is null");

            if (Server.Name != null && Server.Name.Length > 20)
                Errors["srvName"] = "Name is too long, only up to 20 is allowed.";
            if (Server.Description != null && Server.Description.Length > 5000)
                Errors["srvDescription"] = "Description is too long, only up to 5000 is allowed.";
            if (!ValidationHelper.IsValidIP(Server.Address))
            {
                Errors["srvAddress"] = "Invalid Server Address.";
            }
            else if (!IsAdmin && !AppContext.GetContext().Config.AppAllowedServerIps.Any(x => x.IsInRange(Server.Address)))
            {
                Errors["srvAddress"] = string.Format("Server is in an forbidden IP-Range, allowed ranges are {0}", string.Join(", ", AppContext.GetContext().Config.AppAllowedServerIps.Select(x => x.ToString())));
            }
            if (Server.Port < 1 || Server.Port > 65535)
            {
                Errors["srvPort"] = "Invalid Server Port.";
            }
            if (Server.OwnerId > 0)
            {
                var user = AppContext.GetContext().UserManager.Get(Server.OwnerId);
                if (user == null)
                    Errors["srvOwner"] = "Invalid ownerId.";
            }
            return Errors.Count == 0;
        }
    }
}