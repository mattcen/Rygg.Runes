﻿using Rygg.Runes.Client.ViewModels;
using RyggRunes.Client.Core;
using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Maui;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using System.Reactive.Disposables;
using RyggRunes.MAUI.Client.Dispatcher;
using CommunityToolkit.Maui.Markup;
#if WINDOWS
using Windows.Media.Capture;
#endif
namespace RyggRunes.MAUI.Client
{
    public partial class MainPage : ReactiveContentPage<MainWindowViewModel>
    {
        public event EventHandler ImageDataChanged;
        public MainPage(MainWindowViewModel vm)
        {
            
            ViewModel = vm;
            vm.DispatcherService = new MauiDispatcher(Dispatcher);
            InitializeComponent();
            BindingContext = ViewModel;
            ViewModel.WhenPropertyChanged(p => p.CapturedImageBytes).Subscribe(async pv =>
            {
                await Task.Delay(500);
                ImageDataChanged?.Invoke(this, EventArgs.Empty);
            });
            this.WhenActivated(d =>
            {
                ViewModel.Alert.RegisterHandler(async interaction =>
                {
                    await DisplayAlert("Alert", interaction.Input, "OK");
                    interaction.SetOutput(true);
                }).DisposeWith(d);
                ViewModel.OpenFile.RegisterHandler(async interaction =>
                {
                    try
                    {
                        var fileResult = await MediaPicker.PickPhotoAsync(new MediaPickerOptions()
                        {
                            Title = "Pick a Photo with Runes"
                        });
                        if (fileResult != null)
                            interaction.SetOutput(await fileResult.OpenReadAsync());
                    }
                    catch (Exception ex) 
                    {
                        await DisplayAlert("Alert", ex.Message, "Ok");
                    }
                }).DisposeWith(d);
                ViewModel.HasPermissions.RegisterHandler(async interaction =>
                {
                    try
                    {
                        var status = await Permissions.RequestAsync<Permissions.StorageRead>();
                        var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                        interaction.SetOutput(status == PermissionStatus.Granted && cameraStatus == PermissionStatus.Granted);
                    }
                    catch(Exception ex)
                    {
                        await DisplayAlert("Alert", ex.Message, "Ok");
                    }
                }).DisposeWith(d);
                ViewModel.CaptureWithCamera.RegisterHandler(async interaction =>
                {
#if WINDOWS
                    var captureUi = new RyggRunes.MAUI.Client.WinUI.CustomCameraCaptureUI();
                    var result = await captureUi.CaptureFileAsync(CameraCaptureUIMode.Photo);
                    if (result != null && result.IsAvailable)
                    {
                        interaction.SetOutput(await result.OpenStreamForReadAsync());
                    }
#else
                    try
                    {
                        if (MediaPicker.IsCaptureSupported)
                        {
                            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                            {
                                Title = "Take a Photo"
                            });

                            if (photo != null)
                            {
                                interaction.SetOutput(await photo.OpenReadAsync());
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        await DisplayAlert("Alert", ex.Message, "Ok");
                    }
#endif
                }).DisposeWith(d);
                ViewModel.SaveImage.RegisterHandler(async interaction =>
                {
                    MemoryStream ms = new();
                    await imgEditor.SaveAsync(ms, ImageFormat.Jpeg, 100);
                    interaction.SetOutput(ms);
                }).DisposeWith(d);
            });
            
        }
    }
}