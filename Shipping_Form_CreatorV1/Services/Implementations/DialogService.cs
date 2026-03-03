using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Shipping_Form_CreatorV1.Services.Implementations
{
    public class DialogService
    {
        public static void ShowErrorDialog(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static MessageBoxResult ShowCustomYesNoCancelDialog(string title, string message, string yesText, string noText, string cancelText)
        {
            if (!TaskDialog.IsPlatformSupported)
            {
                return MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            }

            var dialog = new TaskDialog
            {
                Caption = title,
                InstructionText = message,
                Icon = TaskDialogStandardIcon.Warning,
            };

            var yesButton = new TaskDialogCommandLink("yesButton", yesText);
            yesButton.Click += (s, e) => { dialog.Close(TaskDialogResult.Yes); };

            var noButton = new TaskDialogCommandLink("noButton", noText);
            noButton.Click += (s, e) => { dialog.Close(TaskDialogResult.No); };

            var cancelButton = new TaskDialogCommandLink("cancelButton", cancelText);
            cancelButton.Click += (s, e) => { dialog.Close(TaskDialogResult.Cancel); };

            dialog.Controls.Add(yesButton);
            dialog.Controls.Add(noButton);
            //dialog.Controls.Add(cancelButton);

            var result = dialog.Show();

            return result switch
            {
                TaskDialogResult.Yes => MessageBoxResult.Yes,
                TaskDialogResult.No => MessageBoxResult.No,
                TaskDialogResult.Cancel => MessageBoxResult.Cancel,
                _ => MessageBoxResult.Cancel, // Default to Cancel if the dialog is closed in a different way
            };
        }
    }
}
