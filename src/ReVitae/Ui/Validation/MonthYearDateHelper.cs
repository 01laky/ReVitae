using System;
using Avalonia.Controls;
using ReVitae.Core.Cv;

namespace ReVitae.Ui.Validation;

public static class MonthYearDateHelper
{
    public static DateTimeOffset? ToSelectedDate(int? month, int? year) =>
        MonthYearSelection.ToDateTimeOffset(month, year);

    public static (int? Month, int? Year) FromSelectedDate(DateTimeOffset? selectedDate) =>
        MonthYearSelection.FromDateTimeOffset(selectedDate);

    public static DatePicker CreatePicker(EventHandler<DatePickerSelectedValueChangedEventArgs>? onChanged = null)
    {
        var picker = new DatePicker();
        if (onChanged is not null)
        {
            picker.SelectedDateChanged += onChanged;
        }

        return picker;
    }
}
