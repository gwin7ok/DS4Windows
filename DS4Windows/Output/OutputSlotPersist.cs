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

using System;
using System.IO;
using System.Xml.Serialization;
using DS4Windows.Core.Persistence.Dto;

namespace DS4Windows.Output
{
    public static class OutputSlotPersist
    {
        private const string CONFIG_FILENAME = "OutputSlots.xml";

        public static bool ReadConfig(OutputSlotManager slotManager, string configDirectory)
        {
            bool result = false;

            string output_path = Path.Combine(configDirectory, CONFIG_FILENAME);
            if (File.Exists(output_path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(OutputSlotPersistDTO));
                using StreamReader sr = new StreamReader(output_path);
                try
                {
                    OutputSlotPersistDTO? dto = serializer.Deserialize(sr) as OutputSlotPersistDTO;
                    dto!.MapTo(slotManager); // 読み込んだ設定（null なら従来どおり NullReferenceException）
                    result = true;
                }
                catch (InvalidOperationException)
                {
                    result = false;
                }
            }

            return result;
        }

        public static bool WriteConfig(OutputSlotManager slotManager, string configDirectory)
        {
            bool result = false;

            string output_path = Path.Combine(configDirectory, CONFIG_FILENAME);
            string testStr = string.Empty;
            XmlSerializer serializer = new XmlSerializer(typeof(OutputSlotPersistDTO));
            OutputSlotPersistDTO dto = new OutputSlotPersistDTO();
            dto.MapFrom(slotManager);
            testStr = XmlDtoText.Serialize(serializer, dto, string.Format(" Made with DS4Windows version {0} ", DS4Windows.Core.Platform.PathService.ExecutableVersionState), writeStartDocument: true);

            try
            {
                using (StreamWriter sw = new StreamWriter(output_path, false))
                {
                    sw.Write(testStr);
                }
                result = true;
            }
            catch (UnauthorizedAccessException)
            {
                result = false;
            }

            return result;
        }
    }
}