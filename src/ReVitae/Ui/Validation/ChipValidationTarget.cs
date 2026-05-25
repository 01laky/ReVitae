using System;
using System.Collections.Generic;
using Avalonia.Controls;
using ReVitae.Ui;

namespace ReVitae.Ui.Validation;

public sealed class ChipValidationTarget
{
	private readonly Border _chipBorder;

	public ChipValidationTarget(Border chipBorder) => _chipBorder = chipBorder;

	public Border ChipBorder => _chipBorder;

	public void Apply(IReadOnlyList<string> messages, bool shouldDisplay)
	{
		if (!shouldDisplay || messages.Count == 0)
		{
			ClearPresentation();
			return;
		}

		var text = string.Join(Environment.NewLine, messages);
		_chipBorder.Classes.Add(UiClasses.ChipInvalid);
		ToolTip.SetTip(_chipBorder, text);
	}

	public void ClearPresentation()
	{
		_chipBorder.Classes.Remove(UiClasses.ChipInvalid);
		ToolTip.SetTip(_chipBorder, null);
	}
}
