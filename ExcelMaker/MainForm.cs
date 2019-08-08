using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public partial class MainForm : Form {
	public MainForm() {
		InitializeComponent();
        Debug.Init(panel_log);
        init();
    }

	private void btn_scan_Click(object sender, EventArgs e) {
        scan();
	}

    private void btn_exportServer_Click(object sender, EventArgs e) {
        export(m_config.serverPath, 'S', (ExportType)m_config.serverExportType, 
            m_config.serverCodePath, check_clientCode.Checked);
    }

    private void btn_exportClient_Click(object sender, EventArgs e) {
        export(m_config.clientPath, 'C', (ExportType)m_config.clientExportType, 
            m_config.clientCodePath, false);
    }

    private void input_root_TextChanged(object sender, EventArgs e) {
        TextBox textBox = sender as TextBox;
        m_config.rootPath = textBox.Text;
        writeConfig();
    }

    private void input_server_TextChanged(object sender, EventArgs e) {
        TextBox textBox = sender as TextBox;
        m_config.serverPath = textBox.Text;
        writeConfig();
    }

    private void input_client_TextChanged(object sender, EventArgs e) {
        TextBox textBox = sender as TextBox;
        m_config.clientPath = textBox.Text;
        writeConfig();
    }

    private void input_serverCode_TextChanged(object sender, EventArgs e) {
        TextBox textBox = sender as TextBox;
        m_config.serverCodePath = textBox.Text;
        writeConfig();
    }

    private void input_clientCode_TextChanged(object sender, EventArgs e) {
        TextBox textBox = sender as TextBox;
        m_config.serverCodePath = textBox.Text;
        writeConfig();
    }

    private void btn_selectAll_Click(object sender, EventArgs e) {
        for (int i = 0; i < excelList.Items.Count; i++) {
            excelList.SetItemChecked(i, true);
        }
    }

    private void btn_selectInversion_Click(object sender, EventArgs e) {
        for (int i = 0; i < excelList.Items.Count; i++) {
            if (excelList.GetItemChecked(i)) {
                excelList.SetItemChecked(i, false);
            }
            else {
                excelList.SetItemChecked(i, true);
            }
        }
    }

    private void radio_clientExportType_CheckedChanged(object sender, EventArgs e) {
        RadioButton radioButton = sender as RadioButton;
        if (radioButton.Checked) {
            m_config.clientExportType = radioButton.TabIndex;
            writeConfig();
            //Debug.LogInfo("clientExportType:" + (ExportType)m_config.clientExportType);
        }
    }

    private void radio_serverExportType_CheckedChanged(object sender, EventArgs e) {
        RadioButton radioButton = sender as RadioButton;
        if (radioButton.Checked) {
            m_config.serverExportType = radioButton.TabIndex;
            writeConfig();
            //Debug.LogInfo("serverExportType:" + (ExportType)m_config.serverExportType);
        }
    }

    private void btn_clearLog_Click(object sender, EventArgs e) {
        Debug.Clear();
    }
}
