using System;
using Avalonia.Controls;

namespace ReVitae.Ui.Validation;

public static class MonthYearDateHelper
{
    public static DateTimeOffset? ToSelectedDate(int? month, int? year)
    {
        if (!month.HasValue || !year.HasValue)
        {
            return null;
        }

        if (month is < 1 or > 12)
        {
            return null;
        }

        return new DateTimeOffset(year.Value, month.Value, 1, 0, 0, 0, TimeSpan.Zero);
    }

    public static (int? Month, int? Year) FromSelectedDate(DateTimeOffset? selectedDate)
    {
        if (!selectedDate.HasValue)
        {
            return (null, null);
        }

        return (selectedDate.Value.Month, selectedDate.Value.Year);
    }

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
