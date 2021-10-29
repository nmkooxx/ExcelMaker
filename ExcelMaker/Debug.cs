using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

/// <summary>
/// 日志输出模板
/// </summary>
public static class Debug {
    const int kLogMax = 90;
    const int kLogSpace = 10;
    private static Size m_logMaxSize;
    private static Queue<Label> m_logLabels;
    private static Panel m_panel;
    private static StreamWriter m_streamWriter;
    public static void Init(Panel panel) {
        if (m_panel != null) {
            return;
        }
        m_panel = panel;
        m_logLabels = new Queue<Label>(kLogMax);
        //日志宽度需要减去滚动条
        m_logMaxSize = new Size(panel.Width - 30, 0);

        string filePath = "ExcelMakerLog.txt";
        if (File.Exists(filePath)) {
            File.Delete(filePath);
        }
        var fileInfo = new FileInfo(filePath);
        m_streamWriter = fileInfo.AppendText();
        m_streamWriter.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n");
        m_streamWriter.Flush();
    }

    public static void Clear() {
        if (m_panel == null) {
            return;
        }
        m_panel.Controls.Clear();
        foreach (var item in m_logLabels) {
            item.Top = 0;
            item.Text = string.Empty;
            item.Height = 0;
        }
    }

    private static Label popLogLabel(string content) {
        Label label;
        if (m_logLabels.Count >= kLogMax) {
            label = m_logLabels.Dequeue();
        }
        else {
            label = new Label();
            label.AutoSize = true;
            label.MaximumSize = m_logMaxSize;
            m_panel.Controls.Add(label);
        }
        string text = content + "\nTime:" + DateTime.Now.ToString("HH:mm:ss.fff");
        label.Text = text;
        label.Top = 0;
        int height = label.Height + kLogSpace;
        foreach (var item in m_logLabels) {
            item.Top += height;
        }
        m_logLabels.Enqueue(label);
        m_panel.Refresh();
        return label;
    }

    public static void Log(string content) {
        Label label = popLogLabel(content);
        label.ForeColor = Color.Black;
        //m_streamWriter.WriteLine("Log\t" + content);
        //m_streamWriter.Flush();
    }

    public static void LogWarning(string content) {
        Label label = popLogLabel(content);
        label.ForeColor = Color.Blue;
        m_streamWriter.WriteLine("Warn\t" + content);
        m_streamWriter.Flush();
    }

    public static void LogError(string content) {
        Label label = popLogLabel(content);
        label.ForeColor = Color.Red;
        m_streamWriter.WriteLine("Error\t" + content);
        m_streamWriter.Flush();
    }

    public static void WriteError(string content) {
        m_streamWriter.WriteLine("Error\t" + content);
        m_streamWriter.Flush();
    }
}