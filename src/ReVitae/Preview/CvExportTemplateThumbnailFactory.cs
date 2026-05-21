using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ReVitae.Core.Export;

namespace ReVitae.Preview;

internal static class CvExportTemplateThumbnailFactory
{
    public static Control Create(CvExportTemplateId templateId)
    {
        return templateId switch
        {
            CvExportTemplateId.ClassicSidebar => TwoColumn("#D8D8D8", "#FFFFFF", 36, 64),
            CvExportTemplateId.ModernSidebar => ModernSidebar(),
            CvExportTemplateId.CleanTopHeader => TopBand("#5A9BD5"),
            CvExportTemplateId.DarkSidebarAccent => DarkSidebarAccent(),
            CvExportTemplateId.CenteredMinimal => StackedBands("#E0E0E0", "#212121"),
            CvExportTemplateId.PhotoLeftBand => PhotoHeader("#E67E22", "#E8E8E8"),
            CvExportTemplateId.ExecutiveBlueSidebar => SidebarAccent("#E5E5E5", "#1E3A5F"),
            CvExportTemplateId.PeachDesigner => PeachHeader(),
            CvExportTemplateId.NavyProfileSplit => TopBand("#1B2A41"),
            CvExportTemplateId.ForestGreenSidebar => ForestSidebar("#2F5D3A"),
            CvExportTemplateId.YellowSkillDots => YellowAccent(),
            CvExportTemplateId.RoyalBlueSidebar => FullSidebar("#4A76C0", "#333A45"),
            CvExportTemplateId.OrangeTimeline => OrangeTimeline("#E67E22"),
            CvExportTemplateId.BlueAccentSummary => BlueAccent(),
            CvExportTemplateId.PillHeaderSplit => PillHeader(),
            CvExportTemplateId.NavyOverlapPhoto => TopBand("#1E3A5F"),
            _ => TwoColumn("#D8D8D8", "#FFFFFF", 36, 64)
        };
    }

    private static Grid TwoColumn(string left, string right, int leftWeight, int rightWeight)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions($"{leftWeight}*,{rightWeight}*") };
        grid.Children.Add(new Border { Background = Brush.Parse(left) });
        var rightBorder = new Border { Background = Brush.Parse(right) };
        Grid.SetColumn(rightBorder, 1);
        grid.Children.Add(rightBorder);
        return grid;
    }

    private static Grid ModernSidebar()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("34*,66*"),
            RowDefinitions = new RowDefinitions("32*,68*")
        };
        var sidebar = new Border { Background = Brush.Parse("#D7D7D7") };
        Grid.SetRowSpan(sidebar, 2);
        grid.Children.Add(sidebar);
        var header = new Border { Background = Brush.Parse("#444444") };
        Grid.SetColumn(header, 1);
        grid.Children.Add(header);
        var body = new Border { Background = Brushes.White };
        Grid.SetColumn(body, 1);
        Grid.SetRow(body, 1);
        grid.Children.Add(body);
        return grid;
    }

    private static Grid TopBand(string color)
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("39*,61*") };
        grid.Children.Add(new Border { Background = Brush.Parse(color) });
        var body = new Border { Background = Brushes.White };
        Grid.SetRow(body, 1);
        grid.Children.Add(body);
        return grid;
    }

    private static Grid DarkSidebarAccent()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("34*,66*") };
        grid.Children.Add(new Border { Background = Brush.Parse("#2F3A45") });
        var right = new Grid { RowDefinitions = new RowDefinitions("32*,68*") };
        Grid.SetColumn(right, 1);
        right.Children.Add(new Border { Background = Brush.Parse("#5B9BB0") });
        var body = new Border { Background = Brush.Parse("#F2F2F2") };
        Grid.SetRow(body, 1);
        right.Children.Add(body);
        grid.Children.Add(right);
        return grid;
    }

    private static Grid StackedBands(string mid, string bottom)
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("20*,35*,45*") };
        grid.Children.Add(new Border { Background = Brushes.White });
        var midBand = new Border { Background = Brush.Parse(mid), CornerRadius = new CornerRadius(4) };
        Grid.SetRow(midBand, 1);
        grid.Children.Add(midBand);
        var bottomBand = new Border { Background = Brush.Parse(bottom), CornerRadius = new CornerRadius(4) };
        Grid.SetRow(bottomBand, 2);
        grid.Children.Add(bottomBand);
        return grid;
    }

    private static Grid PhotoHeader(string accent, string band)
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("40*,60*") };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("30*,70*") };
        header.Children.Add(new Border { Background = Brush.Parse(accent), CornerRadius = new CornerRadius(4) });
        var name = new Border { Background = Brushes.White };
        Grid.SetColumn(name, 1);
        header.Children.Add(name);
        grid.Children.Add(header);
        var summary = new Border { Background = Brush.Parse(band) };
        Grid.SetRow(summary, 1);
        grid.Children.Add(summary);
        return grid;
    }

    private static Grid SidebarAccent(string sidebar, string accent)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("34*,66*") };
        var left = new Grid { RowDefinitions = new RowDefinitions("8*,92*") };
        left.Children.Add(new Border { Background = Brush.Parse(accent) });
        var body = new Border { Background = Brush.Parse(sidebar) };
        Grid.SetRow(body, 1);
        left.Children.Add(body);
        grid.Children.Add(left);
        var right = new Border { Background = Brushes.White };
        Grid.SetColumn(right, 1);
        grid.Children.Add(right);
        return grid;
    }

    private static Grid PeachHeader()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("34*,66*"),
            RowDefinitions = new RowDefinitions("35*,65*")
        };
        var photo = new Border { Background = Brush.Parse("#E9B083"), CornerRadius = new CornerRadius(6) };
        grid.Children.Add(photo);
        var header = new Border { Background = Brush.Parse("#E9B083"), CornerRadius = new CornerRadius(6) };
        Grid.SetColumn(header, 1);
        grid.Children.Add(header);
        var sidebar = new Border { Background = Brush.Parse("#E5E5E5"), CornerRadius = new CornerRadius(6) };
        Grid.SetRow(sidebar, 1);
        grid.Children.Add(sidebar);
        var main = new Border { Background = Brushes.White };
        Grid.SetColumn(main, 1);
        Grid.SetRow(main, 1);
        grid.Children.Add(main);
        return grid;
    }

    private static Grid ForestSidebar(string green)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("34*,66*") };
        var left = new Grid { RowDefinitions = new RowDefinitions("30*,25*,45*") };
        left.Children.Add(new Border { Background = Brush.Parse("#CCCCCC") });
        var name = new Border { Background = Brush.Parse(green), CornerRadius = new CornerRadius(4) };
        Grid.SetRow(name, 1);
        left.Children.Add(name);
        var contact = new Border { Background = Brushes.White };
        Grid.SetRow(contact, 2);
        left.Children.Add(contact);
        grid.Children.Add(left);
        var right = new Border { Background = Brushes.White };
        Grid.SetColumn(right, 1);
        grid.Children.Add(right);
        return grid;
    }

    private static Grid YellowAccent()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("64*,36*") };
        var left = new Grid { ColumnDefinitions = new ColumnDefinitions("8*,92*") };
        left.Children.Add(new Border { Background = Brush.Parse("#F5C400") });
        var body = new Border { Background = Brushes.White };
        Grid.SetColumn(body, 1);
        left.Children.Add(body);
        grid.Children.Add(left);
        var photo = new Border { Background = Brush.Parse("#CCCCCC") };
        Grid.SetColumn(photo, 1);
        grid.Children.Add(photo);
        return grid;
    }

    private static Grid FullSidebar(string blue, string header)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("34*,66*") };
        grid.Children.Add(new Border { Background = Brush.Parse(blue) });
        var right = new Grid { RowDefinitions = new RowDefinitions("35*,65*") };
        Grid.SetColumn(right, 1);
        right.Children.Add(new Border { Background = Brush.Parse(header) });
        var body = new Border { Background = Brushes.White };
        Grid.SetRow(body, 1);
        right.Children.Add(body);
        grid.Children.Add(right);
        return grid;
    }

    private static Grid OrangeTimeline(string orange)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("8*,92*") };
        grid.Children.Add(new Border { Background = Brush.Parse(orange) });
        var body = new Border { Background = Brushes.White };
        Grid.SetColumn(body, 1);
        grid.Children.Add(body);
        return grid;
    }

    private static Grid BlueAccent()
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("35*,65*") };
        var top = new Grid { ColumnDefinitions = new ColumnDefinitions("8*,92*") };
        top.Children.Add(new Border { Background = Brush.Parse("#2C4A93") });
        var bodyTop = new Border { Background = Brushes.White };
        Grid.SetColumn(bodyTop, 1);
        top.Children.Add(bodyTop);
        grid.Children.Add(top);
        var bottom = new Border { Background = Brushes.White };
        Grid.SetRow(bottom, 1);
        grid.Children.Add(bottom);
        return grid;
    }

    private static Grid PillHeader()
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("40*,60*") };
        var top = new Grid { ColumnDefinitions = new ColumnDefinitions("24*,76*") };
        top.Children.Add(new Border { Background = Brush.Parse("#CCCCCC"), CornerRadius = new CornerRadius(8) });
        var pill = new Border { Background = Brush.Parse("#E8E8E8"), CornerRadius = new CornerRadius(8) };
        Grid.SetColumn(pill, 1);
        top.Children.Add(pill);
        grid.Children.Add(top);
        var body = new Border { Background = Brushes.White };
        Grid.SetRow(body, 1);
        grid.Children.Add(body);
        return grid;
    }
}
