using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

/// <summary>
/// 日志输出模板
/// </summary>
public static class Debug {
    const int kLogMax = 100;
    const int kLogSpace = 10;
    private static Size m_logMaxSize;
    private static Queue<Label> m_logLabels;
    private static Panel m_panel;
    public static void Init(Panel panel) {
        if (m_panel != null) {            
            return;
        }
        m_panel = panel;
        m_logLabels = new Queue<Label>(kLogMax);
        //日志宽度需要减去滚动条
        m_logMaxSize = new Size(panel.Width - 30, 0);
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
        label.Text = content + "\nTime:" + DateTime.Now.ToString("HH:mm:ss.fff");
        label.Top = 0;
        int height = label.Height + kLogSpace;
        foreach (var item in m_logLabels) {
            item.Top += height;
        }
        m_logLabels.Enqueue(label);
        return label;
    }

    public static void Log(string content) {
        Label label = popLogLabel(content);
        label.ForeColor = Color.Black;
    }

    public static void LogWarning(string content) {
        Label label = popLogLabel(content);
        label.ForeColor = Color.Blue;
    }

    public static void LogError(string content) {
        Label label = popLogLabel(content);
        label.ForeColor = Color.Red;
    }
}