﻿using Katran.Models;
using Katran.ViewModels;
using KatranClassLibrary;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Katran.UserControlls
{
    /// <summary>
    /// Логика взаимодействия для MessageUI.xaml
    /// </summary>
    public partial class MessageUI : UserControl, INotifyPropertyChanged
    {
        private bool isOwnerMessage;
        public bool IsOwnerMessage
        {
            get { return isOwnerMessage; }
            set 
            { 
                isOwnerMessage = value;

                if (isOwnerMessage)
                {
                    GridColumn = 3;
                    mainGrid.HorizontalAlignment = HorizontalAlignment.Right;
                    messageStatusCheck.HorizontalAlignment = HorizontalAlignment.Left;
                }
                else
                {
                    GridColumn = 0;
                    mainGrid.HorizontalAlignment = HorizontalAlignment.Left;
                    messageStatusCheck.HorizontalAlignment = HorizontalAlignment.Right;
                }
            }
        }

        private int gridColumn;
        public int GridColumn
        {
            get { return gridColumn; }
            set { gridColumn = value; OnPropertyChanged(); }
        }

        public BitmapImage ContactAvatar
        {
            get { return (BitmapImage)GetValue(ContactAvatarProperty); }
            set { SetValue(ContactAvatarProperty, value); OnPropertyChanged(); }
        }
        public static readonly DependencyProperty ContactAvatarProperty =
            DependencyProperty.Register("ContactAvatar", typeof(BitmapImage), typeof(MessageUI),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        private Status contactStatus;
        public Status ContactStatus
        {
            get { return contactStatus; }
            set
            {
                contactStatus = value;

                switch (value)
                {
                    case Status.Online:
                        ContactStatusShower.Fill = (System.Windows.Media.Brush)Application.Current.FindResource("OnlineBrush");
                        break;
                    case Status.Offline:
                    default:
                        ContactStatusShower.Fill = (System.Windows.Media.Brush)Application.Current.FindResource("OfflineBrush");
                        break;
                }

            }
        }

        private MessageType messageType;
        public MessageType MessageType
        {
            get { return messageType; }
            set
            {
                messageType = value;

                switch (messageType)
                {
                    case MessageType.File:
                        messageFile.Visibility = Visibility.Visible;
                        messageText.Visibility = Visibility.Collapsed;
                        break;
                    case MessageType.Text:
                        messageFile.Visibility = Visibility.Collapsed;
                        messageText.Visibility = Visibility.Visible;
                        break;
                }
                OnPropertyChanged();
            }
        }

        private DateTime messageDateTime;
        public DateTime MessageDateTime
        {
            get { return messageDateTime; }
            set 
            { 
                messageDateTime = value;
                OnPropertyChanged();

                Date = messageDateTime.ToShortDateString();
                Time = messageDateTime.ToShortTimeString();
            }
        }

        private string date;
        public string Date
        {
            get { return date; }
            set { date = value; OnPropertyChanged(); }
        }

        private string time;
        public string Time
        {
            get { return time; }
            set { time = value; OnPropertyChanged(); }
        }

        private string text;
        public string Text
        {
            get { return text; }
            set { text = value; OnPropertyChanged(); }
        }

        public ICommand DownloadCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    System.Windows.Forms.FolderBrowserDialog folderDlg = new System.Windows.Forms.FolderBrowserDialog();
                    System.Windows.Forms.DialogResult result = folderDlg.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        mainPageViewModel.DownloadFile(folderDlg.SelectedPath, ChatId, MessageId);
                    }
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        private string filename;
        public string FileName
        {
            get { return filename; }
            set { filename = value; OnPropertyChanged(); }
        }

        private string fileSize;
        public string FileSize
        {
            get { return fileSize; }
            set { fileSize = value; OnPropertyChanged(); }
        }

        private MainPageViewModel mainPageViewModel;
        public MainPageViewModel MainPageViewModel
        {
            get { return mainPageViewModel; }
            set { mainPageViewModel = value; }
        }

        private MessageState messageState;

        public MessageState MessageState
        {
            get { return messageState; }
            set 
            { 
                messageState = value;
                OnPropertyChanged();

                switch (messageState)
                {
                    case MessageState.Readed:
                        messageStatusCheck.Kind = PackIconKind.CheckAll;
                        messageStatusCheck.Foreground = (Brush)Application.Current.FindResource("Readed_MessageSatus_Brush");
                        break;
                    case MessageState.Unreaded:
                    case MessageState.Sended:
                        messageStatusCheck.Kind = PackIconKind.CheckAll;
                        messageStatusCheck.Foreground = (Brush)Application.Current.FindResource("TextColor");
                        break;
                    case MessageState.Unsended:
                        messageStatusCheck.Kind = PackIconKind.Check;
                        messageStatusCheck.Foreground = (Brush)Application.Current.FindResource("TextColor");
                        break;
                    default:
                        break;
                }

            }
        }

        private int chatId;
        public int ChatId
        {
            get { return chatId; }
            set { chatId = value; }
        }

        private int messageId;
        public int MessageId
        {
            get { return messageId; }
            set { messageId = value; }
        }

        private int senderId;
        public int SenderId
        {
            get { return senderId; }
            set 
            { 
                senderId = value;
                OnPropertyChanged();
            }
        }

        private string userName; 

        public string UserName
        {
            get { return userName; }
            set { userName = value; OnPropertyChanged(); }
        }


        public MessageUI()
        {
            InitializeComponent();
            IsOwnerMessage = false;
            ContactAvatar = null;
            ContactStatus = Status.Offline;
            MessageType = MessageType.Text;
            MessageDateTime = DateTime.MinValue;
            MessageState = MessageState.Unsended;
            UserName = "";
            Text = "";
            FileName = "";
            FileSize = "";
            messageStatusCheck.Visibility = Visibility.Collapsed;
        }

        public MessageUI(MainPageViewModel mainPageViewModel, bool isOwnerMessage, BitmapImage contactAvatar, Status contactStatus, MessageType messageType, MessageState messageState, DateTime messageDateTime, byte[] messageBody, string userName, int chatId, int messageId, int senderId, string fileName = "", string fileSize = "")
        {
            InitializeComponent();
            MainPageViewModel = mainPageViewModel;
            IsOwnerMessage = isOwnerMessage;
            ContactAvatar = contactAvatar;
            ContactStatus = contactStatus;
            MessageType = messageType;
            MessageState = messageState;
            MessageDateTime = messageDateTime;
            Text = Encoding.UTF8.GetString(messageBody, 0, messageBody.Length);
            UserName = userName;
            ChatId = chatId;
            MessageId = messageId;
            SenderId = senderId;

            FileName = fileName;
            FileSize = fileSize;

        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void MessageUI_UC_MouseEnter(object sender, MouseEventArgs e)
        {
            if (MessageState != MessageState.Readed && mainPageViewModel.MainViewModel.UserInfo.Info.Id != senderId)
            {
                Task.Factory.StartNew(() =>
                {
                    Client.ServerRequest(new RRTemplate(RRType.RefreshMessageState, new RefreshMessageStateTemplate(ChatId, MessageId, MessageState)));
                });
            }
        }
    }
}
