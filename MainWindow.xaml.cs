using System.Windows;
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Win32;

namespace HashCalculator
{
    public partial class MainWindow : Window
    {
        private List<string> _selectedFiles;
        private List<HashResult> _results;

        public MainWindow()
        {
            InitializeComponent();
            _selectedFiles = new List<string>();
            _results = new List<HashResult>();
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                ShowDragPreview(files);
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void ShowDragPreview(string[] files)
        {
            if (files.Length == 1)
            {
                string fileName = Path.GetFileName(files[0]);
                DropZoneText.Text = $"Drop to add: {fileName}";
            }
            else if (files.Length <= 3)
            {
                var fileNames = files.Select(f => Path.GetFileName(f));
                DropZoneText.Text = $"Drop to add: {string.Join(", ", fileNames)}";
            }
            else
            {
                DropZoneText.Text = $"Drop to add: {files.Length} files";
            }
        }
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                _selectedFiles.AddRange(files);

                UpdateDropZoneText();
            }
        }

        private void UpdateDropZoneText()
        {
            if (_selectedFiles.Count > 0)
            {
                var fileNames = _selectedFiles.Select(f => Path.GetFileName(f));

                if (_selectedFiles.Count == 1)
                {
                    DropZoneText.Text = $"Selected: {fileNames.First()}";
                }
                else
                {
                    DropZoneText.Text = $"Selected files:\n{string.Join("\n", fileNames)}";
                }
            }
            else
            {
                DropZoneText.Text = "Drag and drop files here";
            }
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            UpdateDropZoneText();
        }

        private async void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFiles.Count == 0)
            {
                MessageBox.Show("Please select files first!");
                return;
            }

            var algorithms = new List<string>();
            if (MD5CheckBox.IsChecked == true) algorithms.Add("MD5");
            if (SHA256CheckBox.IsChecked == true) algorithms.Add("SHA256");

            if (algorithms.Count == 0)
            {
                MessageBox.Show("Please select at least one hash algorithm!");
                return;
            }

            // Check if we're in verification mode
            bool isVerifyMode = VerifyModeCheckBox.IsChecked == true;
            string expectedHash = "";

            if (isVerifyMode)
            {
                expectedHash = ExpectedHashTextBox.Text.Trim();
                if (string.IsNullOrEmpty(expectedHash))
                {
                    MessageBox.Show("Please enter an expected hash for verification!");
                    return;
                }
            }

            CalculateButton.IsEnabled = false;

            try
            {
                var calculator = new FileHashCalculator();
                var results = new List<HashResult>();

                foreach (string filePath in _selectedFiles)
                {
                    foreach (string algorithm in algorithms)
                    {
                        try
                        {
                            string hash = await calculator.CalculateHashAsync(filePath, algorithm);
                            string fileName = Path.GetFileName(filePath);

                            // Use verification if enabled
                            string verificationResult = isVerifyMode ? VerifyHash(hash, expectedHash) : "";

                            results.Add(new HashResult
                            {
                                FileName = fileName,
                                Algorithm = algorithm,
                                HashValue = hash,
                                Status = "Success",
                                ExpectedHash = isVerifyMode ? expectedHash : "",      // NEW
                                VerificationStatus = verificationResult              // NEW
                            });
                        }
                        catch (Exception ex)
                        {
                            results.Add(new HashResult
                            {
                                FileName = Path.GetFileName(filePath),
                                Algorithm = algorithm,
                                HashValue = "Error",
                                Status = ex.Message,
                                ExpectedHash = isVerifyMode ? expectedHash : "",
                                VerificationStatus = isVerifyMode ? "ERROR" : ""
                            });
                        }
                    }
                }

                _results.AddRange(results);
                ResultsGrid.ItemsSource = _results;
            }
            finally
            {
                CalculateButton.IsEnabled = true;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _results.Clear();
            _selectedFiles.Clear();
            ResultsGrid.ItemsSource = null;
            ResultsGrid.ItemsSource = _results;
            UpdateDropZoneText();
        }

        private void CopyHash_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem is HashResult selectedResult)
            {
                if (!string.IsNullOrEmpty(selectedResult.HashValue) && selectedResult.HashValue != "Error")
                {
                    Clipboard.SetText(selectedResult.HashValue);
                    MessageBox.Show($"Hash value copied to clipboard!\n{selectedResult.HashValue}",
                                  "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("No valid hash to copy.", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Please select a row first.", "No Selection",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CopyFileName_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem is HashResult selectedResult)
            {
                Clipboard.SetText(selectedResult.FileName);
                MessageBox.Show($"File name copied to clipboard!\n{selectedResult.FileName}",
                              "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select a row first.", "No Selection",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Files to Hash",
                Multiselect = true,
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFiles.AddRange(openFileDialog.FileNames);
                UpdateDropZoneText();
            }
        }

        private void VerifyMode_Changed(object sender, RoutedEventArgs e)
        {
            if (VerifyModeCheckBox.IsChecked == true)
            {
                ExpectedHashLabel.Visibility = Visibility.Visible;
                ExpectedHashTextBox.Visibility = Visibility.Visible;
                CalculateButton.Content = "Calculate & Verify";
            }
            else
            {
                ExpectedHashLabel.Visibility = Visibility.Collapsed;
                ExpectedHashTextBox.Visibility = Visibility.Collapsed;
                CalculateButton.Content = "Calculate Hashes";
                ExpectedHashTextBox.Text = "";
            }
        }

        private string VerifyHash(string calculatedHash, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash))
            {
                return "";
            }
            string cleanCalculated = calculatedHash.Replace(" ", "").ToLower();
            string cleanExpected = expectedHash.Replace(" ", "").ToLower();

            if (cleanCalculated.Equals(cleanExpected))
                return "MATCH";
            else
                return "MISMATCH";
        }
    }
}
