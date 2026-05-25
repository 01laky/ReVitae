using Material.Icons;
using Material.Icons.Avalonia;

namespace ReVitae.Ui;

public static class MaterialIconFactory
{
	public static MaterialIcon Create(MaterialIconKind kind, double size = 20)
	{
		return new MaterialIcon
		{
			Kind = kind,
			Width = size,
			Height = size
		};
	}
}
