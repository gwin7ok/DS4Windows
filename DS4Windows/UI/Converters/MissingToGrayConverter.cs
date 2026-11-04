using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DS4WinWPF.DS4Forms.Converters
{
    // TODO(technical debt): 4.0.1 で、Special Actions の一覧（ProfileEditor.xaml）は DataTrigger に置き換えた。
    // この Converter は、ProfileEditor.xaml の StaticResource の登録（MissingToGrayConverter）にだけ残っている（黒を固定で返すため、ダークテーマでは使えない）。
    // 他に使う予定がなければ、登録とあわせて削除してよい（copilot-instructions 2.4）。
    public class MissingToGrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isMissing = value is bool b && b;
            return isMissing ? Brushes.Gray : Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
