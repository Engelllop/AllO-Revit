using System.Text;

namespace AllO.Models;

public enum NetNodeKind { Root, Fork, Equipment, Terminal, End }

/// <summary>Tramo: corrida de segmentos + fittings de paso entre dos nodos (bifurcación/equipo/final).</summary>
public class NetRun
{
    public int Index { get; set; }
    public double LengthFt { get; set; }
    public int SegmentCount { get; set; }
    public int ElbowCount { get; set; }
    public int OtherFittingCount { get; set; }
    public string Size { get; set; } = "";
}

public class NetNode
{
    public NetNodeKind Kind { get; set; }
    public string Label { get; set; } = "";
    public List<NetBranch> Children { get; } = new();
}

public class NetBranch
{
    public NetRun Run { get; set; } = new();
    public NetNode Node { get; set; } = new();
}

public class NetworkSummary
{
    public double TotalLengthFt { get; private set; }
    public double LongestPathFt { get; private set; }
    public int RunCount { get; private set; }
    public int SegmentCount { get; private set; }
    public int ElbowCount { get; private set; }
    public int OtherFittingCount { get; private set; }
    public int ForkCount { get; private set; }
    public int TerminalCount { get; private set; }

    public static NetworkSummary From(NetNode root, NetRun? rootRun = null)
    {
        var s = new NetworkSummary();
        if (rootRun != null)
        {
            s.Add(rootRun);
            s.RunCount--;
        }
        s.LongestPathFt = (rootRun?.LengthFt ?? 0) + s.Visit(root);
        s.TotalLengthFt += rootRun?.LengthFt ?? 0;
        return s;
    }

    private double Visit(NetNode node)
    {
        if (node.Kind == NetNodeKind.Fork) ForkCount++;
        if (node.Kind is NetNodeKind.Terminal or NetNodeKind.Equipment) TerminalCount++;

        double longest = 0;
        foreach (var b in node.Children)
        {
            Add(b.Run);
            TotalLengthFt += b.Run.LengthFt;
            longest = Math.Max(longest, b.Run.LengthFt + Visit(b.Node));
        }
        return longest;
    }

    private void Add(NetRun run)
    {
        RunCount++;
        SegmentCount += run.SegmentCount;
        ElbowCount += run.ElbowCount;
        OtherFittingCount += run.OtherFittingCount;
    }
}

public static class NetworkTreeFormatter
{
    public static List<string> Lines(NetNode root, NetRun? rootRun, Func<double, string> fmt)
    {
        var lines = new List<string> { "■ " + root.Label };
        if (rootRun != null && rootRun.LengthFt > 0)
            lines.Add("│  (start element: " + RunText(rootRun, fmt) + ")");
        Visit(root, "", lines, fmt);
        return lines;
    }

    private static void Visit(NetNode node, string indent, List<string> lines, Func<double, string> fmt)
    {
        for (int i = 0; i < node.Children.Count; i++)
        {
            var b = node.Children[i];
            bool last = i == node.Children.Count - 1;
            lines.Add(indent + (last ? "└─ " : "├─ ") + $"R{b.Run.Index} · {RunText(b.Run, fmt)} → {NodeText(b.Node)}");
            Visit(b.Node, indent + (last ? "   " : "│  "), lines, fmt);
        }
    }

    private static string RunText(NetRun r, Func<double, string> fmt)
    {
        var sb = new StringBuilder(fmt(r.LengthFt));
        if (!string.IsNullOrEmpty(r.Size)) sb.Append(" · ").Append(r.Size);
        if (r.ElbowCount > 0) sb.Append(" · ").Append(r.ElbowCount).Append(r.ElbowCount == 1 ? " elbow" : " elbows");
        if (r.OtherFittingCount > 0) sb.Append(" · ").Append(r.OtherFittingCount).Append(" fitting(s)");
        return sb.ToString();
    }

    private static string NodeText(NetNode n) => n.Kind switch
    {
        NetNodeKind.Fork => "⑂ " + n.Label,
        NetNodeKind.Equipment or NetNodeKind.Terminal => "● " + n.Label,
        _ => "○ " + (string.IsNullOrEmpty(n.Label) ? "end" : n.Label)
    };

    public static string Csv(NetNode root, NetRun? rootRun, Func<double, string> fmt)
    {
        var sb = new StringBuilder("Run,Path,Length,Size,Segments,Elbows,Other fittings,Reaches\r\n");
        if (rootRun != null && rootRun.LengthFt > 0)
            sb.Append("R0,root,").Append(CsvRun(rootRun, fmt)).Append(',').Append(Escape(root.Label)).Append("\r\n");
        VisitCsv(root, "root", sb, fmt);
        return sb.ToString();
    }

    private static void VisitCsv(NetNode node, string path, StringBuilder sb, Func<double, string> fmt)
    {
        for (int i = 0; i < node.Children.Count; i++)
        {
            var b = node.Children[i];
            var p = path + "." + (i + 1);
            sb.Append('R').Append(b.Run.Index).Append(',').Append(p).Append(',')
              .Append(CsvRun(b.Run, fmt)).Append(',').Append(Escape(b.Node.Label)).Append("\r\n");
            VisitCsv(b.Node, p, sb, fmt);
        }
    }

    private static string CsvRun(NetRun r, Func<double, string> fmt)
        => $"{Escape(fmt(r.LengthFt))},{Escape(r.Size)},{r.SegmentCount},{r.ElbowCount},{r.OtherFittingCount}";

    private static string Escape(string s)
        => s.Contains(',') || s.Contains('"') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
}
