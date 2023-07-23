using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelationshipsStudio
{
    public class MyUserSettings : ApplicationSettingsBase
    {
        [UserScopedSetting()]
        [DefaultSettingValue("")]
        public string BimFilename
        {
            get
            {
                return (string)this[nameof(BimFilename)];
            }
            set
            {
                this[nameof(BimFilename)] = value;
            }
        }

        [UserScopedSetting()]
        [DefaultSettingValue("")]
        public string Relationships
        {
            get
            {
                return (string)this[nameof(Relationships)];
            }
            set
            {
                this[nameof(Relationships)] = value;
            }
        }
    }
}
