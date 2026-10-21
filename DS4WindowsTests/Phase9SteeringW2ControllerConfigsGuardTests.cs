using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 Steering 削除 W2（Phase9-SteeringWheel-Removal-Plan.md §3.2）: <c>ControllerConfigs.xml</c> の読み書きから、車輪の較正値（<c>wheelCenterPoint</c>／<c>wheel90DegPointLeft</c>／<c>wheel90DegPointRight</c>）だけが
    /// 除かれ、コントローラーごとのオプション（<c>DS4Device.optionsStore</c>）と、Mac・ControllerType の属性の読み書きは残っていること（ソースの走査。<c>DS4Device</c> は実機が要るため、挙動のテストは作れない）。
    /// </summary>
    public class Phase9SteeringW2ControllerConfigsGuardTests
    {
        private const string SerializerPath = "DS4Windows/DS4Control/Services/ControllerConfigsXmlSerializer.cs";
        private readonly ITestOutputHelper _output;

        public Phase9SteeringW2ControllerConfigsGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadSerializer()
        {
            string path = SourceFileLocator.Find(SerializerPath);
            if (path == null)
            {
                _output.WriteLine(SerializerPath + " が見つからないため検査を省略した。");
                return null;
            }

            return File.ReadAllText(path);
        }

        [Fact]
        public void TheOptionsAndTheAttributes_StillAreReadAndWritten()
        {
            string text = ReadSerializer();
            if (text == null) return;

            Assert.Contains("device.optionsStore?.LoadSettings(xmlDoc, node)", text);
            Assert.Contains("device.optionsStore?.PersistSettings(xmlDoc, node)", text);
            Assert.Contains("CreateAttribute(\"Mac\")", text);
            Assert.Contains("CreateAttribute(\"ControllerType\")", text);
        }

        [Fact]
        public void TheWheelCalibrationValues_AreNoLongerReadOrWritten()
        {
            string text = ReadSerializer();
            if (text == null) return;

            // 説明のコメントには要素名が出る。コード（コメントを除く）に出ないことを調べる
            string code = System.Text.RegularExpressions.Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            Assert.DoesNotContain("wheelCenterPoint", code);
            Assert.DoesNotContain("wheel90DegPoint", code);
        }
    }
}
