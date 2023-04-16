using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.JsonAdaptor;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Demo_M_Files_Application
{
    [DataContract]
    public class Configuration
    {
        // const works like static variables!
        private const string defaultValue = "My Default Value";
        
        [DataMember]
        [TextEditor(DefaultValue = Configuration.defaultValue)]
        public string TextValueWithDefault { get; set; } = Configuration.defaultValue;

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        [Security(IsPassword = true)]
        public string Password { get; set; }

        // This member is not visible to vault administrators.
        [DataMember]
        [Security(ChangeBy = SecurityAttribute.UserLevel.SystemAdmin)]
        public string WebAddress { get; set; }
    }
}