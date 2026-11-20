using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary><see cref="IEditorLayoutSettings"/> を、設定のサービス（<see cref="IAppSettingsService"/>）へ委譲して実装する。</summary>
    public sealed class EditorLayoutSettingsAdapter : IEditorLayoutSettings
    {
        private readonly IAppSettingsService _settings;

        public EditorLayoutSettingsAdapter(IAppSettingsService settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            _settings = settings;
        }

        public string UseLang => _settings.UseLang;

        public int ProfileEditorLeftWidth { get => _settings.ProfileEditorLeftWidth; set => _settings.ProfileEditorLeftWidth = value; }
        public int ProfileEditorRightWidth { get => _settings.ProfileEditorRightWidth; set => _settings.ProfileEditorRightWidth = value; }
        public int SpecialActionNameColWidth { get => _settings.SpecialActionNameColWidth; set => _settings.SpecialActionNameColWidth = value; }
        public int SpecialActionTriggerColWidth { get => _settings.SpecialActionTriggerColWidth; set => _settings.SpecialActionTriggerColWidth = value; }
        public int SpecialActionDetailColWidth { get => _settings.SpecialActionDetailColWidth; set => _settings.SpecialActionDetailColWidth = value; }

        public int FormWidth { get => _settings.FormWidth; set => _settings.FormWidth = value; }
        public int FormHeight { get => _settings.FormHeight; set => _settings.FormHeight = value; }
    }
}
