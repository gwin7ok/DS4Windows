using System.IO;
using System.Text;
using System.Xml;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence
{
    /// <summary>旧い版（0002・0004・0005）のプロファイルを、現在の形へ書き換える処理。</summary>
    public partial class ProfileMigration
    {
        struct MigrationSettings0004
        {
            public const double DEFAULT_SMOOTH_WEIGHT = 0.5;

            public bool hasGyroMouseSmoothing;
            public bool hasGyroMouseSmoothingWeight;

            public bool useGyroMouseSmoothing;
            public double gyroMouseSmoothingWeight;

            public bool hasGyroMouseStickSmoothing;
            public bool hasGyroMouseStickSmoothingWeight;

            public bool useGyroMouseStickSmoothing;
            public double gyroMouseStickSmoothingWeight;
        }

        private string Version0004Migration()
        {
            MigrationSettings0004 gyroSmoothSettings = new MigrationSettings0004()
            {
                gyroMouseSmoothingWeight = MigrationSettings0004.DEFAULT_SMOOTH_WEIGHT,
                gyroMouseStickSmoothingWeight = MigrationSettings0004.DEFAULT_SMOOTH_WEIGHT,
            };

            void MigrateGyroMouseSmoothingSettings(XmlWriter xmlWriter)
            {
                // <GyroMouseSmoothingSettings>
                xmlWriter.WriteStartElement("GyroMouseSmoothingSettings");

                xmlWriter.WriteStartElement("UseSmoothing");
                xmlWriter.WriteValue(gyroSmoothSettings.useGyroMouseSmoothing.ToString());
                xmlWriter.WriteEndElement();

                if (gyroSmoothSettings.useGyroMouseSmoothing)
                {
                    xmlWriter.WriteStartElement("SmoothingMethod");
                    xmlWriter.WriteValue("weighted-average");
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteStartElement("SmoothingWeight");
                xmlWriter.WriteValue(gyroSmoothSettings.gyroMouseSmoothingWeight.ToString());
                xmlWriter.WriteEndElement();

                // </GyroMouseSmoothingSettings>
                xmlWriter.WriteEndElement();
            }

            void MigrateGyroMouseStickSmoothingSettings(XmlWriter xmlWriter)
            {
                // <GyroMouseStickSmoothingSettings>
                xmlWriter.WriteStartElement("GyroMouseStickSmoothingSettings");

                xmlWriter.WriteStartElement("UseSmoothing");
                xmlWriter.WriteValue(gyroSmoothSettings.useGyroMouseStickSmoothing.ToString());
                xmlWriter.WriteEndElement();

                if (gyroSmoothSettings.useGyroMouseStickSmoothing)
                {
                    xmlWriter.WriteStartElement("SmoothingMethod");
                    xmlWriter.WriteValue("weighted-average");
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteStartElement("SmoothingWeight");
                xmlWriter.WriteValue(gyroSmoothSettings.gyroMouseStickSmoothingWeight.ToString());
                xmlWriter.WriteEndElement();

                // </GyroMouseStickSmoothingSettings>
                xmlWriter.WriteEndElement();
            }

            StringWriter stringWrite = new StringWriter();
            XmlWriter tempWriter = XmlWriter.Create(stringWrite, new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true,
            });
            tempWriter.WriteStartDocument();
            // Move stream to root element
            ProfileReader.MoveToContent();
            // Skip past root element
            ProfileReader.Read();
            ProfileReader.MoveToContent();

            // Write replacement root element in XmlWriter
            tempWriter.WriteStartElement("DS4Windows");
            tempWriter.WriteAttributeString("app_version", PathService.ExecutableVersionState);
            tempWriter.WriteAttributeString("config_version", "4");

            // First pass
            while (!ProfileReader.EOF)
            {
                bool readNext = true;
                if (ProfileReader.IsStartElement() && ProfileReader.Depth == 1)
                {
                    switch(ProfileReader.Name)
                    {
                        case "GyroSmoothing":
                        {
                            gyroSmoothSettings.hasGyroMouseSmoothing = true;
                            string useSmooth = ProfileReader.ReadElementContentAsString();
                            bool.TryParse(useSmooth, out gyroSmoothSettings.useGyroMouseSmoothing);
                            readNext = false;
                            break;
                        }
                        case "GyroSmoothingWeight":
                        {
                            gyroSmoothSettings.hasGyroMouseSmoothingWeight = true;
                            string weight = ProfileReader.ReadElementContentAsString();
                            double.TryParse(weight, out gyroSmoothSettings.gyroMouseSmoothingWeight);
                            readNext = false;
                            break;
                        }
                        case "GyroMouseStickSmoothing":
                        {
                            gyroSmoothSettings.hasGyroMouseStickSmoothing = true;
                            string useSmooth = ProfileReader.ReadElementContentAsString();
                            bool.TryParse(useSmooth, out gyroSmoothSettings.useGyroMouseStickSmoothing);
                            readNext = false;
                            break;
                        }
                        case "GyroMouseStickSmoothingWeight":
                        {
                            gyroSmoothSettings.hasGyroMouseStickSmoothingWeight = true;
                            string weight = ProfileReader.ReadElementContentAsString();
                            double.TryParse(weight, out gyroSmoothSettings.gyroMouseStickSmoothingWeight);
                            readNext = false;
                            break;
                        }
                        default:
                            break;
                    }
                }

                if (readNext)
                {
                    ProfileReader.Read();
                }
            }

            // Close and dispose current XmlReader
            ProfileReader.Close();
            ProfileReader.Dispose();

            // Prepare for second pass
            StringReader stringReader = new StringReader(currentMigrationText);
            profileReader = XmlReader.Create(stringReader);
            // Move stream to root element
            ProfileReader.MoveToContent();
            // Skip past root element
            ProfileReader.Read();
            ProfileReader.MoveToContent();

            // Second pass
            while (!ProfileReader.EOF)
            {
                if (ProfileReader.IsStartElement() && ProfileReader.Depth == 1)
                {
                    switch (ProfileReader.Name)
                    {
                        case "GyroSmoothing":
                        {
                            // Place new GyroMouseSmoothingSettings group where GyroSmoothing used to be
                            MigrateGyroMouseSmoothingSettings(tempWriter);
                            // Consume reset of element
                            ProfileReader.ReadElementContentAsString();
                            break;
                        }
                        case "GyroSmoothingWeight":
                        {
                            // Consume reset of element
                            ProfileReader.ReadElementContentAsString();
                            break;
                        }
                        case "GyroMouseStickSmoothing":
                        {
                            // Place new GyroMouseStickSmoothingSettings group where GyroSmoothing used to be
                            MigrateGyroMouseStickSmoothingSettings(tempWriter);
                            // Consume reset of element
                            ProfileReader.ReadElementContentAsString();
                            break;
                        }
                        case "GyroMouseStickSmoothingWeight":
                        {
                            // Consume reset of element
                            ProfileReader.ReadElementContentAsString();
                            break;
                        }
                        default:
                            tempWriter.WriteNode(ProfileReader, true);
                            break;
                    }
                }
                else
                {
                    ProfileReader.Read();
                }
            }

            // End XML document and flush IO stream
            tempWriter.WriteEndElement();
            tempWriter.WriteEndDocument();
            tempWriter.Close();
            return stringWrite.ToString();
        }

        private string Version0002Migration()
        {
            StringWriter stringWrite = new StringWriter();
            XmlWriter tempWriter = XmlWriter.Create(stringWrite, new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true,
            });
            tempWriter.WriteStartDocument();
            // Move stream to root element
            ProfileReader.MoveToContent();
            // Skip past root element
            ProfileReader.Read();
            ProfileReader.MoveToContent();

            // Write replacement root element in XmlWriter
            tempWriter.WriteStartElement("DS4Windows");
            tempWriter.WriteAttributeString("app_version", PathService.ExecutableVersionState);
            tempWriter.WriteAttributeString("config_version", "2");

            while (!ProfileReader.EOF)
            {
                if (ProfileReader.IsStartElement() && ProfileReader.Depth == 1)
                {
                    switch (ProfileReader.Name)
                    {
                        case "LSDeadZone":
                        {
                            string lsdead = ProfileReader.ReadElementContentAsString();
                            bool valid = int.TryParse(lsdead, out int temp);
                            if (valid)
                            {
                                // Add default deadzone if a 0 dead zone was set in profile.
                                // Jays2Kings used implicit dead zones in the mapper code
                                if (temp <= 0)
                                {
                                    temp = StickDeadZoneInfo.DEFAULT_DEADZONE;
                                }

                                tempWriter.WriteElementString("LSDeadZone", temp.ToString());
                            }

                            break;
                        }
                        case "RSDeadZone":
                        {
                            string rsdead = ProfileReader.ReadElementContentAsString();
                            bool valid = int.TryParse(rsdead, out int temp);
                            if (valid)
                            {
                                // Add default deadzone if a 0 dead zone was set in profile.
                                // Jays2Kings used implicit dead zones in the mapper code
                                if (temp <= 0)
                                {
                                    temp = StickDeadZoneInfo.DEFAULT_DEADZONE;
                                }

                                tempWriter.WriteElementString("RSDeadZone", temp.ToString());
                            }

                            break;
                        }

                        default:
                            tempWriter.WriteNode(ProfileReader, true);
                            break;
                    }
                }
                else
                {
                    ProfileReader.Read();
                }
             }

            // End XML document and flush IO stream
            tempWriter.WriteEndElement();
            tempWriter.WriteEndDocument();
            tempWriter.Close();
            return stringWrite.ToString();
        }

        private string Version0005Migration()
        {
            StringWriter stringWrite = new StringWriter();
            XmlWriter tempWriter = XmlWriter.Create(stringWrite, new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true,
            });
            tempWriter.WriteStartDocument();
            // Move stream to root element
            ProfileReader.MoveToContent();
            // Skip past root element
            ProfileReader.Read();
            ProfileReader.MoveToContent();

            // Write replacement root element in XmlWriter
            tempWriter.WriteStartElement("DS4Windows");
            tempWriter.WriteAttributeString("app_version", PathService.ExecutableVersionState);
            tempWriter.WriteAttributeString("config_version", "5");

            while (!ProfileReader.EOF)
            {
                if (ProfileReader.IsStartElement() && ProfileReader.Depth == 1)
                {
                    switch (ProfileReader.Name)
                    {
                        case "UseTPforControls":
                            {
                                string tpControls = ProfileReader.ReadElementContentAsString();
                                bool valid = bool.TryParse(tpControls, out bool temp);
                                if (valid && temp)
                                {
                                    tempWriter.WriteElementString("TouchpadOutputMode", TouchpadOutMode.Controls.ToString());
                                }

                                break;
                            }
                        default:
                            tempWriter.WriteNode(ProfileReader, true);
                            break;
                    }
                }
                else
                {
                    ProfileReader.Read();
                }
            }

            // End XML document and flush IO stream
            tempWriter.WriteEndElement();
            tempWriter.WriteEndDocument();
            tempWriter.Close();
            return stringWrite.ToString();
        }
    }
}
