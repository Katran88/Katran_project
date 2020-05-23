using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Katran.Models
{
    [Serializable]
    public class Settings
    {
        public const string SettingsFileName = "Settings.xml";

        public enum Culture { EN, RU }

        public enum Theme { Light, Dark }

        public Culture CurrentCulture;
        public Theme CurrentTheme;

        public Settings()
        {
            CurrentCulture = Culture.EN;
            CurrentTheme = Theme.Dark;

            ApplySettings();
        }

        public void ApplySettings()
        {
            App.Theme = CurrentTheme;
            App.Language = new CultureInfo(CurrentCulture.ToString());
        }

        public static void SaveSettings(Settings settings)
        {
            XmlSerializer formatter = new XmlSerializer(typeof(Settings));
            File.Delete(Settings.SettingsFileName);
            using (FileStream fs = new FileStream(Settings.SettingsFileName, FileMode.OpenOrCreate))
            {
                fs.Flush();
                formatter.Serialize(fs, settings);
            }

        }
    }
}
