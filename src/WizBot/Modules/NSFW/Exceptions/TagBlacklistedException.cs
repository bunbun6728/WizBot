using System;

namespace WizBot.Modules.NSFW.Exceptions
{
    public class TagBlacklistedException : Exception
    {
        public TagBlacklistedException() : base("Tag you used is blacklisted.")
        {

        }
    }
}