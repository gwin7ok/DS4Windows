/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.IO;
using System.Text;
using System.Xml;

namespace DS4Windows.Core.Persistence
{
    public partial class ProfileMigration
    {
        private XmlReader? profileReader;
        public XmlReader ProfileReader { get => profileReader!; }   // 読み取りを終えて破棄するまでは、コンストラクターが必ず作る

        private int configFileVersion;
        private bool usedMigration;
        public bool UsedMigration { get => usedMigration; }
        private string currentMigrationText = string.Empty;
        public string CurrentMigrationText { get => currentMigrationText; }

        public ProfileMigration(Stream inputStream)
        {
            //FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            SetupFromStream(inputStream);
            inputStream.Close();
        }

        public ProfileMigration(string profileText)
        {
            using (MemoryStream tempMemStream =
                new MemoryStream(Encoding.UTF8.GetBytes(profileText ?? "")))
            {
                SetupFromStream(tempMemStream);
            }
        }

        private void SetupFromStream(Stream inputStream)
        {
            StreamReader innerStreamReader = new StreamReader(inputStream);
            currentMigrationText = innerStreamReader.ReadToEnd();
            innerStreamReader.Dispose();

            bool profileRead = false;
            try
            {
                profileReader = XmlReader.Create(new StringReader(currentMigrationText));
                // Move stream to root element
                ProfileReader.MoveToContent();
                string? temp = ProfileReader.GetAttribute("config_version");
                if (!string.IsNullOrEmpty(temp))
                {
                    int.TryParse(temp, out configFileVersion);
                }

                profileRead = true;
            }
            catch (XmlException) { /* 欠けた・壊れた値は、既定値のまま続ける */ }

            // config_version not available in file. Assume either version 1 or 2.
            // Try to determine which version
            if (profileRead && configFileVersion == 0)
            {
                DetermineProfileVersion();
            }
        }

        public bool RequiresMigration()
        {
            bool result = false;
            if (configFileVersion >= 1 && configFileVersion < DS4Windows.Core.Persistence.LegacyProfileXmlSerializer.ConfigVersion)
            {
                result = true;
            }

            return result;
        }

        public void Migrate()
        {
            if (RequiresMigration())
            {
                string migratedText;
                int tempVersion = configFileVersion;
                switch(configFileVersion)
                {
                    case 1:
                        migratedText = Version0002Migration();
                        currentMigrationText = migratedText;
                        PrepareReaderMigration(migratedText);
                        tempVersion = 2;
                        goto case 2;
                    case 2:
                    case 3:
                        migratedText = Version0004Migration();
                        currentMigrationText = migratedText;
                        PrepareReaderMigration(migratedText);
                        tempVersion = 4;
                        goto case 4;
                    case 4:
                        migratedText = Version0005Migration();
                        currentMigrationText = migratedText;
                        PrepareReaderMigration(migratedText);
                        tempVersion = 5;
                        goto default;

                    default:
                        break;
                }

                configFileVersion = tempVersion;
            }
        }

        private void PrepareReaderMigration(string migratedText)
        {
            usedMigration = true;
            // Close and flush current XmlReader instance
            ProfileReader.Close();
            ProfileReader.Dispose();

            currentMigrationText = migratedText;
            StringReader stringReader = new StringReader(currentMigrationText);
            profileReader = XmlReader.Create(stringReader);
            // Move stream to root element
            //ProfileReader.MoveToContent();
        }

        public void Close()
        {
            if (profileReader != null)
            {
                // Close and flush current XmlReader instance
                ProfileReader.Close();
                ProfileReader.Dispose();

                profileReader = null;
            }
        }

        private void DetermineProfileVersion()
        {
            bool hasAntiDeadLSTag = false;
            //int deadZoneLS = -1;

            // Move stream to root element
            ProfileReader.MoveToContent();
            // Skip past root element
            ProfileReader.Read();
            while (ProfileReader.Read())
            {
                /*if (ProfileReader.Name == "LSDeadZone" && ProfileReader.IsStartElement())
                {
                    string weight = ProfileReader.ReadElementContentAsString();
                    int.TryParse(weight, out deadZoneLS);
                }
                */
                if (ProfileReader.Name == "LSAntiDeadZone" && ProfileReader.IsStartElement())
                {
                    hasAntiDeadLSTag = true;
                    ProfileReader.ReadElementContentAsString();
                }
            }

            // Close and dispose current XmlReader
            ProfileReader.Close();
            ProfileReader.Dispose();

            if (hasAntiDeadLSTag)
            {
                configFileVersion = 2;
            }
            else
            {
                configFileVersion = 1;
            }

            // Start reader at zero position
            profileReader = XmlReader.Create(new StringReader(currentMigrationText));
            // Move stream to root element
            ProfileReader.MoveToContent();
        }
    }
}
