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
    private static Size m_LogMaxSize;
    private static Queue<Label> m_LogLabels;
    private static Panel m_Panel;
    private static StreamWriter m_StreamWriter;
    public static void Init(Panel panel) {
        if (m_Panel != null) {
            return;
        }
        m_Panel = panel;
        m_LogLabels = new Queue<Label>(kLogMax);
        //日志宽度需要减去滚动条
        m_LogMaxSize = new Size(panel.Width - 30, 0);

        string filePath = "ExcelMakerLog.txt";
        if (File.Exists(filePath)) {
            File.Delete(filePath);
        }
        var fileInfo = new FileInfo(filePath);
        m_StreamWriter = fileInfo.AppendText();
        m_StreamWriter.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n");
        m_StreamWriter.Flush();
    }

    public static void Clear() {
        if (m_Panel == null) {
            return;
        }
        m_Panel.Controls.Clear();
        foreach (var item in m_LogLabels) {
            item.Top = 0;
            item.Text = string.Empty;
            item.Height = 0;
        }
    }

    private static Label PopLogLabel(string content) {
        Label label;
        if (m_LogLabels.Count >= kLogMax) {
            label = m_LogLabels.Dequeue();
        }
        else {
            label = new Label();
            label.AutoSize = true;
            label.MaximumSize = m_LogMaxSize;
            m_Panel.Controls.Add(label);
        }
        string text = content + "\nTime:" + DateTime.Now.ToString("HH:mm:ss.fff");
        label.Text = text;
        label.Top = 0;
        int height = label.Height + kLogSpace;
        foreach (var item in m_LogLabels) {
            item.Top += height;
        }
        m_LogLabels.Enqueue(label);
        m_Panel.Refresh();
        return label;
    }

    public static void Log(string content) {
        if (m_Panel == null) {
            return;
        }
        Label label = PopLogLabel(content);
        label.ForeColor = Color.Black;
        //m_streamWriter.WriteLine("Log\t" + content);
        //m_streamWriter.Flush();
    }

    public static void LogWarning(string content) {
        if (m_Panel == null) {
            return;
        }
        Label label = PopLogLabel(content);
        label.ForeColor = Color.Blue;
        m_StreamWriter.WriteLine("Warn\t" + content);
        m_StreamWriter.Flush();
    }

    public static void LogError(string content) {
        if (m_Panel == null) {
            return;
        }
        Label label = PopLogLabel(content);
        label.ForeColor = Color.Red;
        m_StreamWriter.WriteLine("Error\t" + content);
        m_StreamWriter.Flush();
    }

    public static void WriteError(string content) {
        if (m_Panel == null) {
            return;
        }
        m_StreamWriter.WriteLine("Error\t" + content);
        m_StreamWriter.Flush();
    }
}