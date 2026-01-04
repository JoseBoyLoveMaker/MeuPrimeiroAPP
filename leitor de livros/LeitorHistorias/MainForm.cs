using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Windows.Forms;

namespace LeitorHistorias;

public class MainForm : Form
{
	private ListBox listStories;

	private Button btnOpen;

	private Button btnPrev;

	private Button btnNext;

	private Button btnPlay;

	private Button btnStop;

	private Button btnFontPlus;

	private Button btnFontMinus;

	private Button btnBookmark;

	private Button btnTheme;

	private Label lblPage;

	private TrackBar progressBar;

	private RichTextBox rtbContent;

	private OpenFileDialog openFileDialog;

	private List<string> filePaths = new List<string>();

	private string currentText = "";

	private List<string> pages = new List<string>();

	private int charsPerPage = 2000;

	private int currentPage = 0;

	private SpeechSynthesizer synth;

	private bool isPlaying = false;

	private int savedBookmarkPage = -1;

	private bool darkMode = true;

	private Label lblValorVel;

	private int leituraRate = 0;

	private List<string> sentences = new List<string>();

	private int currentSentenceIndex = 0;

	private bool isPaused = false;

	private bool isStopped = false;

	private int currentWordIndex = 0;

	private string[] currentWords;

	private DateTime speakStartTime;

	private void RtbContent_MouseDown(object sender, MouseEventArgs e)
	{
		try
		{
			int charIndexFromPosition = rtbContent.GetCharIndexFromPosition(e.Location);
			string text = rtbContent.Text;
			if (charIndexFromPosition >= 0 && charIndexFromPosition < text.Length)
			{
				string text2 = text.Substring(charIndexFromPosition);
				PrepareSentences(text2);
				synth.SpeakAsyncCancelAll();
				isPlaying = true;
				isPaused = false;
				btnPlay.Text = "Pause";
				ReadNextSentence();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Erro: " + ex.Message);
		}
	}

	public MainForm()
	{
		InitializeComponent();
		synth = new SpeechSynthesizer();
		synth.SetOutputToDefaultAudioDevice();
		LoadInstalledVoices();
		synth.SpeakCompleted += Synth_SpeakCompleted;
		rtbContent.MouseDown += RtbContent_MouseDown;
		synth.SpeakProgress += Synth_SpeakProgress;
		ApplyTheme();
	}

	private void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
	{
		if (!e.Cancelled && !isPaused && !isStopped)
		{
			currentSentenceIndex++;
			ReadNextSentence();
		}
	}

	private void BtnLerCaractere_Click(object sender, EventArgs e)
	{
		try
		{
			TextBox textBox = base.Controls["txtCaractere"] as TextBox;
			if (!int.TryParse(textBox.Text, out var result))
			{
				MessageBox.Show("Digite um número válido.");
				return;
			}
			string text = rtbContent.Text;
			if (result < 0 || result >= text.Length)
			{
				MessageBox.Show("Índice fora do limite do texto.");
				return;
			}
			string text2 = text.Substring(result);
			PrepareSentences(text2);
			synth.SpeakAsyncCancelAll();
			isPlaying = true;
			isPaused = false;
			btnPlay.Text = "Pause";
			ReadNextSentence();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Erro: " + ex.Message);
		}
	}

	private void LoadInstalledVoices()
	{
		try
		{
			ReadOnlyCollection<InstalledVoice> installedVoices = synth.GetInstalledVoices();
			if (installedVoices.Count == 0)
			{
				MessageBox.Show("Nenhuma voz instalada no Windows.");
				return;
			}
			foreach (InstalledVoice item in installedVoices)
			{
				MessageBox.Show("Voz encontrada: " + item.VoiceInfo.Name);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Erro ao carregar vozes: " + ex.Message);
		}
	}

	private void InitializeComponent()
	{
		this.listStories = new System.Windows.Forms.ListBox();
		this.btnOpen = new System.Windows.Forms.Button();
		this.rtbContent = new System.Windows.Forms.RichTextBox();
		this.btnPrev = new System.Windows.Forms.Button();
		this.btnNext = new System.Windows.Forms.Button();
		this.btnPlay = new System.Windows.Forms.Button();
		this.btnStop = new System.Windows.Forms.Button();
		this.btnFontPlus = new System.Windows.Forms.Button();
		this.btnFontMinus = new System.Windows.Forms.Button();
		this.btnBookmark = new System.Windows.Forms.Button();
		this.btnTheme = new System.Windows.Forms.Button();
		this.lblPage = new System.Windows.Forms.Label();
		this.progressBar = new System.Windows.Forms.TrackBar();
		this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
		((System.ComponentModel.ISupportInitialize)this.progressBar).BeginInit();
		base.SuspendLayout();
		System.Windows.Forms.Label label = new System.Windows.Forms.Label();
		label.Text = "Velocidade:";
		label.Location = new System.Drawing.Point(20, 180);
		label.AutoSize = true;
		base.Controls.Add(label);
		System.Windows.Forms.Label lblValorVel = new System.Windows.Forms.Label();
		lblValorVel.Text = this.leituraRate.ToString();
		lblValorVel.Location = new System.Drawing.Point(80, 180);
		lblValorVel.AutoSize = true;
		lblValorVel.Name = "lblValorVel";
		base.Controls.Add(lblValorVel);
		System.Windows.Forms.Button button = new System.Windows.Forms.Button();
		button.Text = "-";
		button.Width = 40;
		button.Location = new System.Drawing.Point(100, 175);
		button.Click += delegate
		{
			if (this.leituraRate > -10)
			{
				this.leituraRate--;
			}
			lblValorVel.Text = this.leituraRate.ToString();
			if (this.synth != null)
			{
				this.synth.Rate = this.leituraRate;
				this.synth.SpeakAsyncCancelAll();
				if (this.isPlaying && !this.isPaused)
				{
					this.ContinueFromWord();
				}
			}
		};
		base.Controls.Add(button);
		System.Windows.Forms.Button button2 = new System.Windows.Forms.Button();
		button2.Text = "+";
		button2.Width = 40;
		button2.Location = new System.Drawing.Point(145, 175);
		button2.Click += delegate
		{
			if (this.leituraRate < 10)
			{
				this.leituraRate++;
			}
			lblValorVel.Text = this.leituraRate.ToString();
			if (this.synth != null)
			{
				this.synth.Rate = this.leituraRate;
				this.synth.SpeakAsyncCancelAll();
				if (this.isPlaying && !this.isPaused)
				{
					this.ContinueFromWord();
				}
			}
		};
		base.Controls.Add(button2);
		this.listStories.Location = new System.Drawing.Point(12, 50);
		this.listStories.Size = new System.Drawing.Size(180, 550);
		this.listStories.SelectedIndexChanged += new System.EventHandler(ListStories_SelectedIndexChanged);
		this.btnOpen.Location = new System.Drawing.Point(12, 12);
		this.btnOpen.Size = new System.Drawing.Size(180, 30);
		this.btnOpen.Text = "Abrir TXT";
		this.btnOpen.Click += new System.EventHandler(BtnOpen_Click);
		this.rtbContent.Location = new System.Drawing.Point(200, 50);
		this.rtbContent.Size = new System.Drawing.Size(760, 550);
		this.rtbContent.Font = new System.Drawing.Font("Segoe UI", 12f);
		this.btnPrev.Location = new System.Drawing.Point(200, 12);
		this.btnPrev.Size = new System.Drawing.Size(75, 30);
		this.btnPrev.Text = "Página -";
		this.btnPrev.Click += new System.EventHandler(BtnPrev_Click);
		this.btnNext.Location = new System.Drawing.Point(285, 12);
		this.btnNext.Size = new System.Drawing.Size(75, 30);
		this.btnNext.Text = "Página +";
		this.btnNext.Click += new System.EventHandler(BtnNext_Click);
		this.btnPlay.Location = new System.Drawing.Point(365, 12);
		this.btnPlay.Size = new System.Drawing.Size(75, 30);
		this.btnPlay.Text = "Play";
		this.btnPlay.Click += new System.EventHandler(BtnPlay_Click);
		this.btnStop.Location = new System.Drawing.Point(445, 12);
		this.btnStop.Size = new System.Drawing.Size(75, 30);
		this.btnStop.Text = "Stop";
		this.btnStop.Click += new System.EventHandler(BtnStop_Click);
		this.btnFontPlus.Location = new System.Drawing.Point(525, 12);
		this.btnFontPlus.Size = new System.Drawing.Size(75, 30);
		this.btnFontPlus.Text = "+ Fonte";
		this.btnFontPlus.Click += new System.EventHandler(BtnFontPlus_Click);
		this.btnFontMinus.Location = new System.Drawing.Point(605, 12);
		this.btnFontMinus.Size = new System.Drawing.Size(75, 30);
		this.btnFontMinus.Text = "- Fonte";
		this.btnFontMinus.Click += new System.EventHandler(BtnFontMinus_Click);
		this.btnBookmark.Location = new System.Drawing.Point(685, 12);
		this.btnBookmark.Size = new System.Drawing.Size(110, 30);
		this.btnBookmark.Text = "Salvar Marcador";
		this.btnBookmark.Click += new System.EventHandler(BtnBookmark_Click);
		this.btnTheme.Location = new System.Drawing.Point(800, 12);
		this.btnTheme.Size = new System.Drawing.Size(75, 30);
		this.btnTheme.Text = "Tema";
		this.btnTheme.Click += new System.EventHandler(BtnTheme_Click);
		this.lblPage.Location = new System.Drawing.Point(885, 12);
		this.lblPage.Size = new System.Drawing.Size(120, 30);
		this.lblPage.Text = "Página 1/1";
		this.progressBar.Location = new System.Drawing.Point(200, 605);
		this.progressBar.Size = new System.Drawing.Size(760, 45);
		this.progressBar.Scroll += new System.EventHandler(ProgressBar_Scroll);
		base.ClientSize = new System.Drawing.Size(1000, 670);
		base.Controls.Add(this.listStories);
		base.Controls.Add(this.btnOpen);
		base.Controls.Add(this.rtbContent);
		base.Controls.Add(this.btnPrev);
		base.Controls.Add(this.btnNext);
		base.Controls.Add(this.btnPlay);
		base.Controls.Add(this.btnStop);
		base.Controls.Add(this.btnFontPlus);
		base.Controls.Add(this.btnFontMinus);
		base.Controls.Add(this.btnBookmark);
		base.Controls.Add(this.btnTheme);
		base.Controls.Add(this.lblPage);
		base.Controls.Add(this.progressBar);
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Leitor de Histórias";
		((System.ComponentModel.ISupportInitialize)this.progressBar).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}

	private void ApplyTheme()
	{
		if (darkMode)
		{
			BackColor = Color.FromArgb(30, 30, 30);
			{
				foreach (Control control3 in base.Controls)
				{
					control3.ForeColor = Color.White;
					if (control3 is Button)
					{
						control3.BackColor = Color.FromArgb(60, 60, 60);
					}
					if (control3 is RichTextBox)
					{
						control3.BackColor = Color.FromArgb(20, 20, 20);
					}
					if (control3 is ListBox)
					{
						control3.BackColor = Color.FromArgb(20, 20, 20);
					}
				}
				return;
			}
		}
		BackColor = SystemColors.Control;
		foreach (Control control4 in base.Controls)
		{
			control4.ForeColor = SystemColors.ControlText;
			if (control4 is Button)
			{
				control4.BackColor = SystemColors.ControlLight;
			}
			if (control4 is RichTextBox)
			{
				control4.BackColor = Color.White;
			}
			if (control4 is ListBox)
			{
				control4.BackColor = Color.White;
			}
		}
	}

	private void BtnOpen_Click(object sender, EventArgs e)
	{
		openFileDialog.Multiselect = true;
		openFileDialog.Filter = "Arquivos de Texto|*.txt";
		if (openFileDialog.ShowDialog() != DialogResult.OK)
		{
			return;
		}
		filePaths = openFileDialog.FileNames.ToList();
		listStories.Items.Clear();
		foreach (string filePath in filePaths)
		{
			listStories.Items.Add(Path.GetFileName(filePath));
		}
		if (filePaths.Count > 0)
		{
			listStories.SelectedIndex = 0;
			LoadStory(filePaths[0]);
		}
		if (synth == null)
		{
			synth = new SpeechSynthesizer();
			synth.Rate = leituraRate;
		}
		synth.Rate = leituraRate;
		if (listStories.SelectedItem == null)
		{
			MessageBox.Show("Selecione uma história primeiro.");
		}
	}

	private void ListStories_SelectedIndexChanged(object sender, EventArgs e)
	{
		int selectedIndex = listStories.SelectedIndex;
		if (selectedIndex >= 0 && selectedIndex < filePaths.Count)
		{
			LoadStory(filePaths[selectedIndex]);
		}
	}

	private void LoadStory(string path)
	{
		try
		{
			currentText = File.ReadAllText(path);
			PaginarTexto();
			currentPage = 0;
			UpdatePage();
			PrepareSentences(rtbContent.Text);
			currentSentenceIndex = 0;
		}
		catch (Exception ex)
		{
			MessageBox.Show("Erro ao abrir arquivo: " + ex.Message);
		}
	}

	private void PaginarTexto()
	{
		pages.Clear();
		if (string.IsNullOrEmpty(currentText))
		{
			pages.Add("");
			return;
		}
		int num;
		for (int i = 0; i < currentText.Length; i += num)
		{
			num = Math.Min(charsPerPage, currentText.Length - i);
			string text = currentText.Substring(i, num);
			if (i + num < currentText.Length)
			{
				int num2 = text.LastIndexOfAny(new char[3] { '\n', ' ', '\r' });
				if (num2 > 0)
				{
					text = text.Substring(0, num2);
					num = text.Length;
				}
			}
			pages.Add(text);
		}
	}

	private void UpdatePage()
	{
		if (pages.Count != 0)
		{
			currentPage = Math.Max(0, Math.Min(currentPage, pages.Count - 1));
			rtbContent.Text = pages[currentPage];
			lblPage.Text = $"Página {currentPage + 1}/{pages.Count}";
			progressBar.Maximum = pages.Count - 1;
			progressBar.Value = currentPage;
		}
	}

	private void BtnPrev_Click(object sender, EventArgs e)
	{
		if (currentPage > 0)
		{
			currentPage--;
			UpdatePage();
		}
	}

	private void BtnNext_Click(object sender, EventArgs e)
	{
		if (currentPage < pages.Count - 1)
		{
			currentPage++;
			UpdatePage();
		}
	}

	private void BtnPlay_Click(object sender, EventArgs e)
	{
		if (isStopped)
		{
			isStopped = false;
			isPlaying = true;
			isPaused = false;
			PrepareSentences(rtbContent.Text);
			currentSentenceIndex = 0;
			currentWordIndex = 0;
			btnPlay.Text = "Pause";
			ReadNextSentence();
		}
		else if (!isPlaying)
		{
			if (isPaused)
			{
				isPaused = false;
				isPlaying = true;
				btnPlay.Text = "Pause";
				ContinueFromWord();
				return;
			}
			isPlaying = true;
			isPaused = false;
			btnPlay.Text = "Pause";
			if (sentences == null || sentences.Count == 0)
			{
				PrepareSentences(rtbContent.Text);
			}
			ReadNextSentence();
		}
		else
		{
			synth.SpeakAsyncCancelAll();
			isPaused = true;
			isPlaying = false;
			btnPlay.Text = "Play";
		}
	}

	private void BtnStop_Click(object sender, EventArgs e)
	{
		isStopped = true;
		synth.SpeakAsyncCancelAll();
		isPlaying = false;
		isPaused = false;
		PrepareSentences(rtbContent.Text);
		currentSentenceIndex = 0;
		currentWordIndex = 0;
		btnPlay.Text = "Play";
	}

	private void BtnFontPlus_Click(object sender, EventArgs e)
	{
		rtbContent.Font = new Font(rtbContent.Font.FontFamily, rtbContent.Font.Size + 1f);
	}

	private void BtnFontMinus_Click(object sender, EventArgs e)
	{
		rtbContent.Font = new Font(rtbContent.Font.FontFamily, Math.Max(8f, rtbContent.Font.Size - 1f));
	}

	private void BtnBookmark_Click(object sender, EventArgs e)
	{
		savedBookmarkPage = currentPage;
		MessageBox.Show($"Marcador salvo na página {currentPage + 1}");
	}

	private void BtnTheme_Click(object sender, EventArgs e)
	{
		darkMode = !darkMode;
		ApplyTheme();
	}

	private void ProgressBar_Scroll(object sender, EventArgs e)
	{
		currentPage = progressBar.Value;
		UpdatePage();
	}

	private void PrepareSentences(string text)
	{
		sentences = (from s in text.Split(new char[3] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
			select s.Trim() + ".").ToList();
		if (sentences.Count == 0)
		{
			sentences.Add(text);
		}
		currentSentenceIndex = 0;
	}

	private void ReadNextSentence()
	{
		if (currentSentenceIndex >= sentences.Count)
		{
			if (currentPage < pages.Count - 1)
			{
				currentPage++;
				UpdatePage();
				PrepareSentences(rtbContent.Text);
				currentSentenceIndex = 0;
				ReadNextSentence();
			}
			else
			{
				isPlaying = false;
				btnPlay.Text = "Play";
			}
		}
		else
		{
			string text = sentences[currentSentenceIndex];
			currentWords = text.Split(' ');
			currentWordIndex = 0;
			currentWords = text.Split(' ');
			currentWordIndex = 0;
			synth.SpeakAsync(text);
		}
	}

	private void Synth_SpeakProgress(object sender, SpeakProgressEventArgs e)
	{
		for (int i = 0; i < currentWords.Length; i++)
		{
			if (currentWords[i].StartsWith(e.Text, StringComparison.OrdinalIgnoreCase))
			{
				currentWordIndex = i;
				break;
			}
		}
	}

	private void ContinueFromWord()
	{
		if (currentWords == null || currentWords.Length == 0)
		{
			ReadNextSentence();
			return;
		}
		string textToSpeak = string.Join(" ", currentWords.Skip(currentWordIndex));
		synth.Rate = leituraRate;
		synth.SpeakAsyncCancelAll();
		synth.SpeakAsync(textToSpeak);
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		synth.SpeakAsyncCancelAll();
		synth.Dispose();
		base.OnFormClosing(e);
	}
}
