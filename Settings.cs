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
        public string SelectedModel
        {
            get
            {
                return (string)this[nameof(SelectedModel)];
            }
            set
            {
               this[nameof(SelectedModel)] = value;
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
