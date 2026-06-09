using AllO.Models;
using Xunit;

namespace AllO.Tests;

public class NetworkTreeTests
{
    private static string Fmt(double ft) => $"{ft:0.##} ft";

    // Red de ejemplo:  raíz ─R1(10ft,2 codos)→ Tee ─R2(4ft)→ Terminal A
    //                                            └─R3(6ft,1 codo)→ Terminal B
    private static (NetNode root, NetRun rootRun) Sample()
    {
        var tee = new NetNode { Kind = NetNodeKind.Fork, Label = "Tee" };
        tee.Children.Add(new NetBranch
        {
            Run = new NetRun { Index = 2, LengthFt = 4, SegmentCount = 1, Size = "Ø15" },
            Node = new NetNode { Kind = NetNodeKind.Terminal, Label = "Terminal A" }
        });
        tee.Children.Add(new NetBranch
        {
            Run = new NetRun { Index = 3, LengthFt = 6, SegmentCount = 2, ElbowCount = 1, Size = "Ø15" },
            Node = new NetNode { Kind = NetNodeKind.Terminal, Label = "Terminal B" }
        });

        var root = new NetNode { Kind = NetNodeKind.Root, Label = "VRF Outdoor" };
        root.Children.Add(new NetBranch
        {
            Run = new NetRun { Index = 1, LengthFt = 10, SegmentCount = 3, ElbowCount = 2, Size = "Ø19" },
            Node = tee
        });

        return (root, new NetRun { LengthFt = 2, SegmentCount = 1 });
    }

    [Fact]
    public void Summary_totals_include_root_run()
    {
        var (root, rootRun) = Sample();
        var s = NetworkSummary.From(root, rootRun);

        Assert.Equal(22, s.TotalLengthFt, 3);          // 2 + 10 + 4 + 6
        Assert.Equal(18, s.LongestPathFt, 3);          // 2 + 10 + 6
        Assert.Equal(3, s.RunCount);
        Assert.Equal(7, s.SegmentCount);               // 1 + 3 + 1 + 2
        Assert.Equal(3, s.ElbowCount);
        Assert.Equal(1, s.ForkCount);
        Assert.Equal(2, s.TerminalCount);
    }

    [Fact]
    public void Summary_without_root_run()
    {
        var (root, _) = Sample();
        var s = NetworkSummary.From(root);

        Assert.Equal(20, s.TotalLengthFt, 3);
        Assert.Equal(16, s.LongestPathFt, 3);
        Assert.Equal(6, s.SegmentCount);
    }

    [Fact]
    public void Lines_draw_tree_with_box_chars()
    {
        var (root, rootRun) = Sample();
        var lines = NetworkTreeFormatter.Lines(root, rootRun, Fmt);

        Assert.Equal("■ VRF Outdoor", lines[0]);
        Assert.Contains("start element: 2 ft", lines[1]);
        Assert.Contains("R1 · 10 ft · Ø19 · 2 elbows → ⑂ Tee", lines[2]);
        Assert.StartsWith("└─ ", lines[2]);
        Assert.Contains("R2 · 4 ft · Ø15 → ● Terminal A", lines[3]);
        Assert.StartsWith("   ├─ ", lines[3]);
        Assert.Contains("R3 · 6 ft · Ø15 · 1 elbow → ● Terminal B", lines[4]);
        Assert.StartsWith("   └─ ", lines[4]);
        Assert.Equal(5, lines.Count);
    }

    [Fact]
    public void Csv_one_row_per_run_with_paths()
    {
        var (root, rootRun) = Sample();
        var csv = NetworkTreeFormatter.Csv(root, rootRun, Fmt);
        var rows = csv.TrimEnd().Split("\r\n");

        Assert.Equal(5, rows.Length);                  // header + R0 + 3 runs
        Assert.StartsWith("Run,Path,Length,Size,Segments,Elbows,Other fittings,Reaches", rows[0]);
        Assert.StartsWith("R0,root,2 ft", rows[1]);
        Assert.Contains("R1,root.1,10 ft,Ø19,3,2,0,Tee", csv);
        Assert.Contains("R2,root.1.1,4 ft,Ø15,1,0,0,Terminal A", csv);
        Assert.Contains("R3,root.1.2,6 ft,Ø15,2,1,0,Terminal B", csv);
    }

    [Fact]
    public void Csv_escapes_commas_in_labels()
    {
        var root = new NetNode { Kind = NetNodeKind.Root, Label = "Root" };
        root.Children.Add(new NetBranch
        {
            Run = new NetRun { Index = 1, LengthFt = 1 },
            Node = new NetNode { Kind = NetNodeKind.Terminal, Label = "Unit, indoor" }
        });

        var csv = NetworkTreeFormatter.Csv(root, null, Fmt);
        Assert.Contains("\"Unit, indoor\"", csv);
    }
}
