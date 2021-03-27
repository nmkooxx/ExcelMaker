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

    private Logic m_Logic;
    public MainForm() {
		InitializeComponent();
        Debug.Init(panel_log);
        m_Logic = new Logic();
        initUI();
        m_Logic.Scan(excelList);        
    }

    public Config config {
        get {
            return m_Logic.config;
        }
    }

    public Setting setting {
        get {
            return m_Logic.setting;
        }
    }

    private void initUI() {
        foreach (var item in group_serverExportType.Controls) {
            var radioButton = item as RadioButton;
            if (radioButton.TabIndex == setting.serverExportType) {
                radioButton.Checked = true;
                break;
            }
        }

        foreach (var item in group_clientExportType.Controls) {
            var radioButton = item as RadioButton;
            if (radioButton.TabIndex == setting.clientExportType) {
                radioButton.Checked = true;
                break;
            }
        }

        input_root.Text = config.rootPath;
        input_server.Text = config.serverPath;
        input_serverCode.Text = config.serverCodePath;
        input_client.Text = config.clientPath;
        input_clientCode.Text = config.clientCodePath;

        check_useSheetName.Checked = setting.nameSource == 1;
    }

    private void btn_scan_Click(object sender, EventArgs e) {
        m_Logic.Scan(excelList);
	}

    private void btn_exportServer_Click(object sender, EventArgs e) {
        var indices = new int[excelList.CheckedIndices.Count];
        for (int i = 0; i < excelList.CheckedIndices.Count; i++) {
            indices[i] = excelList.CheckedIndices[i];
        }

        m_Logic.Export(config.serverPath, indices,
            'S', (ExportType)setting.serverExportType, (ExportLanguage)setting.serverLanguage,
            config.serverCodePath, check_serverCode.Checked);
    }

    private void btn_exportClient_Click(object sender, EventArgs e) {
        var indices = new int[excelList.CheckedIndices.Count];
        for (int i = 0; i < excelList.CheckedIndices.Count; i++) {
            indices[i] = excelList.CheckedIndices[i];
        }

        m_Logic.Export(config.clientPath, indices,
            'C', (ExportType)setting.clientExportType, (ExportLanguage)setting.clientLanguage,
            config.clientCodePath, check_clientCode.Checked);
    }

    private void btn_syncServer_Click(object sender, EventArgs e) {
        m_Logic.Export(config.serverPath, null,
            'S', (ExportType)setting.serverExportType, (ExportLanguage)setting.serverLanguage,
            config.serverCodePath, check_serverCode.Checked);
    }

    private void btn_syncClient_Click(object sender, EventArgs e) {
        m_Logic.Export(config.clientPath, null,
            'C', (ExportType)setting.clientExportType, (ExportLanguage)setting.clientLanguage,
            config.clientCodePath, check_clientCode.Checked);
    }

    private void input_root_TextChanged(object sender, EventArgs e) {
        TextBox textBox = sender as TextBox;
        config.rootPath = textBox.Text;
        m_Logic.WriteConfig();
    }

    private void input_server_TextChanged(object sender, EventArgs e) {
        TextBox textBox = sender as TextBox;
        config.serverPath = textBox.Text;
        m_Logic.WriteConfig();
    }

    private void input_client_TextChanged(object sender, EventArgs e) {
        TextBox textBox = sender as TextBox;
        config.clientPath = textBox.Text;
        m_Logic.WriteConfig();
    }

    private void input_serverCode_TextChanged(object sender, EventArgs e) {
        TextBox textBox = sender as TextBox;
        config.serverCodePath = textBox.Text;
        m_Logic.WriteConfig();
    }

    private void input_clientCode_TextChanged(object sender, EventArgs e) {
        TextBox textBox = sender as TextBox;
        config.clientCodePath = textBox.Text;
        m_Logic.WriteConfig();
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
            setting.clientExportType = radioButton.TabIndex;
            m_Logic.WriteSetting();
            //Debug.LogInfo("clientExportType:" + (ExportType)m_config.clientExportType);
        }
    }

    private void radio_serverExportType_CheckedChanged(object sender, EventArgs e) {
        RadioButton radioButton = sender as RadioButton;
        if (radioButton.Checked) {
            setting.serverExportType = radioButton.TabIndex;
            m_Logic.WriteSetting();
            //Debug.LogInfo("serverExportType:" + (ExportType)m_config.serverExportType);
        }
    }

    private void btn_clearLog_Click(object sender, EventArgs e) {
        Debug.Clear();
    }

    private void check_useSheetName_CheckedChanged(object sender, EventArgs e) {
        CheckBox check = sender as CheckBox;
        setting.nameSource = check.Checked ? 1 : 0;
        m_Logic.WriteSetting();
    }
}
