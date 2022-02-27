using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AssignmentTwo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string? sourceText;
        // propertis that will handle casesens settings
        private string SearchText => this.SearchIsCaseSens ? this.wordTextBox.Text : this.wordTextBox.Text.ToLower();
        private string StartText => this.StartIsCaseSens ? this.wordReplaceTextBox.Text : this.wordReplaceTextBox.Text.ToLower();
        // setting up a char array for punctuation
        private static char[] SplitChars => new[] { '.', '!', '?' };
        // bools for checkboxes
        private bool SearchIsCaseSens { get; set; }
        private bool StartIsCaseSens { get; set; }
        // property that will handle if did set null but not take casesens into account
        public string SourceText
        {
            get 
            {
                return this.sourceText ?? string.Empty; 
            }
            set 
            { 
                this.sourceText = value; 
            }
        }
        // property that will handle SourceText with casesens setting, we need to have both to be able to write the new document with same case as it was default
        public string SourceTextCaseHandled => this.SearchIsCaseSens ? this.SourceText : this.SourceText.ToLower();

        private void SourceFileButton_Click(object sender, RoutedEventArgs e)
        {
            // setting up a openfiledialog to have the user choose a text file
            OpenFileDialog fileDialog = new OpenFileDialog();
            // setting up a filter so you cant submit any other type of file
            fileDialog.Filter = "text file (*.txt;*.text)|*.txt;*.text";
            fileDialog.Title = "Select File";
            // we are not handling multiple files in the code, so lets keep it that way
            fileDialog.Multiselect = false;
            // if the user presses OK in the dialog
            if (fileDialog.ShowDialog() == true)
            {
                // setting up a streamreader to read the files content
                StreamReader sr = new(fileDialog.FileName);

                // adding the files content to SourceText
                this.SourceText = sr.ReadToEnd();
                // showing the content in our ui
                this.RefreshSourceFileTextBlock();
                // since we now have a file, we can enable the stackpanel that is holding all textfields and buttons
                if (!string.IsNullOrEmpty(this.SourceText))
                {
                    stackPanel.IsEnabled = true;
                }
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            this.ClearStats();
            // if we have loaded a file with any text
            if (this.SourceText != null)
            {
                // start a new task to not lock ui and also have something we can timeout on if the operation would become to heavy
                var task = Task.Run(() => {
                    // using dispatcher to be able to change ui stuff that other treads own
                    this.Dispatcher.Invoke(() =>
                    {
                        this.totalWordTextblock.Text = $"Total occurrences of {this.SearchText}: {Regex.Matches(this.SourceTextCaseHandled, @$"(?:^|\W){this.SearchText}(?:$|\W)").Count}";
                        this.totalSentenceTextblock.Text = $"Total in sentences {this.SourceTextCaseHandled.Split(SplitChars).Where(x => Regex.Match(x,@$"(?:^|\W){this.SearchText}(?:$|\W)").Success).Count()}";
                        this.totalSentenceMissingTextblock.Text = $"Total sentences without {this.SourceTextCaseHandled.Split(SplitChars).Where(x => !Regex.Match(x, @$"(?:^|\W){this.SearchText}(?:$|\W)").Success).Count()}";
                        this.totalBlockTextblock.Text = $"Total in segments { this.SourceTextCaseHandled.Split(Environment.NewLine).Where(x =>  Regex.Match(x, @$"(?:^|\W){this.SearchText}(?:$|\W)").Success).Count()}";
                    });
                });
                // we wait for a maximum of 60 seconds for the operation above to finnish
                await task.WaitAsync(TimeSpan.FromSeconds(60));
            }
        }

        private void ClearStats()
        {
            // Clearing all textblocks with string.empty
            this.totalWordTextblock.Text = string.Empty;
            this.totalSentenceMissingTextblock.Text= string.Empty;
            this.totalSentenceTextblock.Text = string.Empty;
            this.totalBlockTextblock.Text = string.Empty;
        }

        private void WordTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // if space discard by telling event is handled
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void Search_CaseSensCheckBox_Change(object sender, RoutedEventArgs e)
        {
            // setting new value for SearchIsCaseSens when even that checkbox is check or unchecked is fired (we bound them in the xaml)
            CheckBox chkBox = (CheckBox)sender;

            this.Dispatcher.Invoke(() => 
            { 
                this.SearchIsCaseSens = chkBox?.IsChecked ?? false;
            });
        }

        private void Start_CaseSensCheckBox_Change(object sender, RoutedEventArgs e)
        {
            // same as above but diff property
            CheckBox chkBox = (CheckBox)sender;

            this.Dispatcher.Invoke(() =>
            {
                this.StartIsCaseSens = chkBox?.IsChecked ?? false;
            });
        }

        private void WordIndexReplaceTextBox_PreviewKeyDown(object sender, TextCompositionEventArgs e)
        {
            // regex to make textbox only accept numeric, since wpf dont have such a editor by default

            e.Handled = Regex.Match(e.Text, "[^0-9]+").Success;
        }

        public void RefreshSourceFileTextBlock()
        {
            // here we refresh the ui textblock, hence just setting its textvalue to SourceText
            this.sourceFileTextBlock.Text = this.SourceText;
        }

        private async void AddWordButton_Click(object sender, RoutedEventArgs e)
        {
            // since the stats would be obsolete when you are adding new words, we clear them
            this.ClearStats();
            // firstly we start with sepeation all sentences using regex
            var allSentences = Regex.Split(this.SourceText, @$"(?<=[{string.Join("", SplitChars)}])");

            // setting up an empty string to build upon
            string text = string.Empty;

            // task to have the operation run on separate thread
            var task = Task.Run(() => {

                this.Dispatcher.Invoke(() =>
                {
                    // we try to parse the textbox to get where to put SUNDSVALL, the textbox should only accept numerics, but lets be safe and use a if tryparse
                    if (int.TryParse(this.wordIndexReplaceTextBox.Text, out int index))
                    {
                        // since index start at 0 for the array, and set Sundsvall before choosen index, we dont have to decrement,
                        // setting 1 will result in word set at position 2, setting 0 resulting in it coming at first position of sentence

                        // iterating all sentenses 
                        foreach (var item in allSentences)
                        {
                            // looking for matching sentences using regex and also taking case sensitivity into account
                            if (Regex.Match(this.StartIsCaseSens ? item : item.ToLower(), @$"(?:^|\W){this.StartText}(?:$|\W)").Success)
                            {
                                // if the sentence match, we split up the words to know how many it contains, and be able to manipulate the places we want
                                var splitted = item.Split(' ');
                                // if index is larger or same lengt,
                                if (index >= splitted.Length)
                                {
                                    //  we add SUNDSVALL to end of sentence, we also move the punctuation so the added SUNDSVALL is now last word of sentence.
                                    text += $"{string.Join(" ", splitted, startIndex: 0, splitted.Length - 1)} {string.Join("", splitted[splitted.Length -1].Substring(0, splitted[splitted.Length - 1].Length -1) + " SUNDSVALL" + splitted[splitted.Length -1].Last())}";
                                }
                                else
                                {
                                    // if index is smaller we just add it before the current word holding that index, taking its spot
                                    splitted[index] = "SUNDSVALL " + splitted[index];
                                    text += string.Join(" ", splitted);
                                }
                            }
                            else
                            {
                                // if the sentence doesnt contaion our word, we just add the sentence as whole.
                                text += item;
                            }
                        }
                        // when we are done, we change the SourceText and update the UI
                        this.SourceText = text;
                        this.RefreshSourceFileTextBlock();
                    }
                });
            });

            await task.WaitAsync(TimeSpan.FromSeconds(60));

        }

        private void saveToDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            // setting up a dialog for saving files
            // setting its filter and having a title saying what you are doing
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "text file (*.txt;*.text)|*.txt;*.text";
            saveFileDialog.Title = "Save As";

            // if you set a name or select a existing file and pressing ok
            if (saveFileDialog.ShowDialog() == true)
            {
                // write to file, if file exists, its going to get overwritten, but it will inform the user, and user has to accept or decline, else we create the file
                File.WriteAllText(saveFileDialog.FileName, this.SourceText);
            }
        }
    }
}
