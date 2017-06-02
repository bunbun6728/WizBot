﻿using WizBot.Services.Database.Models;

namespace WizBot.Services.Permissions
{
    public class OldPermissionCache
    {
        public string PermRole { get; set; }
        public bool Verbose { get; set; } = true;
        public Permission RootPermission { get; set; }
    }

    public class PermissionCache
    {
        public string PermRole { get; set; }
        public bool Verbose { get; set; } = true;
        public PermissionsCollection<Permissionv2> Permissions { get; set; }
    }
}
