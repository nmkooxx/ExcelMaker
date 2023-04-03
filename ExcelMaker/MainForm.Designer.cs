partial class MainForm {
    /// <summary>
    /// 必需的设计器变量。
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// 清理所有正在使用的资源。
    /// </summary>
    /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
    protected override void Dispose(bool disposing) {
        if (disposing && (components != null)) {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows 窗体设计器生成的代码

    /// <summary>
    /// 设计器支持所需的方法 - 不要修改
    /// 使用代码编辑器修改此方法的内容。
    /// </summary>
    private void InitializeComponent() {
            this.title_root = new System.Windows.Forms.Label();
            this.input_root = new System.Windows.Forms.TextBox();
            this.btn_exportServer = new System.Windows.Forms.Button();
            this.input_server = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.input_client = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btn_exportClient = new System.Windows.Forms.Button();
            this.btn_selectAll = new System.Windows.Forms.Button();
            this.btn_selectInversion = new System.Windows.Forms.Button();
            this.btn_scan = new System.Windows.Forms.Button();
            this.panel_log = new System.Windows.Forms.Panel();
            this.excelList = new System.Windows.Forms.CheckedListBox();
            this.group_serverExportType = new System.Windows.Forms.GroupBox();
            this.radio_serverExportType_1 = new System.Windows.Forms.RadioButton();
            this.radio_serverExportType_2 = new System.Windows.Forms.RadioButton();
            this.group_clientExportType = new System.Windows.Forms.GroupBox();
            this.radio_clientExportType_1 = new System.Windows.Forms.RadioButton();
            this.radio_clientExportType_2 = new System.Windows.Forms.RadioButton();
            this.btn_clearLog = new System.Windows.Forms.Button();
            this.input_clientCode = new System.Windows.Forms.TextBox();
            this.input_serverCode = new System.Windows.Forms.TextBox();
            this.check_clientCode = new System.Windows.Forms.CheckBox();
            this.check_serverCode = new System.Windows.Forms.CheckBox();
            this.btn_syncServer = new System.Windows.Forms.Button();
            this.btn_syncClient = new System.Windows.Forms.Button();
            this.check_keep = new System.Windows.Forms.CheckBox();
            this.group_serverExportType.SuspendLayout();
            this.group_clientExportType.SuspendLayout();
            this.SuspendLayout();
            // 
            // title_root
            // 
            this.title_root.AutoSize = true;
            this.title_root.Location = new System.Drawing.Point(13, 14);
            this.title_root.Name = "title_root";
            this.title_root.Size = new System.Drawing.Size(59, 12);
            this.title_root.TabIndex = 0;
            this.title_root.Text = "Excel目录";
            // 
            // input_root
            // 
            this.input_root.Location = new System.Drawing.Point(78, 10);
            this.input_root.Name = "input_root";
            this.input_root.Size = new System.Drawing.Size(550, 21);
            this.input_root.TabIndex = 1;
            this.input_root.TextChanged += new System.EventHandler(this.input_root_TextChanged);
            // 
            // btn_exportServer
            // 
            this.btn_exportServer.Location = new System.Drawing.Point(656, 72);
            this.btn_exportServer.Name = "btn_exportServer";
            this.btn_exportServer.Size = new System.Drawing.Size(119, 23);
            this.btn_exportServer.TabIndex = 2;
            this.btn_exportServer.Text = "导出到服务器";
            this.btn_exportServer.UseVisualStyleBackColor = true;
            this.btn_exportServer.Click += new System.EventHandler(this.btn_exportServer_Click);
            // 
            // input_server
            // 
            this.input_server.Location = new System.Drawing.Point(78, 37);
            this.input_server.Name = "input_server";
            this.input_server.Size = new System.Drawing.Size(448, 21);
            this.input_server.TabIndex = 6;
            this.input_server.TextChanged += new System.EventHandler(this.input_server_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "服务器目录";
            // 
            // input_client
            // 
            this.input_client.Location = new System.Drawing.Point(78, 107);
            this.input_client.Name = "input_client";
            this.input_client.Size = new System.Drawing.Size(448, 21);
            this.input_client.TabIndex = 8;
            this.input_client.TextChanged += new System.EventHandler(this.input_client_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 111);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 12);
            this.label3.TabIndex = 7;
            this.label3.Text = "客户端目录";
            // 
            // btn_exportClient
            // 
            this.btn_exportClient.Location = new System.Drawing.Point(656, 141);
            this.btn_exportClient.Name = "btn_exportClient";
            this.btn_exportClient.Size = new System.Drawing.Size(119, 23);
            this.btn_exportClient.TabIndex = 9;
            this.btn_exportClient.Text = "导出到客户端";
            this.btn_exportClient.UseVisualStyleBackColor = true;
            this.btn_exportClient.Click += new System.EventHandler(this.btn_exportClient_Click);
            // 
            // btn_selectAll
            // 
            this.btn_selectAll.Location = new System.Drawing.Point(15, 173);
            this.btn_selectAll.Name = "btn_selectAll";
            this.btn_selectAll.Size = new System.Drawing.Size(75, 23);
            this.btn_selectAll.TabIndex = 14;
            this.btn_selectAll.Text = "全选";
            this.btn_selectAll.UseVisualStyleBackColor = true;
            this.btn_selectAll.Click += new System.EventHandler(this.btn_selectAll_Click);
            // 
            // btn_selectInversion
            // 
            this.btn_selectInversion.Location = new System.Drawing.Point(100, 173);
            this.btn_selectInversion.Name = "btn_selectInversion";
            this.btn_selectInversion.Size = new System.Drawing.Size(75, 23);
            this.btn_selectInversion.TabIndex = 15;
            this.btn_selectInversion.Text = "反选";
            this.btn_selectInversion.UseVisualStyleBackColor = true;
            this.btn_selectInversion.Click += new System.EventHandler(this.btn_selectInversion_Click);
            // 
            // btn_scan
            // 
            this.btn_scan.Location = new System.Drawing.Point(656, 10);
            this.btn_scan.Name = "btn_scan";
            this.btn_scan.Size = new System.Drawing.Size(119, 23);
            this.btn_scan.TabIndex = 18;
            this.btn_scan.Text = "重新扫描";
            this.btn_scan.UseVisualStyleBackColor = true;
            this.btn_scan.Click += new System.EventHandler(this.btn_scan_Click);
            // 
            // panel_log
            // 
            this.panel_log.AutoScroll = true;
            this.panel_log.AutoScrollMinSize = new System.Drawing.Size(20, 0);
            this.panel_log.Location = new System.Drawing.Point(624, 202);
            this.panel_log.Name = "panel_log";
            this.panel_log.Size = new System.Drawing.Size(543, 754);
            this.panel_log.TabIndex = 19;
            // 
            // excelList
            // 
            this.excelList.CheckOnClick = true;
            this.excelList.ColumnWidth = 420;
            this.excelList.FormattingEnabled = true;
            this.excelList.Location = new System.Drawing.Point(13, 202);
            this.excelList.MultiColumn = true;
            this.excelList.Name = "excelList";
            this.excelList.Size = new System.Drawing.Size(605, 756);
            this.excelList.TabIndex = 20;
            // 
            // group_serverExportType
            // 
            this.group_serverExportType.Controls.Add(this.radio_serverExportType_1);
            this.group_serverExportType.Controls.Add(this.radio_serverExportType_2);
            this.group_serverExportType.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.group_serverExportType.Location = new System.Drawing.Point(219, 64);
            this.group_serverExportType.Name = "group_serverExportType";
            this.group_serverExportType.Size = new System.Drawing.Size(431, 33);
            this.group_serverExportType.TabIndex = 21;
            this.group_serverExportType.TabStop = false;
            this.group_serverExportType.Text = "导出格式";
            // 
            // radio_serverExportType_1
            // 
            this.radio_serverExportType_1.AutoSize = true;
            this.radio_serverExportType_1.Location = new System.Drawing.Point(33, 11);
            this.radio_serverExportType_1.Name = "radio_serverExportType_1";
            this.radio_serverExportType_1.Size = new System.Drawing.Size(41, 16);
            this.radio_serverExportType_1.TabIndex = 0;
            this.radio_serverExportType_1.TabStop = true;
            this.radio_serverExportType_1.Text = "csv";
            this.radio_serverExportType_1.UseVisualStyleBackColor = true;
            this.radio_serverExportType_1.CheckedChanged += new System.EventHandler(this.radio_serverExportType_CheckedChanged);
            // 
            // radio_serverExportType_2
            // 
            this.radio_serverExportType_2.AutoSize = true;
            this.radio_serverExportType_2.Location = new System.Drawing.Point(98, 11);
            this.radio_serverExportType_2.Name = "radio_serverExportType_2";
            this.radio_serverExportType_2.Size = new System.Drawing.Size(47, 16);
            this.radio_serverExportType_2.TabIndex = 1;
            this.radio_serverExportType_2.TabStop = true;
            this.radio_serverExportType_2.Text = "json";
            this.radio_serverExportType_2.UseVisualStyleBackColor = true;
            this.radio_serverExportType_2.CheckedChanged += new System.EventHandler(this.radio_serverExportType_CheckedChanged);
            // 
            // group_clientExportType
            // 
            this.group_clientExportType.Controls.Add(this.radio_clientExportType_1);
            this.group_clientExportType.Controls.Add(this.radio_clientExportType_2);
            this.group_clientExportType.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.group_clientExportType.Location = new System.Drawing.Point(219, 134);
            this.group_clientExportType.Name = "group_clientExportType";
            this.group_clientExportType.Size = new System.Drawing.Size(431, 33);
            this.group_clientExportType.TabIndex = 22;
            this.group_clientExportType.TabStop = false;
            this.group_clientExportType.Text = "导出格式";
            // 
            // radio_clientExportType_1
            // 
            this.radio_clientExportType_1.AutoSize = true;
            this.radio_clientExportType_1.Location = new System.Drawing.Point(33, 11);
            this.radio_clientExportType_1.Name = "radio_clientExportType_1";
            this.radio_clientExportType_1.Size = new System.Drawing.Size(41, 16);
            this.radio_clientExportType_1.TabIndex = 0;
            this.radio_clientExportType_1.TabStop = true;
            this.radio_clientExportType_1.Text = "csv";
            this.radio_clientExportType_1.UseVisualStyleBackColor = true;
            this.radio_clientExportType_1.CheckedChanged += new System.EventHandler(this.radio_clientExportType_CheckedChanged);
            // 
            // radio_clientExportType_2
            // 
            this.radio_clientExportType_2.AutoSize = true;
            this.radio_clientExportType_2.Location = new System.Drawing.Point(98, 11);
            this.radio_clientExportType_2.Name = "radio_clientExportType_2";
            this.radio_clientExportType_2.Size = new System.Drawing.Size(47, 16);
            this.radio_clientExportType_2.TabIndex = 1;
            this.radio_clientExportType_2.TabStop = true;
            this.radio_clientExportType_2.Text = "json";
            this.radio_clientExportType_2.UseVisualStyleBackColor = true;
            this.radio_clientExportType_2.CheckedChanged += new System.EventHandler(this.radio_clientExportType_CheckedChanged);
            // 
            // btn_clearLog
            // 
            this.btn_clearLog.Location = new System.Drawing.Point(623, 175);
            this.btn_clearLog.Name = "btn_clearLog";
            this.btn_clearLog.Size = new System.Drawing.Size(75, 23);
            this.btn_clearLog.TabIndex = 23;
            this.btn_clearLog.Text = "清空日志";
            this.btn_clearLog.UseVisualStyleBackColor = true;
            this.btn_clearLog.Click += new System.EventHandler(this.btn_clearLog_Click);
            // 
            // input_clientCode
            // 
            this.input_clientCode.Location = new System.Drawing.Point(532, 107);
            this.input_clientCode.Name = "input_clientCode";
            this.input_clientCode.Size = new System.Drawing.Size(495, 21);
            this.input_clientCode.TabIndex = 24;
            this.input_clientCode.TextChanged += new System.EventHandler(this.input_clientCode_TextChanged);
            // 
            // input_serverCode
            // 
            this.input_serverCode.Location = new System.Drawing.Point(532, 37);
            this.input_serverCode.Name = "input_serverCode";
            this.input_serverCode.Size = new System.Drawing.Size(495, 21);
            this.input_serverCode.TabIndex = 25;
            this.input_serverCode.TextChanged += new System.EventHandler(this.input_serverCode_TextChanged);
            // 
            // check_clientCode
            // 
            this.check_clientCode.AutoSize = true;
            this.check_clientCode.Location = new System.Drawing.Point(136, 145);
            this.check_clientCode.Name = "check_clientCode";
            this.check_clientCode.Size = new System.Drawing.Size(72, 16);
            this.check_clientCode.TabIndex = 26;
            this.check_clientCode.Text = "导出代码";
            this.check_clientCode.UseVisualStyleBackColor = true;
            // 
            // check_serverCode
            // 
            this.check_serverCode.AutoSize = true;
            this.check_serverCode.Location = new System.Drawing.Point(136, 75);
            this.check_serverCode.Name = "check_serverCode";
            this.check_serverCode.Size = new System.Drawing.Size(72, 16);
            this.check_serverCode.TabIndex = 27;
            this.check_serverCode.Text = "导出代码";
            this.check_serverCode.UseVisualStyleBackColor = true;
            // 
            // btn_syncServer
            // 
            this.btn_syncServer.Location = new System.Drawing.Point(781, 72);
            this.btn_syncServer.Name = "btn_syncServer";
            this.btn_syncServer.Size = new System.Drawing.Size(119, 23);
            this.btn_syncServer.TabIndex = 30;
            this.btn_syncServer.Text = "全部同步到服务器";
            this.btn_syncServer.UseVisualStyleBackColor = true;
            this.btn_syncServer.Click += new System.EventHandler(this.btn_syncServer_Click);
            // 
            // btn_syncClient
            // 
            this.btn_syncClient.Location = new System.Drawing.Point(781, 141);
            this.btn_syncClient.Name = "btn_syncClient";
            this.btn_syncClient.Size = new System.Drawing.Size(119, 23);
            this.btn_syncClient.TabIndex = 31;
            this.btn_syncClient.Text = "全部同步到客户端";
            this.btn_syncClient.UseVisualStyleBackColor = true;
            this.btn_syncClient.Click += new System.EventHandler(this.btn_syncClient_Click);
            // 
            // check_keep
            // 
            this.check_keep.AutoSize = true;
            this.check_keep.Location = new System.Drawing.Point(193, 177);
            this.check_keep.Name = "check_keep";
            this.check_keep.Size = new System.Drawing.Size(72, 16);
            this.check_keep.TabIndex = 32;
            this.check_keep.Text = "原件导出";
            this.check_keep.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1177, 968);
            this.Controls.Add(this.check_keep);
            this.Controls.Add(this.btn_syncClient);
            this.Controls.Add(this.btn_syncServer);
            this.Controls.Add(this.check_serverCode);
            this.Controls.Add(this.check_clientCode);
            this.Controls.Add(this.input_serverCode);
            this.Controls.Add(this.input_clientCode);
            this.Controls.Add(this.btn_clearLog);
            this.Controls.Add(this.group_clientExportType);
            this.Controls.Add(this.group_serverExportType);
            this.Controls.Add(this.excelList);
            this.Controls.Add(this.panel_log);
            this.Controls.Add(this.btn_scan);
            this.Controls.Add(this.btn_selectInversion);
            this.Controls.Add(this.btn_selectAll);
            this.Controls.Add(this.btn_exportClient);
            this.Controls.Add(this.input_client);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.input_server);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btn_exportServer);
            this.Controls.Add(this.input_root);
            this.Controls.Add(this.title_root);
            this.Name = "MainForm";
            this.Text = "Excel转换处理";
            this.group_serverExportType.ResumeLayout(false);
            this.group_serverExportType.PerformLayout();
            this.group_clientExportType.ResumeLayout(false);
            this.group_clientExportType.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label title_root;
    private System.Windows.Forms.TextBox input_root;
    private System.Windows.Forms.Button btn_exportServer;
    private System.Windows.Forms.TextBox input_server;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TextBox input_client;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Button btn_exportClient;
    private System.Windows.Forms.Button btn_selectAll;
    private System.Windows.Forms.Button btn_selectInversion;
    private System.Windows.Forms.Button btn_scan;
    private System.Windows.Forms.Panel panel_log;
    private System.Windows.Forms.CheckedListBox excelList;
    private System.Windows.Forms.GroupBox group_serverExportType;
    private System.Windows.Forms.RadioButton radio_serverExportType_2;
    private System.Windows.Forms.RadioButton radio_serverExportType_1;
    private System.Windows.Forms.GroupBox group_clientExportType;
    private System.Windows.Forms.RadioButton radio_clientExportType_2;
    private System.Windows.Forms.RadioButton radio_clientExportType_1;
    private System.Windows.Forms.Button btn_clearLog;
    private System.Windows.Forms.TextBox input_clientCode;
    private System.Windows.Forms.TextBox input_serverCode;
    private System.Windows.Forms.CheckBox check_clientCode;
    private System.Windows.Forms.CheckBox check_serverCode;
    private System.Windows.Forms.Button btn_syncServer;
    private System.Windows.Forms.Button btn_syncClient;
    private System.Windows.Forms.CheckBox check_keep;
}

