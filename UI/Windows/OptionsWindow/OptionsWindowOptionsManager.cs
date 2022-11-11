using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using SPCode.Utils;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.UI.Windows;

public partial class OptionsWindow
{
    private async void RestoreButton_Clicked(object sender, RoutedEventArgs e)
    {
        var result = await this.ShowMessageAsync(Translate("Warning"),
            Translate("ResetOptQues"), MessageDialogStyle.AffirmativeAndNegative, Program.MainWindow.MetroDialogOptions);
        if (result == MessageDialogResult.Affirmative)
        {
            Program.OptionsObject = new OptionsControl();
            Program.OptionsObject.ReCreateCryptoKey();
            Program.MainWindow.OptionMenuEntry.IsEnabled = false;
            await this.ShowMessageAsync(Translate("RestartEditor"),
                Translate("YRestartEditor"), MessageDialogStyle.Affirmative,
                Program.MainWindow.MetroDialogOptions);
            Close();
        }
    }
    
    private async void BackupButton_Clicked(object sender, RoutedEventArgs e)
    {
        var optionsFile = new FileInfo(PathsHelper.OptionsFilePath);
        var sfd = new SaveFileDialog
        {
            FileName = "options_0",
            DefaultExt = ".dat",
            Filter = Constants.DatFilesFilter
        };

        if (sfd.ShowDialog() is bool result && result)
        {
            try
            {
                if (File.Exists(sfd.FileName))
                {
                    File.Delete(sfd.FileName);
                }
                OptionsControl.Save();
                File.Copy(optionsFile.FullName, sfd.FileName);
                await this.ShowMessageAsync(Translate("Success"), Translate("BackupOptionsSuccessMessage"), settings: Program.MainWindow.MetroDialogOptions);
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync(Translate("Error"), $"{Translate("BackupOptionsErrorMessage")}. {Translate("Details")}: {ex.Message}", settings: Program.MainWindow.MetroDialogOptions);
            }
        }
    }

    private async void LoadButton_Clicked(object sender, RoutedEventArgs e)
    {

        var confirmationResult = await this.ShowMessageAsync(Translate("Warning"), Translate("LoadOptionsWarningMessage"), 
            MessageDialogStyle.AffirmativeAndNegative, Program.MainWindow.MetroDialogOptions);

        if (confirmationResult == MessageDialogResult.Negative)
        {
            return;
        }

        var optionsFile = new FileInfo(PathsHelper.OptionsFilePath);
        var ofd = new OpenFileDialog
        {
            Filter = Constants.DatFilesFilter
        };

        if (ofd.ShowDialog() is bool result && result)
        {
            try
            {
                var pickedFile = new FileInfo(ofd.FileName);
                optionsFile.Delete();
                pickedFile.CopyTo(optionsFile.FullName);
                Program.PreventOptionsSaving = true;
                Program.MainWindow.OptionMenuEntry.IsEnabled = false;
                await this.ShowMessageAsync(Translate("Success"), $"{Translate("LoadOptionsSuccessMessage")}.", settings: Program.MainWindow.MetroDialogOptions);
                Close();
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync(Translate("Error"), $"{Translate("LoadOptionsErrorMessage")}. {Translate("Details")}: {ex.Message}", settings: Program.MainWindow.MetroDialogOptions);
            }
        }
    }
}
