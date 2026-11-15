using System;
using System.Linq;
using DS4Windows.Input;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 第 3 層（<c>DS4Windows.Output</c>）が入力機器の具象（<see cref="InputDeviceCore"/>）を参照せず、
    /// 用途ごとの狭い契約（<c>IRumbleTarget</c>・<c>IOutputBoundDevice</c>・<c>IMotionSource</c>・<c>IPadStatusSource</c>）だけを使うこと。
    /// </summary>
    public class OutputInputDeviceBoundaryTests
    {
        [Fact]
        public void OutputLayer_DoesNotReferenceInputDeviceCore()
        {
            string target = typeof(InputDeviceCore).FullName;
            var offenders = LayerDependencyScanner.Edges
                .Where(e => e.SourceLayer == ArchLayer.Output && e.Target == target)
                .Select(e => e.Source)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();

            Assert.True(offenders.Count == 0, "Output が InputDeviceCore を参照している: " + string.Join(", ", offenders));
        }

        [Fact]
        public void InputDeviceCore_ImplementsTheContractsTheOutputLayerUses()
        {
            Assert.True(typeof(DS4Windows.Core.Abstractions.IRumbleTarget).IsAssignableFrom(typeof(InputDeviceCore)));
            Assert.True(typeof(DS4Windows.Core.Abstractions.IOutputBoundDevice).IsAssignableFrom(typeof(InputDeviceCore)));
            Assert.True(typeof(DS4Windows.Core.Abstractions.IMotionSource).IsAssignableFrom(typeof(InputDeviceCore)));
            Assert.True(typeof(DS4Windows.Core.Abstractions.IPadStatusSource).IsAssignableFrom(typeof(InputDeviceCore)));
        }
    }
}
