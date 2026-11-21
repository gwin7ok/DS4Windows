using DS4Windows.Processing.Macro;
using System.Collections.Generic;
using System.IO;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>マクロの読み込み・書き出し・プリセットの保存。</summary>
    public partial class RecordBoxViewModel
    {
        public void LoadMacro()
        {
            int[] macro = (int[])settings.action.actionMacro;

            MacroParser macroParser = new MacroParser(macro, MacroOutType);
            macroParser.LoadMacro();
            foreach (MacroStep step in macroParser.MacroSteps)
            {
                MacroStepItem item = new MacroStepItem(step);
                macroSteps.Add(item);
            }
        }

        public void ExportMacro()
        {
            int[] outmac = new int[macroSteps.Count];
            int index = 0;
            foreach (MacroStepItem step in macroSteps)
            {
                outmac[index] = step.Step.Value;
                index++;
            }

            settings.action.actionMacro = outmac;
            settings.actionType = DS4ControlSettings.ActionType.Macro;
            // 「Repeat while held」は RepeatMacro だけで表す
            settings.keyType = playbackOptions.ApplyTo(DS4KeyType.Macro);
        }

        /// <summary>マクロのフォルダを初期位置にして、プリセットを読み込む。</summary>
        public bool PickAndLoadPreset() => PickAndLoadPreset(macrosFolder?.Invoke());

        /// <summary>マクロのフォルダを初期位置にして、プリセットを書き出す。</summary>
        public bool PickAndSavePreset() => PickAndSavePreset(macrosFolder?.Invoke());

        /// <summary>プリセットのファイルを選ばせ、選ばれたら今の手順を置き換えて読み込む。</summary>
        /// <returns>読み込んだら true。取り消しなら false</returns>
        public bool PickAndLoadPreset(string? initialDirectory)
        {
            string? file = dialogs.PickOpenFile(new FileDialogOptions
            {
                Title = "Select Preset File",
                Filter = "Text Documents (*.txt)|*.txt",
                DefaultExtension = ".txt",
                InitialDirectory = initialDirectory,
            });
            if (file == null) return false;

            macroSteps.Clear();
            LoadPresetFromFile(file);
            return true;
        }

        /// <summary>保存先を選ばせ、選ばれたら今の手順を書き出す。</summary>
        /// <returns>保存したら true。取り消しなら false</returns>
        public bool PickAndSavePreset(string? initialDirectory)
        {
            string? file = dialogs.PickSaveFile(new FileDialogOptions
            {
                Title = "Select Export File",
                Filter = "Text Documents (*.txt)|*.txt",
                DefaultExtension = ".txt",
                InitialDirectory = initialDirectory,
                CheckFileExists = false,
            });
            if (file == null) return false;

            SavePreset(file);
            return true;
        }

        public void LoadPresetFromFile(string filepath)
        {
            string[] macs = File.ReadAllText(filepath).Split('/');
            List<int> tmpmacro = new List<int>();
            int temp;
            foreach (string s in macs)
            {
                if (int.TryParse(s, out temp))
                    tmpmacro.Add(temp);
            }

            MacroParser macroParser = new MacroParser(tmpmacro.ToArray(), MacroOutType);
            macroParser.LoadMacro();
            foreach (MacroStep step in macroParser.MacroSteps)
            {
                MacroStepItem item = new MacroStepItem(step);
                macroSteps.Add(item);
            }
        }

        public void SavePreset(string filepath)
        {
            int[] outmac = new int[macroSteps.Count];
            int index = 0;
            foreach (MacroStepItem step in macroSteps)
            {
                outmac[index] = step.Step.Value;
                index++;
            }

            string macro = string.Join("/", outmac);
            StreamWriter sw = new StreamWriter(filepath);
            sw.Write(macro);
            sw.Close();
        }
    }
}
