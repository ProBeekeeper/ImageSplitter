using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageSplitter.Models;
using ImageSplitter.Services;

namespace ImageSplitter.Views
{
    public class MainForm : Form
    {
        private readonly LocalizationService _langService = new();
        private readonly ImageProcessingService _imageService = new();

        private ComboBox comboLang = null!;
        private Label lblRows = null!;
        private NumericUpDown numRows = null!;
        private Label lblCols = null!;
        private NumericUpDown numCols = null!;
        private Button btnSelect = null!;
        private Button btnExecute = null!;
        private ProgressBar progressBar = null!;
        private TextBox txtLog = null!;
        private CheckBox chkPreview = null!;

        private Label lblPreview = null!;
        private PictureBox picPreview = null!;
        private Bitmap? _previewBitmap;
        private string[] _selectedFiles = Array.Empty<string>();
        private readonly System.Windows.Forms.Timer _debounceTimer = new();

        public MainForm()
        {
            InitUI();
            UpdateLanguageStrings();
            InitDebounceTimer();
        }

        private void InitUI()
        {
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ImageSplitter.split.ico"))
            {
                if (stream != null) this.Icon = new Icon(stream);
            }

            this.Size = new Size(540, 480);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            comboLang = new ComboBox { Location = new Point(380, 15), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var lang in _langService.AvailableLanguages) comboLang.Items.Add(lang);
            
            int defaultIdx = comboLang.Items.IndexOf("en-US");
            comboLang.SelectedIndex = defaultIdx >= 0 ? defaultIdx : 0;

            comboLang.SelectedIndexChanged += (s, e) => {
                if (comboLang.SelectedItem != null)
                {
                    _langService.SetLanguage(comboLang.SelectedItem.ToString()!);
                    UpdateLanguageStrings();
                }
            };

            lblRows = new Label { Location = new Point(20, 20), AutoSize = true };
            numRows = new NumericUpDown { Location = new Point(180, 18), Size = new Size(60, 25), Minimum = 1, Value = 2 };
            numRows.ValueChanged += (s, e) => RestartDebounce();
            numRows.KeyUp += (s, e) => RestartDebounce();

            lblCols = new Label { Location = new Point(20, 55), AutoSize = true };
            numCols = new NumericUpDown { Location = new Point(180, 53), Size = new Size(60, 25), Minimum = 1, Value = 3 };
            numCols.ValueChanged += (s, e) => RestartDebounce();
            numCols.KeyUp += (s, e) => RestartDebounce();

            chkPreview = new CheckBox
            {
                Text = "Preview before split",
                Location = new Point(260, 53),
                Size = new Size(150, 25),
                Checked = true
            };
            chkPreview.CheckedChanged += (s, e) => TogglePreviewPanelVisibility();

            btnSelect = new Button { Location = new Point(20, 95), Size = new Size(230, 50), Font = new Font("Microsoft JhengHei", 10, FontStyle.Bold) };
            btnSelect.Click += (s, e) => ExecuteFileSelectionWorkflow();

            btnExecute = new Button { Location = new Point(274, 95), Size = new Size(230, 50), Font = new Font("Microsoft JhengHei", 10, FontStyle.Bold), Enabled = false };
            btnExecute.Click += async (s, e) => await ExecuteImageSplittingAsync();

            progressBar = new ProgressBar { Location = new Point(20, 160), Size = new Size(484, 20) };
            txtLog = new TextBox { Location = new Point(20, 195), Size = new Size(484, 225), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9.5f) };

            lblPreview = new Label { Location = new Point(540, 20), AutoSize = true, Font = new Font("Microsoft JhengHei", 10, FontStyle.Bold), Visible = false };
            picPreview = new PictureBox
            {
                Location = new Point(540, 55),
                Size = new Size(380, 365),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                Visible = false
            };
            picPreview.Paint += PicPreview_Paint;

            this.Controls.AddRange(new Control[] { comboLang, lblRows, numRows, lblCols, numCols, chkPreview, btnSelect, btnExecute, progressBar, txtLog, lblPreview, picPreview });
        }

        private void InitDebounceTimer()
        {
            _debounceTimer.Interval = 300;
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                picPreview.Invalidate();
            };
        }

        private void RestartDebounce()
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void UpdateLanguageStrings()
        {
            this.Text = _langService.Get("Title");
            lblRows.Text = _langService.Get("Rows");
            lblCols.Text = _langService.Get("Cols");
            btnSelect.Text = _langService.Get("BtnSelect");
            btnExecute.Text = _langService.Get("BtnExecute") != "BtnExecute" ? _langService.Get("BtnExecute") : "Execute Split";
            lblPreview.Text = _langService.Get("PreviewLabel");
        }

        private void PicPreview_Paint(object? sender, PaintEventArgs e)
        {
            if (picPreview.Image == null || _previewBitmap == null) return;

            float imgWidth = _previewBitmap.Width;
            float imgHeight = _previewBitmap.Height;
            float boxWidth = picPreview.Width;
            float boxHeight = picPreview.Height;

            float ratio = Math.Min(boxWidth / imgWidth, boxHeight / imgHeight);
            float nw = imgWidth * ratio;
            float nh = imgHeight * ratio;
            float x = (boxWidth - nw) / 2;
            float y = (boxHeight - nh) / 2;

            int rows = (int)numRows.Value;
            int cols = (int)numCols.Value;

            if (rows < 1 || cols < 1) return;

            using (Pen pen = new Pen(Color.Red, 2f))
            {
                for (int i = 1; i < cols; i++)
                {
                    float cx = x + (nw / cols) * i;
                    e.Graphics.DrawLine(pen, cx, y, cx, y + nh);
                }
                for (int i = 1; i < rows; i++)
                {
                    float cy = y + (nh / rows) * i;
                    e.Graphics.DrawLine(pen, x, cy, x + nw, cy);
                }
            }
        }

        private void ExecuteFileSelectionWorkflow()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                ofd.Filter = _langService.Get("Filter") + "|All Files|*.*";
                ofd.Title = _langService.Get("OfdTitle");

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _selectedFiles = ofd.FileNames;
                    txtLog.Clear();
                    progressBar.Value = 0;

                    if (_selectedFiles.Length == 1)
                    {
                        chkPreview.Enabled = true;
                        Log(string.Format(_langService.Get("LogLoaded"), 1));
                        
                        try
                        {
                            _previewBitmap?.Dispose();
                            _previewBitmap = new Bitmap(_selectedFiles[0]);
                            picPreview.Image = _previewBitmap;
                        }
                        catch (Exception ex)
                        {
                            Log($"[Preview Error] {ex.Message}");
                        }

                        TogglePreviewPanelVisibility();
                    }
                    else
                    {
                        chkPreview.Enabled = false;
                        _previewBitmap?.Dispose();
                        _previewBitmap = null;
                        picPreview.Image = null;
                        
                        this.Width = 540;
                        lblPreview.Visible = false;
                        picPreview.Visible = false;

                        Log(string.Format(_langService.Get("LogLoaded"), _selectedFiles.Length));
                    }

                    btnExecute.Enabled = _selectedFiles.Length > 0;
                }
            }
        }

        private void TogglePreviewPanelVisibility()
        {
            if (_selectedFiles.Length == 1 && chkPreview.Checked && _previewBitmap != null)
            {
                this.Width = 960;
                lblPreview.Visible = true;
                picPreview.Visible = true;
                picPreview.Invalidate();
            }
            else
            {
                this.Width = 540;
                lblPreview.Visible = false;
                picPreview.Visible = false;
            }
        }

        private async Task ExecuteImageSplittingAsync()
        {
            if (_selectedFiles.Length == 0) return;

            int rows = (int)numRows.Value;
            int cols = (int)numCols.Value;

            SetControlsEnabled(false);
            IProgress<int> progress = new Progress<int>(value => progressBar.Value = value);

            await Task.Run(() => {
                int completed = 0;
                Parallel.ForEach(_selectedFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file => {
                    try
                    {
                        _imageService.SplitImageToFolder(file, rows, cols, (folderName) => {
                            Log(string.Format(_langService.Get("LogSuccess"), folderName));
                        });
                        int current = System.Threading.Interlocked.Increment(ref completed);
                        progress.Report((int)((double)current / _selectedFiles.Length * 100));
                    }
                    catch (Exception ex)
                    {
                        Log(string.Format(_langService.Get("LogError"), Path.GetFileName(file), ex.Message));
                    }
                });
            });

            Log(_langService.Get("LogAllDone"));
            SetControlsEnabled(true);
            btnExecute.Enabled = false;
            _selectedFiles = Array.Empty<string>();
            
            MessageBox.Show(_langService.Get("MsgSuccess"), _langService.Get("MsgTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SetControlsEnabled(bool enabled)
        {
            btnSelect.Enabled = comboLang.Enabled = numRows.Enabled = numCols.Enabled = chkPreview.Enabled = enabled;
        }

        private void Log(string message)
        {
            if (txtLog.InvokeRequired) txtLog.Invoke(new Action(() => Log(message)));
            else txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _debounceTimer.Dispose();
            _previewBitmap?.Dispose();
            base.OnFormClosed(e);
        }
    }
}